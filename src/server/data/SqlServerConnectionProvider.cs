using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace Screenly.Server.Data
{

    public class SqlServerConnectionProvider : IConnectionProvider
    {
        private readonly string _connectionString;

        public SqlServerConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetConnection() 
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}