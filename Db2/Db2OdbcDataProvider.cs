using Dapper;
using Microsoft.Extensions.Options;
using MoE.Commercial.Data.Extensions;
using System.Data;
using System.Data.Odbc;

namespace MoE.Commercial.Data.Db2
{
    public class Db2OdbcDataProvider : IDataProvider
    {
            public Db2OdbcDataProvider(IOptions<Db2Settings> options)
            {
                Config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            }

            public Db2Settings Config { get; }

            public string Schema => Config.Schema;

        public Task<IEnumerable<T>> Query<T>(string query, GenericDbParameter[]? parameters = null)
        {
            var parms = new DynamicParameters();

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    parms.Add(param.ParameterName, param.Value);
                }
            }

            using (OdbcConnection conn = new OdbcConnection(Config.ConnectionString))
            {
                return Task.FromResult(conn.Query<T>(query, parms));
            }
        }

        public Task ExecuteNonQuery(
            string commandText,
            CommandType commandType,
            GenericDbParameter[] parameters
        )
        {
            using (OdbcConnection conn = new OdbcConnection(Config.ConnectionString))
            {
                conn.Open();

                using (OdbcCommand cmd = new OdbcCommand(commandText, conn))
                {
                    cmd.CommandTimeout = 60;
                    cmd.CommandType = commandType;

                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value));
                    }

                    cmd.ExecuteNonQuery();
                }

                conn.Close();
            }

            return Task.CompletedTask;
        }

        public Task<DataTable> ExecuteSql(string sql, GenericDbParameter[]? parameters = null)
        {
            DataTable data = null;

            //fetch data from DB2
            using (OdbcConnection conn = new OdbcConnection(Config.ConnectionString))
            {
                OdbcCommand cmd = new OdbcCommand(sql, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value));
                    }
                }

                cmd.CommandTimeout = 60;

                OdbcDataAdapter da = new OdbcDataAdapter(cmd);

                DataSet records = new DataSet();

                da.Fill(records);

                data = records.Tables[0];
            }

            return Task.FromResult(data);
        }

        public async Task<T> ExecuteScalar<T>(string sql, GenericDbParameter[]? parameters = null)
        {
            T data = default;

            using (OdbcConnection conn = new OdbcConnection(Config.ConnectionString))
            {
                OdbcCommand cmd = new OdbcCommand(sql, conn);

                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(new OdbcParameter(param.ParameterName, param.Value));
                    }
                }

                cmd.CommandTimeout = 60;

                await conn.OpenAsync();

                var rawData = await cmd.ExecuteScalarAsync();

                if (rawData != null && rawData != DBNull.Value)
                {
                    if (rawData is string
                        && typeof(T) != typeof(string)
                        && !typeof(T).IsSubclassOf(typeof(string)))
                    {
                        // caller wants something other than a string, but the value returned from SQL is a string
                        // try and convert the string value to type T
                        data = ((string)rawData).Convert<T>();
                    }
                    else
                        data = rawData is T ? (T)rawData : default;
                }
            }

            return data;
        }
    }
}