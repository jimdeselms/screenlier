using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Dapper;

namespace getblob
{
    class Program
    {
        static void Main(string[] args)
        {
            var sha = args[0];

            using (var conn = new SQLiteConnection($"Data Source=d:\\git\\screenlier2\\src\\server\\temporaryDatabase;Mode=Memory;Cache=Shared"))
            {
                var blob = conn.Query<byte[]>("select data from blob where sha = :sha", new { sha }).First();
                File.WriteAllBytes("c:\\temp.png", blob);
            }
        }
    }
}
