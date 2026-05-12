using Npgsql;
using System.Collections.Concurrent;

namespace bifeldy_lib_90.Databases {

    public interface IDataSourceCache {
        NpgsqlDataSource GetOrAddNpgsqlDataSource(string connectionString);
    }

    public sealed class CDataSourceCache : IDataSourceCache {

        private readonly ConcurrentDictionary<string, NpgsqlDataSource> _npgsql = new();

        public NpgsqlDataSource GetOrAddNpgsqlDataSource(string connectionString) {
            return this._npgsql.GetOrAdd(connectionString, cs => {
                var builder = new NpgsqlSlimDataSourceBuilder(cs);

                // Biar Bisa Lempar Array Parameter
                _ = builder.EnableArrays();

                return builder.Build();
            });
        }

    }

}