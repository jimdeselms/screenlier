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
    public class TestRepository : ITestRepository
    {
        private readonly IConnectionProvider _connectionProvider;
        public TestRepository(IConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        private static SHA256Managed _crypto = new SHA256Managed();

        const string SCHEMA = @"
        CREATE TABLE TestRun (
            testrunid integer primary key, 
            application varchar(100) not null, 
            start datetime not null, 
            end datetime, 
            metadata varchar(1000));

        CREATE INDEX IX_TestRun_Application ON TestRun (application);

        CREATE TABLE Benchmark (
            application varchar(100), 
            path varchar(8000) not null, 
            name varchar(100) not null,
            data varchar(64) not null,
            PRIMARY KEY (application, path));

        CREATE TABLE ReferenceImage (
            testrunid integer not null,
            path varchar(8000) not null, 
            name varchar(100) not null,
            data varchar(64) not null,
            PRIMARY KEY (testrunid, path));

        CREATE TABLE TestImage (
            testrunid integer not null,
            path varchar(8000) not null, 
            name varchar(100) not null,
            state INT not null,
            uploaded DATETIME not null,
            compareStart DATETIME,
            compareEnd DATETIME,
            claimedBy VARCHAR(100),
            data varchar(64),
            difference varchar(64),
            error varchar(8000),
            PRIMARY KEY (testrunid, path));

        CREATE TABLE Blob (
            sha varchar(64) primary key,
            data blob not null
        )
        ";

        private static string GetSha(byte[] bytes)
        {
            return string.Join("", _crypto.ComputeHash(bytes).Select(b => b.ToString("x2")).ToArray());
        }

        public void EnsureSchema()
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                if (conn.QuerySingle<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='TestRun'") == 0)
                {
                    conn.Execute(SCHEMA);
                    int i = conn.QuerySingle<int>("SELECT count(*) FROM sqlite_master WHERE type='table' AND name='TestRun'");
                    Console.WriteLine(i);
                }
            }
        }

        public IEnumerable<TestRunSummary> GetTestRunSummaries(string appName)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                var whereClause = string.IsNullOrEmpty(appName)
                    ? ""
                    : "where application = :appName";

                var sql = @"
                    select 
                        testRunId,
                        application, 
                        start, 
                        end,
                        (select count(1) from testimage ti where tr.testrunid = ti.testrunid) testCount,
                        (select count(1) from testimage ti where tr.testrunid = ti.testrunid and ti.state = 3) successCount,
                        (select count(1) from testimage ti where tr.testrunid = ti.testrunid and ti.state = 2) missingBenchmarkCount,
                        (select count(1) from testimage ti where tr.testrunid = ti.testrunid and ti.state = 4) differenceCount,
                        (select count(1) from testimage ti where tr.testrunid = ti.testrunid and ti.state = 5) errorCount
                    from TestRun tr " + whereClause + " order by tr.start desc";
                return conn.Query<TestRunSummary>(sql, new { appName });
            }
        }
        public int CreateTestRun(string application)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                return conn.QuerySingle<int>("insert into TestRun (application, start) values (:application, datetime('now')); select last_insert_rowid();",
                    new { application });
            }
        }

        public void SetTestRunEnd(int testRunId, DateTime end)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                conn.Execute("update TestRun set end = :end where testrunid = :testRunId",
                    new { testRunId, end });
            }
        }

        public TestRun GetTestRun(int testRunId)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                var testRun = conn.Query<TestRun>(
                    @"select TestRunId, Application, Start, End from TestRun where testRunId = :testRunId",
                    new { testRunId }).FirstOrDefault();
                if (testRun == null) return null;

                testRun.ReferenceImages = conn.Query<ReferenceImage>("select name, path from referenceimage where testrunid = :testRunId",
                    new { testRunId }).ToArray();

                testRun.TestImages = conn.Query<TestImage>("select name, path, state, compareStart, error from TestImage where testRunId = :testRunId",
                    new { testRunId }).ToArray();

                return testRun;
            }
        }

        public void DeleteTestRun(int testRunId)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                conn.Execute("delete from TestImage where testRunId = :testRunId; delete from ReferenceImage where testRunId = :testRunId; delete from TestRun where testRunId = :testRunId",
                    new {testRunId});
            }
        }

        private string SaveBlob(IDbConnection conn, byte[] data)
        {
            var sha = GetSha(data);

            if (conn.QuerySingle<int>("select count(*) from Blob where sha = :sha", new { sha }) == 0)
            {
                conn.Execute("insert into Blob (sha, data) values (:sha, :data)", new { sha, data} );
            }

            return sha;
        }

        public void SaveBenchmark(string application, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = _connectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, data);
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "insert or replace into Benchmark (application, path, name, data) values (:application, :path, :name, :sha)";
                    AddParameter(cmd, ":application", application);
                    AddParameter(cmd, ":path", path);
                    AddParameter(cmd, ":name", name);
                    AddParameter(cmd, ":sha", sha);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SaveReferenceImage(int testRun, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                var sha = SaveBlob(conn, data);

                cmd.CommandText = "insert or replace into ReferenceImage (testRunId, path, name, data) values (:testRun, :path, :name, :sha)";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);
                AddParameter(cmd, ":name", name);
                AddParameter(cmd, ":sha", sha);
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveTestImageError(int testRun, string path, string name, string error)
        {
            path = path.TrimStart('/');

            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "insert or replace into TestImage (testRunId, path, name, state, uploaded, error) values (:testRun, :path, :name, 5, datetime('now'), :error)";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);
                AddParameter(cmd, ":name", name);
                AddParameter(cmd, ":error", error);
                cmd.ExecuteNonQuery();
            }
        }

        public void SaveTestImage(int testRunId, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            using (var conn = _connectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, data);

                var hasBenchmark = conn.Query<int>("select count(*) from Benchmark b join TestRun tr on tr.application = b.application where tr.testRunId = :testRunId and b.path = :path"
                    , new { testRunId, path }).Single() > 0;
                
                var state = hasBenchmark ? ImageState.Submitted : ImageState.NoBenchmark;

                conn.Execute("insert or replace into TestImage (testRunId, path, name, state, uploaded, data) values (:testRunId, :path, :name, :state, datetime('now'), :sha)",
                    new { testRunId, path, name, state, sha, data });
            }
        }

        public void PromoteBenchmark(int testRunId, string path)
        {
            path = path.TrimStart('/');
            using (var conn = _connectionProvider.GetConnection())
            {
                var application = conn.QuerySingle<string>("select application from TestRun where testRunId = :testRunId", new {testRunId});

                conn.Execute(@"
                    insert or replace into Benchmark (application, path, name, data) 
                        select :application, path, name, data from TestImage where testRunId = :testRunId and path = :path;

                    update TestImage set state = 3, difference = null where testRunId = :testRunId and path = :path",
                    new { application, testRunId, path });
            }
        }

        public byte[] GetBenchmark(string application, string path)
        {
            path = path.TrimStart('/');
            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select b.data from Benchmark be join Blob b on b.sha = be.data where be.application = :application and be.path = :path";
                AddParameter(cmd, ":application", application);
                AddParameter(cmd, ":path", path);

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
            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select b.data from ReferenceImage ri join Blob b on b.sha = ri.data where ri.testRunId = :testRun and ri.path = :path";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);

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
            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select b.data from TestImage ti join Blob b on b.sha = ti.data where ti.testRunId = :testRun and ti.path = :path";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);

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
            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select b.data from TestImage ti join Blob b on b.sha = ti.difference where ti.testRunId = :testRun and ti.path = :path";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);

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
            using (var conn = _connectionProvider.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "select b.data from TestImage ti join Blob b on b.sha = ti.difference where ti.testRunId = :testRun and ti.path = :path";
                AddParameter(cmd, ":testRun", testRun);
                AddParameter(cmd, ":path", path);

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

        public ClaimedTestImageInfo ClaimNextTestImage(string claimedBy)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                return conn.Query<ClaimedTestImageInfo>(
                    @"update TestImage set claimedBy=:claimedBy, state = 1, compareStart=datetime('now') where testrunid = 
                        (select testrunid from TestImage where state = 0 order by uploaded limit 1); 
                    select tr.application, ti.testrunid, ti.path
                        from TestImage ti
                        join TestRun tr on tr.testrunid = ti.testrunid
                        where ti.state = 1 and ti.claimedBy=:claimedBy order by ti.uploaded limit 1", 
                new { claimedBy }).FirstOrDefault();
            }
        }

        public void MarkTestImageDifferent(int testRunId, string path, byte[] difference)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                var sha = SaveBlob(conn, difference);
                conn.Execute("update TestImage set state = 4, compareEnd = datetime('now'), difference = :sha, error = null where testRunId = :testRunId and path = :path", new { testRunId, path, sha });
            }
        }

        public void MarkTestImageError(int testRunId, string path, string error)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                conn.Execute("update TestImage set state = 5, compareEnd = datetime('now'), difference = null, error = :error where testRunId = :testRunId and path = :path", new { testRunId, path, error });
            }
        }

        public void MarkTestImageSuccess(int testRunId, string path)
        {
            using (var conn = _connectionProvider.GetConnection())
            {
                conn.Execute("update TestImage set state = 3, compareEnd = datetime('now'), difference = null, error = null where testRunId = :testRunId and path = :path", new { testRunId, path });
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