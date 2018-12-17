using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Screenly.Server.Models
{
    public static class ImageState {
        public static readonly string Submitted = "Submitted";
        public static readonly string Running = "Running";
        public static readonly string NoBenchmark = "NoBenchmark";
        public static readonly string Success = "Success";
        public static readonly string Different = "Different";
        public static readonly string Error = "Error";
    }

    public class TestImage
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string State { get; set; }
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