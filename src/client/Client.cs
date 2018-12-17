using System;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace Screenly.Client
{
    public class Client
    {
        private static readonly string BASE_URL = System.Environment.GetEnvironmentVariable("SCREENLY_SERVER_BASE_URL") ?? "http://localhost:5000";
        private static readonly string API_BASE_URL = $"{BASE_URL}/api/v1";
        private readonly HttpClient _client = new HttpClient();

        public Client()
        {
            Console.WriteLine("Using screenly server at " + BASE_URL);
            _client = new HttpClient();
        }

        public async Task<int> StartTestRun(string app)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testrun/{app}");

            using (var response = await _client.SendAsync(request))
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();

                return int.Parse(System.Text.Encoding.UTF8.GetString(bytes));
            }
        }

        public async Task EndTestRun(int id)
        {
            HttpResponseMessage foo;
            var request = new HttpRequestMessage();
            request.Method = new HttpMethod("PATCH");
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testrun/{id}?complete=true");

            using (await _client.SendAsync(request))
            {
            }
        }

        public async Task UploadReferenceImage(int testRunId, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/refimage/{testRunId}/{path}?name={name}");
            request.Content = new ByteArrayContent(data);

            using (await _client.SendAsync(request))
            {
            }
        }

        public async Task UploadTestImage(int testRunId, string path, string name, byte[] data)
        {
            path = path.TrimStart('/');

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimage/{testRunId}/{path}?name={name}");
            request.Content = new ByteArrayContent(data);

            using (await _client.SendAsync(request))
            {
            }
        }

        public async Task UploadTestError(int testRunId, string path, string name, string message)
        {
            path = path.TrimStart('/');

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Put;
            request.RequestUri = new System.Uri($"{API_BASE_URL}/testimageerror/{testRunId}/{path}?name={name}");
            request.Content = new StringContent(message);

            using (await _client.SendAsync(request))
            {
            }
        }
    }
}