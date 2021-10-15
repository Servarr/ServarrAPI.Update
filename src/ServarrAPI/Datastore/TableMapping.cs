using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dapper;
using ServarrAPI.Datastore.Converters;
using ServarrAPI.Model;

namespace ServarrAPI.Datastore
{
    public static class TableMapping
    {
        static TableMapping()
        {
            Mapper = new TableMapper();
        }

        public static TableMapper Mapper { get; private set; }

        public static void Map()
        {
            RegisterMappers();

            Mapper.Entity<UpdateEntity>("update").RegisterModel()
                .LazyLoad(x => x.UpdateFiles,
                          async (db, u) =>
                          {
                              var result = await db.Query<UpdateFileEntity>(new SqlBuilder()
                                                                            .Where<UpdateFileEntity>(x => x.UpdateId == u.Id))
                                  .ConfigureAwait(false);
                              return result.ToList();
                          },
                          u => u.Id > 0);

            Mapper.Entity<UpdateFileEntity>("updatefile").RegisterModel()
                .HasOne(x => x.Update, x => x.UpdateId);

            Mapper.Entity<Notification>("notification").RegisterModel();
        }

        private static void RegisterMappers()
        {
            SqlMapper.RemoveTypeMap(typeof(byte[]));
            SqlMapper.AddTypeHandler(new GzipConverter());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<string>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<Runtime>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<Architecture>>());
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<OperatingSystem>>());
        }
    }
}
