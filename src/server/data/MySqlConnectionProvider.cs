using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.IO;

namespace Screenly.Server.Data
{

    public class MySqlConnectionProvider : IConnectionProvider
    {
        private readonly string _connectionString;

        public MySqlConnectionProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetConnection() 
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}