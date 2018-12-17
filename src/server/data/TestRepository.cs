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
    public abstract class TestRepository : ITestRepository
    {
        protected IConnectionProvider ConnectionProvider { get; }
        private string _statementSeparator;
        private string _blobType;
        private string _autoIncrement;
        private string _schemaExistsSql;
        private string _nowSql;
        private string _lastRowIdSql;
        private string _limitBeforeSql;
        private string _limitAfterSql;
        private string _escapeOpen;
        private string _escapeClose;

        protected TestRepository(IConnectionProvider connectionProvider, string statementSeparator, string blobType, string autoIncrement, string schemaExistsSql, string nowSql, string lastRowIdSql, string escapeOpen, string escapeClose, bool limitBefore)
        {
            ConnectionProvider = connectionProvider;
            _statementSeparator = statementSeparator;
            _blobType = blobType;
            _autoIncrement = autoIncrement;
            _schemaExistsSql = schemaExistsSql;
            _nowSql = nowSql;
            _lastRowIdSql = lastRowIdSql;
            _limitBeforeSql = limitBefore ? "top 1" : "";
            _limitAfterSql = limitBefore ? "" : "limit 1";
            _escapeOpen = escapeOpen;
            _escapeClose = escapeClose;
        }

        private static SHA256Managed _crypto = new SHA256Managed();

        const string SCHEMA = @"
        CREATE TABLE TestRun (
            testrunid integer <AUTOINCREMENT>, 
            application varchar(100) not null, 
            start datetime not null, 
            <ESCAPEOPEN>end<ESCAPECLOSE> datetime, 
            metadata varchar(1000),
            PRIMARY KEY (testrunid)) <EOS>

        CREATE INDEX IX_TestRun_Application ON TestRun (application) <EOS>

        CREATE TABLE Benchmark (
            application varchar(100), 
            path varchar(500) not null, 
            name varchar(100) not null,
            data varchar(64) not null,
            PRIMARY KEY (application, path)) <EOS>

        CREATE TABLE ReferenceImage (
            testrunid integer not null,
            path varchar(500) not null, 
            name varchar(100) not null,
            data varchar(64) not null,
            PRIMARY KEY (testrunid, path)) <EOS>

        CREATE TABLE TestImage (
            testrunid integer not null,
            path varchar(500) not null, 
            name varchar(100) not null,
            state varchar(15) not null,
            uploaded DATETIME not null,
            compareStart DATETIME,
            compareEnd DATETIME,
            claimedBy VARCHAR(100),
            data varchar(64),
            difference varchar(64),
            error varchar(8000),
            PRIMARY KEY (testrunid, path)) <EOS>

        CREATE TABLE <ESCAPEOPEN>Blob<ESCAPECLOSE> (
            sha varchar(64) primary key,
            data <BLOBTYPE> not null
        ) <EOS>
        ";

        private static string GetSha(byte[] bytes)
        {
            return string.Join("", _crypto.ComputeHash(bytes).Select(b => b.ToString("x2")).ToArray());
        }

        private object _schemaCheckLock = new object();

        public void EnsureSchema()
        {
            lock (_schemaCheckLock)
            {
                using (var conn = ConnectionProvider.GetConnection())
                {
                    if (conn.QuerySingle<int>(_schemaExistsSql) == 0)
                    {
                        Console.WriteLine("Recreating schema");

                        var schema = ReplaceTokens(SCHEMA);

                        conn.Execute(schema);
                        int i = conn.QuerySingle<int>(_schemaExistsSql);
                        Console.WriteLine(i);
                    }
                }
            }
        }

        public IEnumerable<TestRunSummary> GetTestRunSummaries(string appName)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                var whereClause = string.IsNullOrEmpty(appName)
                    ? ""
                    : "where application = @appName";

                var sql = ReplaceTokens(@"
                    select 
                        testRunId,
                        application, 
                        start, 
                        <ESCAPEOPEN>end<ESCAPECLOSE>,
                        (select count(1) from TestImage ti where tr.testrunid = ti.testrunid) testCount,
                        (select count(1) from TestImage ti where tr.testrunid = ti.testrunid and ti.state = 'Success') successCount,
                        (select count(1) from TestImage ti where tr.testrunid = ti.testrunid and ti.state = 'NoBenchmark') missingBenchmarkCount,
                        (select count(1) from TestImage ti where tr.testrunid = ti.testrunid and ti.state = 'Different') differenceCount,
                        (select count(1) from TestImage ti where tr.testrunid = ti.testrunid and ti.state = 'Error') errorCount
                    from TestRun tr " + whereClause + " order by tr.start desc");
                return conn.Query<TestRunSummary>(sql, new { appName });
            }
        }

        private string ReplaceTokens(string s)
        {
            return s
                .Replace("<EOS>", _statementSeparator)
                .Replace("<BLOBTYPE>", _blobType)
                .Replace("<AUTOINCREMENT>", _autoIncrement)
                .Replace("<ESCAPEOPEN>", _escapeOpen)
                .Replace("<ESCAPECLOSE>", _escapeClose)
                .Replace("<NOW>", _nowSql)
                .Replace("<LIMITBEFORE>", _limitBeforeSql)
                .Replace("<LIMITAFTER>", _limitAfterSql);
        }

        public int CreateTestRun(string application)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                return conn.QuerySingle<int>($"insert into TestRun (application, start) values (@application, {_nowSql}); select {_lastRowIdSql};",
                    new { application });
            }
        }

        public void SetTestRunEnd(int testRunId, DateTime end)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                conn.Execute($"update TestRun set {_escapeOpen}end{_escapeClose} = @end where testrunid = @testRunId",
                    new { testRunId, end });
            }
        }

        public TestRun GetTestRun(int testRunId)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                var testRun = conn.Query<TestRun>(
                    $"select TestRunId, Application, Start, {_escapeOpen}end{_escapeClose} from TestRun where testRunId = @testRunId",
                    new { testRunId }).FirstOrDefault();
                if (testRun == null) return null;

                testRun.ReferenceImages = conn.Query<ReferenceImage>("select name, path from ReferenceImage where testrunid = @testRunId",
                    new { testRunId }).ToArray();

                testRun.TestImages = conn.Query<TestImage>("select name, path, state, compareStart, error from TestImage where testRunId = @testRunId",
                    new { testRunId }).ToArray();

                return testRun;
            }
        }

        public void DeleteTestRun(int testRunId)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                conn.Execute("delete from TestImage where testRunId = @testRunId; delete from ReferenceImage where testRunId = @testRunId; delete from TestRun where testRunId = @testRunId",
                    new {testRunId});
            }
        }

        private string SaveBlob(IDbConnection conn, byte[] data)
        {
            var sha = GetSha(data);

            if (conn.QuerySingle<int>(ReplaceTokens("select count(*) from <ESCAPEOPEN>Blob<ESCAPECLOSE> where sha = @sha"), new { sha }) == 0)
            {
                conn.Execute(ReplaceTokens("insert into <ESCAPEOPEN>Blob<ESCAPECLOSE> (sha, data) values (@sha, @data)"), new { sha, data} );
            }

            return sha;
        }

        public void SaveBenchmark(string application, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = ConnectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, data);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "delete from Benchmark where application=@application and path=@path; insert into Benchmark (application, path, name, data) values (@application, @path, @name, @sha)";
                    AddParameter(cmd, "@application", application);
                    AddParameter(cmd, "@path", path);
                    AddParameter(cmd, "@name", name);
                    AddParameter(cmd, "@sha", sha);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveReferenceImage(int testRun, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var sha = SaveBlob(conn, data);

                cmd.CommandText = "delete from ReferenceImage where testRunId=@testRun and path=@path; insert into ReferenceImage (testRunId, path, name, data) values (@testRun, @path, @name, @sha)";
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);
                AddParameter(cmd, "@name", name);
                AddParameter(cmd, "@sha", sha);
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveTestImageError(int testRun, string path, string name, string error)
        {
            path = path.TrimStart('/');

            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"delete from TestImage where testRunId=@testRun and path=@path; insert into TestImage (testRunId, path, name, state, uploaded, error) values (@testRun, @path, @name, 'Error', {_nowSql}, @error)";
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);
                AddParameter(cmd, "@name", name);
                AddParameter(cmd, "@error", error);
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveTestImage(int testRunId, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = ConnectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, data);

                var hasBenchmark = conn.Query<int>("select count(*) from Benchmark b join TestRun tr on tr.application = b.application where tr.testRunId = @testRunId and b.path = @path"
                    , new { testRunId, path }).Single() > 0;
                
                var state = hasBenchmark ? ImageState.Submitted : ImageState.NoBenchmark;

                conn.Execute($"delete from TestImage where testRunId=@testRunId and path=@path; insert into TestImage (testRunId, path, name, state, uploaded, data) values (@testRunId, @path, @name, @state, {_nowSql}, @sha)",
                    new { testRunId, path, name, state, sha, data });
            }
        }

        public void PromoteBenchmark(int testRunId, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            {
                var application = conn.QuerySingle<string>("select application from TestRun where testRunId = @testRunId", new {testRunId});

                conn.Execute(@"
                    delete from Benchmark where application=@application and path=@path; insert into Benchmark (application, path, name, data) 
                        select @application, path, name, data from TestImage where testRunId = @testRunId and path = @path;

                    update TestImage set state = 'Success', difference = null where testRunId = @testRunId and path = @path",
                    new { application, testRunId, path });
            }
        }

        public byte[] GetBenchmark(string application, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ReplaceTokens("select b.data from Benchmark be join <ESCAPEOPEN>Blob<ESCAPECLOSE> b on b.sha = be.data where be.application = @application and be.path = @path");
                AddParameter(cmd, "@application", application);
                AddParameter(cmd, "@path", path);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var bytes = reader.IsDBNull(0) ? null : (byte[])reader[0];
                        return bytes;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            throw new NotImplementedException();
        }

        public byte[] GetReferenceImage(int testRun, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ReplaceTokens("select b.data from ReferenceImage ri join <ESCAPEOPEN>Blob<ESCAPECLOSE> b on b.sha = ri.data where ri.testRunId = @testRun and ri.path = @path");
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull(0) ? null : (byte[])reader[0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public byte[] GetTestImage(int testRun, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ReplaceTokens("select b.data from TestImage ti join <ESCAPEOPEN>Blob<ESCAPECLOSE> b on b.sha = ti.data where ti.testRunId = @testRun and ti.path = @path");
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull(0) ? null : (byte[])reader[0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public byte[] GetTestDifference(int testRun, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ReplaceTokens("select b.data from TestImage ti join <ESCAPEOPEN>Blob<ESCAPECLOSE> b on b.sha = ti.difference where ti.testRunId = @testRun and ti.path = @path");
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull(0) ? null : (byte[])reader[0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public byte[] GetDifferenceImage(int testRun, string path)
        {
            path = path.TrimStart('/');
            using (var conn = ConnectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = ReplaceTokens("select b.data from TestImage ti join <ESCAPEOPEN>Blob<ESCAPECLOSE> b on b.sha = ti.difference where ti.testRunId = @testRun and ti.path = @path");
                AddParameter(cmd, "@testRun", testRun);
                AddParameter(cmd, "@path", path);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.IsDBNull(0) ? null : (byte[])reader[0];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        private object _claimLock = new object();

        public ClaimedTestImageInfo ClaimNextTestImage(string claimedBy)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                var claimed = GetClaimedImage(conn, claimedBy);
                if (claimed != null)
                {
                    return claimed;
                }

                // Don't allow two differ clients to claim the same image.
                lock (_claimLock)
                {
                    var unclaimed = GetUnclaimedImage(conn);
                    if (unclaimed != null)
                    {
                        ClaimImage(conn, unclaimed, claimedBy);
                        return unclaimed;
                    }
                }
            }

            return null;
        }

        private ClaimedTestImageInfo GetClaimedImage(IDbConnection conn, string claimedBy)
        {
            return conn.Query<ClaimedTestImageInfo>(ReplaceTokens(@"
                    select <LIMITBEFORE> tr.application, ti.testrunid, ti.path
                        from TestImage ti
                        join TestRun tr on tr.testrunid = ti.testrunid
                        where ti.state = 'Running' and ti.claimedBy=@claimedBy order by ti.uploaded <LIMITAFTER>"),
                    new { claimedBy }).FirstOrDefault();
        }

        private ClaimedTestImageInfo GetUnclaimedImage(IDbConnection conn)
        {
            return conn.Query<ClaimedTestImageInfo>(ReplaceTokens(@"
                select <LIMITBEFORE> tr.application, ti.testrunid, ti.path
                        from TestImage ti
                        join TestRun tr on tr.testrunid = ti.testrunid
                        where ti.state = 'Submitted' order by ti.uploaded <LIMITAFTER>")).FirstOrDefault();
        }

        private void ClaimImage(IDbConnection conn, ClaimedTestImageInfo image, string claimedBy)
        {
            conn.Execute(ReplaceTokens(
                    @"update TestImage set claimedBy=@claimedBy, state = 'Running', compareStart=<NOW> where 
                        state = 'Submitted' and testrunid = @testRunId and path = @path"),
                    new { claimedBy, testRunId = image.TestRunId, application = image.TestRunId, path = image.Path});
        }

        public void MarkTestImageDifferent(int testRunId, string path, byte[] difference)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, difference);
                conn.Execute($"update TestImage set state = 'Different', compareEnd = {_nowSql}, difference = @sha, error = null where testRunId = @testRunId and path = @path", new { testRunId, path, sha });
            }
        }

        public void MarkTestImageError(int testRunId, string path, string error)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                conn.Execute($"update TestImage set state = 'Error', compareEnd = {_nowSql}, difference = null, error = @error where testRunId = @testRunId and path = @path", new { testRunId, path, error });
            }
        }

        public void MarkTestImageSuccess(int testRunId, string path)
        {
            using (var conn = ConnectionProvider.GetConnection())
            {
                conn.Execute($"update TestImage set state = 'Success', compareEnd = {_nowSql}, difference = null, error = null where testRunId = @testRunId and path = @path", new { testRunId, path });
            }
        }

        private void AddParameter(IDbCommand cmd, string name, object value)
        {
            var parm = cmd.CreateParameter();
            parm.ParameterName = name;
            parm.Value = value;
            cmd.Parameters.Add(parm);
        }
    }
}