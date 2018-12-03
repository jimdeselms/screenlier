using System;
using System.Collections.Generic;
using System.IO;
using Screenly.Server.Models;

namespace Screenly.Server.Data
{
    public interface ITestRepository
    {
        void EnsureSchema();

        IEnumerable<TestRunSummary> GetTestRunSummaries(string appName);
        int CreateTestRun(string application);
        void SetTestRunEnd(int id, DateTime end);

        TestRun GetTestRun(int id);

        void DeleteTestRun(int testRunId);

        void SaveBenchmark(string application, string path, string name, byte[] image);
        void SaveReferenceImage(int testRun, string path, string name, byte[] image);
        void SaveTestImage(int testRun, string path, string name, byte[] image);
        void SaveTestImageError(int testRun, string path, string name, string error);

        void PromoteBenchmark(int testRun, string path);

        void MarkTestImageDifferent(int testRun, string path, byte[] diff);
        void MarkTestImageError(int testRun, string path, string error);
        void MarkTestImageSuccess(int testRun, string path);
        byte[] GetBenchmark(string application, string path);
        byte[] GetReferenceImage(int testRun, string path);
        byte[] GetTestImage(int testRun, string path);
        byte[] GetTestDifference(int testRun, string path);

        ClaimedTestImageInfo ClaimNextTestImage(string claimedBy);
    }
}