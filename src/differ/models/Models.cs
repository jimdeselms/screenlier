using System;

namespace Screenly.Differ.Models
{
    public enum ImageState {
        Submitted = 0,
        Running = 1,
        NoBenchmark = 2,
        Success = 3,
        Different = 4,
        Error = 5
    }
    public class ClaimedTestImageInfo
    {
        public int TestRunId { get; set; }
        public string Path { get; set; }
        public string Application { get; set; }
    }
}