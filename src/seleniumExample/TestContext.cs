using OpenQA.Selenium.Remote;
using Screenly.Client;

namespace Screenly.SeleniumExample
{
    public class TestContext
    {
        public RemoteWebDriver Driver { get; }
        public TestRun TestRun { get; }
        public string Name { get; }

        public TestContext(RemoteWebDriver webDriver, TestRun testRun, string name)
        {
            Driver = webDriver;
            TestRun = testRun;
            Name = name;
        }
    }
}