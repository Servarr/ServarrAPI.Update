using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Serilog;

namespace ServarrAPI.Datastore
{
    public static class SqlMapperExtensions
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(SqlMapperExtensions));

        public static async Task<IEnumerable<T>> Query<T>(this IDatabase db, string sql, object param = null)
        {
            using var conn = await db.OpenConnection().ConfigureAwait(false);

            var items = await SqlMapper.QueryAsync<T>(conn, sql, param).ConfigureAwait(false);
            if (TableMapping.Mapper.LazyLoadList.TryGetValue(typeof(T), out var lazyProperties))
            {
                foreach (var item in items)
                {
                    ApplyLazyLoad(db, item, lazyProperties);
                }
            }

            return items;
        }

        public static async Task<IEnumerable<TReturn>> Query<TFirst, TSecond, TReturn>(this IDatabase db, string sql, Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id", int? commandTimeout = null, CommandType? commandType = null)
        {
            TReturn mapWithLazy(TFirst first, TSecond second)
            {
                ApplyLazyLoad(db, first);
                ApplyLazyLoad(db, second);
                return map(first, second);
            }

            using var conn = await db.OpenConnection();
            return await SqlMapper.QueryAsync<TFirst, TSecond, TReturn>(conn, sql, mapWithLazy, param, transaction, buffered, splitOn, commandTimeout, commandType);
        }

        public static async Task<IEnumerable<T>> Query<T>(this IDatabase db, SqlBuilder builder)
        {
            var watch = Stopwatch.StartNew();
            var type = typeof(T);
            var sql = builder.Select(type).AddSelectTemplate(type);

            Logger.Verbose($"Generated query in {watch.ElapsedMilliseconds}ms");
            watch.Restart();

            var result = await db.Query<T>(sql.RawSql, sql.Parameters).ConfigureAwait(false);
            Logger.Verbose($"Executed SQL in {watch.ElapsedMilliseconds}ms");

            return result;
        }

        public static async Task<IEnumerable<T>> QueryJoined<T, T2>(this IDatabase db, SqlBuilder builder, Func<T, T2, T> mapper)
        {
            var type = typeof(T);
            var sql = builder.Select(type, typeof(T2)).AddSelectTemplate(type);

            return await db.Query(sql.RawSql, mapper, sql.Parameters);
        }

        private static void ApplyLazyLoad<TModel>(IDatabase db, TModel model)
        {
            if (TableMapping.Mapper.LazyLoadList.TryGetValue(typeof(TModel), out var lazyProperties))
            {
                ApplyLazyLoad(db, model, lazyProperties);
            }
        }

        private static void ApplyLazyLoad<TModel>(IDatabase db, TModel model, List<LazyLoadedProperty> lazyProperties)
        {
            if (model == null)
            {
                return;
            }

            foreach (var lazyProperty in lazyProperties)
            {
                var lazy = (ILazyLoaded)lazyProperty.LazyLoad.Clone();
                lazy.Prepare(db, model);
                lazyProperty.Property.SetValue(model, lazy);
            }
        }
    }
}
