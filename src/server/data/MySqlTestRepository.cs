using System;
using System.Data.SQLite;
using System.Linq;
using System.Data;
using System.IO;
using Dapper;
using Screenly.Server.Models;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Screenly.Server.Data
{
    public class MySqlTestRepository : TestRepository
    {
        public MySqlTestRepository(IConnectionProvider connectionProvider) : base(
            connectionProvider, 
            ";", 
            "mediumblob",
            "AUTO_INCREMENT",
            "SELECT count(*) FROM information_schema.tables where table_name = 'TestRun'",
            "now()",
            "LAST_INSERT_ID()",
            "`",
            "`",
            limitBefore: false)
        {
        }
   }
}