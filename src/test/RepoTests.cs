using System;
using System.IO;
using Xunit;

using Screenly.Server.Data;
using Screenly.Server.Models;

namespace Screenly.Tests
{
    public class ScreenlyUnitTests
    {
        [Fact]
        public void BasicRepoTests()
        {
            using (var connProvider = new SqliteConnectionProvider("basicRepoTests"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveBenchmark("prod", "/this/that", "mybenchmark", new byte[] { 1, 2, 3, 4});
                repo.SaveReferenceImage(1, "/this/that", "myreferenceimage", new byte[] { 1, 2, 3, 4});
                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3, 4});
                repo.SaveTestImage(1, "/this/that2", "mytestimage", new byte[] { 1, 2, 3, 4});

                repo.SetTestRunEnd(1, new DateTime(2001, 2, 4));

                var testRun = repo.GetTestRun(1);

                Assert.Equal("prod", testRun.Application);
                Assert.NotNull(testRun.End);

                Assert.Equal("myreferenceimage", testRun.ReferenceImages[0].Name);
                Assert.Equal("/this/that", testRun.ReferenceImages[0].Path);

                Assert.Equal("mytestimage", testRun.TestImages[0].Name);
                Assert.Equal("/this/that", testRun.TestImages[0].Path);
                Assert.Equal("Submitted", testRun.TestImages[0].State);

                var benchmark = repo.GetBenchmark("prod", "/this/that");
                Assert.Equal(new byte[] { 1, 2, 3, 4 }, benchmark);

                var testImage = repo.GetTestImage(1, "/this/that");
                Assert.Equal(new byte[] { 1, 2, 3 ,4 }, testImage);
            }
        }

        [Fact]
        public void ClaimTestAndMarkSuccess()
        {
            using (var connProvider = new SqliteConnectionProvider("claimtest"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveBenchmark("prod", "/this/that", "mytestimage", new byte[] { 1, 2, 3 });
                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3 });

                var claimedImage = repo.ClaimNextTestImage("test");
                
                Assert.Equal(1, claimedImage.TestRunId);
                Assert.Equal("/this/that", claimedImage.Path);

                var benchmark = repo.GetBenchmark(claimedImage.Application, claimedImage.Path);
                var testImage = repo.GetTestImage(claimedImage.TestRunId, claimedImage.Path);

                Assert.Equal(new byte[] { 1, 2, 3 }, benchmark);
                Assert.Equal(new byte[] { 1, 2, 3 }, testImage);

                repo.MarkTestImageSuccess(1, "/this/that");

                Assert.Null(repo.ClaimNextTestImage("test"));

                var testRun = repo.GetTestRun(1);
                Assert.Equal("Submitted", testRun.TestImages[0].State);
            }
        }

        [Fact]
        public void ClaimTestAndMarkDifference()
        {
            using (var connProvider = new SqliteConnectionProvider("claimtest"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveBenchmark("prod", "/this/that", "mytestimage", new byte[] { 1, 2, 3 });
                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3 });

                var claimedImage = repo.ClaimNextTestImage("test");
                
                Assert.Equal(1, claimedImage.TestRunId);
                Assert.Equal("/this/that", claimedImage.Path);

                repo.MarkTestImageDifferent(1, "/this/that", new byte[] { 2, 3 });

                Assert.Null(repo.ClaimNextTestImage("test"));

                var testRun = repo.GetTestRun(1);
                Assert.Equal("Different", testRun.TestImages[0].State);
                
                var diff = repo.GetDifferenceImage(1, "/this/that");
                Assert.Equal(new byte[] { 2, 3 }, diff);
            }
        }

        [Fact]
        public void ClaimTestAndMarkError()
        {
            using (var connProvider = new SqliteConnectionProvider("claimtest"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveBenchmark("prod", "/this/that", "mytestimage", new byte[] { 1, 2, 3 });
                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3 });

                var claimedImage = repo.ClaimNextTestImage("test");
                
                Assert.Equal(1, claimedImage.TestRunId);
                Assert.Equal("/this/that", claimedImage.Path);

                repo.MarkTestImageError(1, "/this/that", "Something went wrong");

                Assert.Null(repo.ClaimNextTestImage("test"));

                var testRun = repo.GetTestRun(1);
                Assert.Equal("Error", testRun.TestImages[0].State);
                Assert.Equal("Something went wrong", testRun.TestImages[0].Error);
            }
        }

        [Fact]
        public void TestImageWithoutBenchmarkCantBeClaimed()
        {
            using (var connProvider = new SqliteConnectionProvider("claimtest"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3 });

                Assert.Null(repo.ClaimNextTestImage("test"));

                var testRun = repo.GetTestRun(1);
                Assert.Equal(ImageState.NoBenchmark, testRun.TestImages[0].State);
            }
        }

        [Fact]
        public void DeleteTestRun()
        {
            using (var connProvider = new SqliteConnectionProvider("claimtest"))
            {
                var repo = new TestRepository(connProvider);
                repo.EnsureSchema();
                repo.CreateTestRun("prod");

                repo.SaveReferenceImage(1, "/this/that", "refImage", new byte[] { 1, 2, 3 });
                repo.SaveTestImage(1, "/this/that", "mytestimage", new byte[] { 1, 2, 3 });

                repo.DeleteTestRun(1);

                Assert.Null(repo.GetTestRun(1));
                Assert.Null(repo.GetTestImage(1, "/this/that"));
                Assert.Null(repo.GetReferenceImage(1, "/this/that"));
            }
        }
    }
}
