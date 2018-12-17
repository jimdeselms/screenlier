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
    public class SqliteTestRepository : TestRepository
    {
        public SqliteTestRepository(IConnectionProvider connectionProvider) : base(
            connectionProvider, 
            ";", 
            "blob",
            "",
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='TestRun'",
            "datetime('now')",
            "last_insert_rowid()",
            "[",
            "]",
            limitBefore: false)
        {
        }
   }
}