using System;
using System.Threading.Tasks;

namespace Screenly.Client
{
    public class TestRun : IDisposable
    {
        private Client _client;
        private readonly int _testRunId;

        internal TestRun(Client client, int testRunId)
        {
            _client = client;
            _testRunId = testRunId;
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.EndTestRun(_testRunId).Wait();
                _client = null;
            }
        }

        public void EndTestRun()
        {
            Dispose();
        }

        public Task UploadReferenceImage(string path, byte[] data, string name = null)
        {
            return _client.UploadReferenceImage(_testRunId, path, name, data);
        }

        public Task UploadTestImage(string path, byte[] data, string name=null)
        {
            return _client.UploadTestImage(_testRunId, path, name, data);
        }

        public Task UploadTestError(string path, string message, string name=null)
        {
            return _client.UploadTestError(_testRunId, path, name, message);
        }
    }
}