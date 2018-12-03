using System.Net.Http;
using Screenly.Differ.Models;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Screenly.Differ
{
    public class DifferClient
    {
        private static readonly string BASE_URL = System.Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";
        private static readonly string IMAGE_BASE_URL = $"{BASE_URL}/images";
        private static readonly string API_BASE_URL = $"{BASE_URL}/api/v1";
        private readonly HttpClient _client = new HttpClient();

        public DifferClient()
        {
            _client = new HttpClient();
        }

        public async Task<ClaimedTestImageInfo> ClaimImage(string claimedBy)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimage/imageclaim/{claimedBy}");

            using (var response = await _client.SendAsync(request))
            {
                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<ClaimedTestImageInfo>(json);
            }
        }

        public async Task PostSuccess(int testRunId, string path)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimage/success/{testRunId}/{path}");

            using (await _client.SendAsync(request))
            {
            }
        }

        public async Task PostError(int testRunId, string path, string error)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimage/error/{testRunId}/{path}");
            request.Content = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(error));

            using (await _client.SendAsync(request))
            {
            }
        }

        public async Task PostDifference(int testRunId, string path, byte[] difference)
        {
            path = path.TrimStart('/');

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimage/different/{testRunId}/{path}");
            request.Content = new ByteArrayContent(difference);

            using (await _client.SendAsync(request))
            {
            }
        }
        public async Task<byte[]> GetBenchmark(string application, string path)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new System.Uri($"{IMAGE_BASE_URL}/benchmark/{application}/{path}");

            using (var response = await _client.SendAsync(request))
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<byte[]> GetTestImage(int testRunId, string path)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new System.Uri($"{IMAGE_BASE_URL}/testimage/{testRunId}/{path}");

            using (var response = await _client.SendAsync(request))
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
    }
}