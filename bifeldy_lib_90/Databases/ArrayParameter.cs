using Npgsql;
using NpgsqlTypes;
using System.Data;
using static Dapper.SqlMapper;

namespace bifeldy_lib_90.Databases {

    public sealed class NpgsqlArrayParameter : ICustomQueryParameter {

        private readonly Array _values;
        private readonly NpgsqlDbType _dbType;

        public NpgsqlArrayParameter(Array values, NpgsqlDbType dbType) {
            this._values = values;
            this._dbType = dbType;
        }

        public void AddParameter(IDbCommand command, string name) {
            var param = (NpgsqlParameter)command.CreateParameter();

            param.ParameterName = name;
            param.NpgsqlDbType = NpgsqlDbType.Array | this._dbType;
            param.Value = this._values;

            _ = command.Parameters.Add(param);
        }

    }

}