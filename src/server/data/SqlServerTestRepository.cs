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
    public class SqlServerTestRepository : TestRepository
    {
        public SqlServerTestRepository(IConnectionProvider connectionProvider) : base(
            connectionProvider, 
            "", 
            "varbinary(max)",
            "IDENTITY(1,1)",
            "SELECT count(*) FROM sys.tables WHERE name='TestRun'",
            "getdate()",
            "SCOPE_IDENTITY()",
            "[",
            "]",
            limitBefore: true)
        {
        }
   }
}