using System.Threading.Tasks;

namespace Screenly.Client
{
    public class TestRunFactory
    {
        private readonly Client _client;

        public TestRunFactory(Client client)
        {
            _client = client;
        }

        public async Task<TestRun> StartTestRun(string appName)
        {
            int id = await _client.StartTestRun(appName);
            return new TestRun(_client, id);
        }
    }
}