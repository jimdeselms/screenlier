using System;

namespace Screenly.Server.Models
{
    public class TestRunSummary
    {
        public int TestRunId { get; set; }
        public string Application { get; set; }
        public DateTime Start { get; set; } 
        public DateTime? End { get; set; }

        public int TestCount { get; set; }
        public int SuccessCount { get; set; }
        public int MissingBenchmarkCount { get; set; }

        public int DifferenceCount { get; set; }
        public int ErrorCount { get; set; }
    }
}