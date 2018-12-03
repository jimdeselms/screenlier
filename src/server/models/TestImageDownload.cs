using System;

namespace Screenly.Server.Models
{
    public class TestImageDownload
    {
        public string App { get; set; }
        public int TestRunId { get; set; }
        public string Path { get; set; }
        public byte[] Benchmark { get; set; }
        public byte[] Data { get; set; }
    }
}