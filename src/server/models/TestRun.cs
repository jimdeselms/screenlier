using System;

namespace Screenly.Server.Models
{
    public class TestRun
    {
        public int TestRunId { get; set; }
        public string Application { get; set; }

        public DateTime Start { get; set; }
        public DateTime? End { get; set; }

        public TestImage[] TestImages { get; set; }
        public ReferenceImage[] ReferenceImages { get; set; }
    }
}