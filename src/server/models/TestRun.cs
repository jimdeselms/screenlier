using System;

namespace Screenly.Server.Models
{
    public class TestRun
    {
        public int TestRunId { get; set; }
        public string Application { get; set; }

        public DateTime TestStart { get; set; }
        public DateTime? TestEnd { get; set; }

        public TestImage[] TestImages { get; set; }
        public ReferenceImage[] ReferenceImages { get; set; }
    }
}