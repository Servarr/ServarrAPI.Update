using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;

namespace ServarrAPI.Datastore
{
    public interface IBasicRepository<TModel>
        where TModel : ModelBase, new()
    {
        Task<IEnumerable<TModel>> All();
        Task<int> Count();
        Task<TModel> Get(int id);
        Task<TModel> Insert(TModel model);
        Task<TModel> Update(TModel model);
        Task<TModel> Upsert(TModel model);
        Task SetFields(TModel model, params Expression<Func<TModel, object>>[] properties);
        Task Delete(TModel model);
        Task Delete(int id);
        Task<IEnumerable<TModel>> Get(IEnumerable<int> ids);
        Task InsertMany(IList<TModel> model);
        Task UpdateMany(IList<TModel> model);
        Task SetFields(IList<TModel> models, params Expression<Func<TModel, object>>[] properties);
        Task DeleteMany(List<TModel> model);
        Task DeleteMany(IEnumerable<int> ids);
        Task Purge();
        Task<bool> HasItems();
        Task<TModel> Single();
        Task<TModel> SingleOrDefault();
    }

    public class BasicRepository<TModel> : IBasicRepository<TModel>
        where TModel : ModelBase, new()
    {
        private readonly PropertyInfo _keyProperty;
        private readonly List<PropertyInfo> _properties;
        private readonly string _updateSql;
        private readonly string _insertSql;

        private readonly TransactionOptions _transactionOptions = new TransactionOptions
        {
            IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted
        };

        protected readonly IDatabase _database;
        protected readonly string _table;

        public BasicRepository(IDatabase database)
        {
            _database = database;

            var type = typeof(TModel);

            _table = TableMapping.Mapper.TableNameMapping(type);
            _keyProperty = type.GetProperty(nameof(ModelBase.Id));

            var excluded = TableMapping.Mapper.ExcludeProperties(type).Select(x => x.Name).ToList();
            excluded.Add(_keyProperty.Name);
            _properties = type.GetProperties().Where(x => x.IsMappableProperty() && !excluded.Contains(x.Name)).ToList();

            _insertSql = GetInsertSql();
            _updateSql = GetUpdateSql(_properties);
        }

        protected virtual SqlBuilder Builder() => new SqlBuilder();

        protected virtual async Task<List<TModel>> Query(SqlBuilder builder) => (await _database.Query<TModel>(builder).ConfigureAwait(false)).ToList();

        protected Task<List<TModel>> Query(Expression<Func<TModel, bool>> where) => Query(Builder().Where(where));

        public async Task<int> Count()
        {
            using var conn = await _database.OpenConnection();
            return await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {_table}");
        }

        public virtual async Task<IEnumerable<TModel>> All()
        {
            return await Query(Builder());
        }

        public async Task<TModel> Get(int id)
        {
            var model = (await Query(x => x.Id == id)).FirstOrDefault();

            if (model == null)
            {
                throw new Exception($"{typeof(TModel)} with id {id} not found");
            }

            return model;
        }

        public async Task<IEnumerable<TModel>> Get(IEnumerable<int> ids)
        {
            if (!ids.Any())
            {
                return new List<TModel>();
            }

            var result = await Query(x => ids.Contains(x.Id));

            if (result.Count != ids.Count())
            {
                throw new ApplicationException($"Expected query to return {ids.Count()} rows but returned {result.Count}");
            }

            return result;
        }

        public async Task<TModel> SingleOrDefault()
        {
            return (await All()).SingleOrDefault();
        }

        public async Task<TModel> Single()
        {
            return (await All()).Single();
        }

        public async Task<TModel> Insert(TModel model)
        {
            if (model.Id != 0)
            {
                throw new InvalidOperationException("Can't insert model with existing ID " + model.Id);
            }

            using var conn = await _database.OpenConnection();
            return await Insert(conn, model);
        }

        private string GetInsertSql()
        {
            var sbColumnList = new StringBuilder(null);
            for (var i = 0; i < _properties.Count; i++)
            {
                var property = _properties[i];
                sbColumnList.AppendFormat("\"{0}\"", property.Name.ToLower());
                if (i < _properties.Count - 1)
                {
                    sbColumnList.Append(", ");
                }
            }

            var sbParameterList = new StringBuilder(null);
            for (var i = 0; i < _properties.Count; i++)
            {
                var property = _properties[i];
                sbParameterList.AppendFormat("@{0}", property.Name);
                if (i < _properties.Count - 1)
                {
                    sbParameterList.Append(", ");
                }
            }

            return $"INSERT INTO {_table} ({sbColumnList.ToString()}) VALUES ({sbParameterList.ToString()}) RETURNING id";
        }

        private async Task<TModel> Insert(IDbConnection connection, TModel model)
        {
            SqlBuilderExtensions.LogQuery(_insertSql, model);
            var id = await connection.ExecuteScalarAsync<int>(_insertSql, model);
            _keyProperty.SetValue(model, id);

            return model;
        }

        public async Task InsertMany(IList<TModel> models)
        {
            if (models.Any(x => x.Id != 0))
            {
                throw new InvalidOperationException("Can't insert model with existing ID != 0");
            }

            using var tran = new TransactionScope(TransactionScopeOption.Required, _transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
            using var conn = await _database.OpenConnection();
            foreach (var model in models)
            {
                await Insert(conn, model);
            }

            tran.Complete();
        }

        public async Task<TModel> Update(TModel model)
        {
            if (model.Id == 0)
            {
                throw new InvalidOperationException("Can't update model with ID 0");
            }

            using var conn = await _database.OpenConnection();
            await UpdateFields(conn, model, _properties);

            return model;
        }

        public async Task UpdateMany(IList<TModel> models)
        {
            if (models.Any(x => x.Id == 0))
            {
                throw new InvalidOperationException("Can't update model with ID 0");
            }

            using var tran = new TransactionScope(TransactionScopeOption.Required, _transactionOptions, TransactionScopeAsyncFlowOption.Enabled);
            using var conn = await _database.OpenConnection();

            await UpdateFields(conn, models, _properties);

            tran.Complete();
        }

        protected async Task Delete(Expression<Func<TModel, bool>> where)
        {
            await Delete(Builder().Where<TModel>(where));
        }

        protected async Task Delete(SqlBuilder builder)
        {
            var sql = builder.AddDeleteTemplate(typeof(TModel)).LogQuery();

            using var conn = await _database.OpenConnection();
            await conn.ExecuteAsync(sql.RawSql, sql.Parameters);
        }

        public async Task Delete(TModel model)
        {
            await Delete(model.Id);
        }

        public async Task Delete(int id)
        {
            await Delete(x => x.Id == id);
        }

        public async Task DeleteMany(IEnumerable<int> ids)
        {
            if (ids.Any())
            {
                await Delete(x => ids.Contains(x.Id));
            }
        }

        public async Task DeleteMany(List<TModel> models)
        {
            await DeleteMany(models.Select(m => m.Id));
        }

        public async Task<TModel> Upsert(TModel model)
        {
            if (model.Id == 0)
            {
                await Insert(model);
                return model;
            }

            await Update(model);
            return model;
        }

        public async Task Purge()
        {
            using var conn = await _database.OpenConnection();
            await conn.ExecuteAsync($"DELETE FROM [{_table}]");
        }

        public async Task<bool> HasItems()
        {
            return await Count() > 0;
        }

        public async Task SetFields(TModel model, params Expression<Func<TModel, object>>[] properties)
        {
            if (model.Id == 0)
            {
                throw new InvalidOperationException("Attempted to update model without ID");
            }

            var propertiesToUpdate = properties.Select(x => x.GetMemberName()).ToList();

            using var conn = await _database.OpenConnection();
            await UpdateFields(conn, model, propertiesToUpdate);
        }

        public async Task SetFields(IList<TModel> models, params Expression<Func<TModel, object>>[] properties)
        {
            if (models.Any(x => x.Id == 0))
            {
                throw new InvalidOperationException("Attempted to update model without ID");
            }

            var propertiesToUpdate = properties.Select(x => x.GetMemberName()).ToList();

            using var conn = await _database.OpenConnection();
            await UpdateFields(conn, models, propertiesToUpdate);
        }

        private string GetUpdateSql(List<PropertyInfo> propertiesToUpdate)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("UPDATE {0} SET ", _table);

            for (var i = 0; i < propertiesToUpdate.Count; i++)
            {
                var property = propertiesToUpdate[i];
                sb.AppendFormat("\"{0}\" = @{1}", property.Name.ToLower(), property.Name);
                if (i < propertiesToUpdate.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append($" WHERE \"{_keyProperty.Name.ToLower()}\" = @{_keyProperty.Name}");

            return sb.ToString();
        }

        private async Task UpdateFields(IDbConnection connection, TModel model, List<PropertyInfo> propertiesToUpdate)
        {
            var sql = propertiesToUpdate == _properties ? _updateSql : GetUpdateSql(propertiesToUpdate);

            SqlBuilderExtensions.LogQuery(sql, model);

            await connection.ExecuteAsync(sql, model);
        }

        private async Task UpdateFields(IDbConnection connection, IList<TModel> models, List<PropertyInfo> propertiesToUpdate)
        {
            var sql = propertiesToUpdate == _properties ? _updateSql : GetUpdateSql(propertiesToUpdate);

            foreach (var model in models)
            {
                SqlBuilderExtensions.LogQuery(sql, model);
            }

            await connection.ExecuteAsync(sql, models);
        }
    }
}
