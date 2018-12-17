using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Screenly.Server.Data
{
    public static class RepositoryFactory
    {
        private static readonly ITestRepository _testRepository;

        static RepositoryFactory()
        {
            // Is there a SQL Server database connection string?
            var mysql = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
            if (mysql != null)
            {
                Console.WriteLine("Using MySQL database " + mysql.Substring(0, 50) + "...");
                _testRepository = CreateMySqlRepository(mysql);
            }
            else
            {
                var sqlServer = Environment.GetEnvironmentVariable("SQLSERVER_DATABASE");
                if (sqlServer != null)
                {
                    Console.WriteLine("Using SQL Server database " + sqlServer.Substring(0, 50) + "...");
                    _testRepository = CreateSqlServerRepository(sqlServer);
                }
                else
                {
                    // Has a sqlite database been defined? Use that.
                    var sqliteDb = Environment.GetEnvironmentVariable("SQLITE_DATABASE") ?? "sqlite.db";
                    Console.WriteLine("Using SQLite database " + sqliteDb);
                    _testRepository = CreateSqliteRepository(sqliteDb);
                }
            }
        }

        public static ITestRepository GetTestRepository()
        {
            return _testRepository;
        }

        private static ITestRepository CreateMySqlRepository(string connectionString)
        {
            var connectionProvider = new MySqlConnectionProvider(connectionString);
            return new MySqlTestRepository(connectionProvider);
        }
        private static ITestRepository CreateSqlServerRepository(string connectionString)
        {
            var connectionProvider = new SqlServerConnectionProvider(connectionString);
            return new SqlServerTestRepository(connectionProvider);
        }

        private static ITestRepository CreateSqliteRepository(string filename)
        {
            var connectionProvider = new SqliteConnectionProvider(filename);
            return new SqliteTestRepository(connectionProvider);
        }
    }
}