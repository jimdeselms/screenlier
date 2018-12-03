using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Screenly.Server.Models
{
    public enum ImageState {
        Submitted = 0,
        Running = 1,
        NoBenchmark = 2,
        Success = 3,
        Different = 4,
        Error = 5,
    }

    public class TestImage
    {
        public string Name { get; set; }
        public string Path { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ImageState State { get; set; }
        public DateTime? CompareStart { get; set; }
        public string Error { get; set; }
    }

    public class ClaimedTestImageInfo
    {
        public string Application { get; set; }
        public int TestRunId { get; set; }
        public string Path { get; set; }
    }
}