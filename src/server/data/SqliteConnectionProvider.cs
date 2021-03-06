using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Screenly.Server.Data
{

    public class SqliteConnectionProvider : IConnectionProvider, IDisposable
    {
        private readonly string _name;
        private readonly IDbConnection _openConnection;

        public SqliteConnectionProvider(string name)
        {
            if (System.IO.File.Exists(name))
            {
                File.Delete(name);
            }
            _name = name;

            _openConnection = GetConnection();
        }

        public void Dispose()
        {
            _openConnection.Dispose();
            System.IO.File.Delete(_name);
        }

        public IDbConnection GetConnection() 
        {
            var conn = new SQLiteConnection($"Data Source={_name};Mode=Memory;Cache=Shared");
            conn.Open();
            return conn;
        }
    }
}