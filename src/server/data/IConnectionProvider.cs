using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace Screenly.Server.Data
{
    public interface IConnectionProvider
    {
        IDbConnection GetConnection();
    }
}