using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenQA.Selenium.Firefox;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.ImageSharp.PixelFormats;
using OpenQA.Selenium.Support.UI;

using Screenly.Client;
using OpenQA.Selenium.Interactions;
using System.Threading;
using System.Reflection;

namespace Screenly.SeleniumExample
{
   public class TestRunner
   {
        private readonly Dictionary<string, DriverOptions> _drivers;
        private readonly Uri _gridUri;
        private readonly TestRun _testRun;

        private readonly List<TestDetails> _tests = new List<TestDetails>();

        public TestRunner(TestRun testRun, string url)
        {
            _drivers = new Dictionary<string, DriverOptions>();

            _drivers["chrome"] = new ChromeOptions();
//            _drivers["firefox"] = new FirefoxOptions();

            _gridUri = new Uri(url);
            
            _testRun = testRun;
        }

        private RemoteWebDriver CreateDriver(string driver)
        {
            return new RemoteWebDriver(_gridUri, _drivers[driver]);
        }

        public void Test(params object[] testClasses)
        {
            Console.WriteLine("Discovering tests");
            foreach (var testClass in testClasses)
            {
                var type = testClass.GetType();
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    var testAttr = method.GetCustomAttributes().FirstOrDefault(a => a is TestAttribute);
                    if (testAttr != null)
                    {
                        ValidateMethod(method);
                        var testName = ((TestAttribute)testAttr).Name ?? method.Name;
                        var testDetails = new TestDetails(testName, context => (bool)method.Invoke(testClass, new [] { context }));
                        _tests.Add(testDetails);
                    }
                }
            }
            Console.WriteLine("Waiting for tasks to complete");
            RunTests().Wait();
        }

        private static void ValidateMethod(MethodInfo method)
        {
            if (method.ReturnType != typeof(bool) && method.ReturnType != typeof(void))
            {
                throw new Exception($"{method.Name} is not a valid test method because it doesn't return bool or void");
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || parameters[0].ParameterType != typeof(TestContext))
            {
                throw new Exception($"{method.Name} is not a valid test method because it doesn't take one TestContext parameter");
            }
        }

        private int _runningTests = 0;
        private object _runningTestLock = new object();
        private const int MAX_CONCURRENT_TESTS = 5;
        private Task RunTests()
        {
            var tasks = new List<Task>();

            foreach (var test in _tests)
            {
                foreach (var pair in _drivers)
                {
                    tasks.Add(Task.Factory.StartNew(() => RunTestForDriver(pair.Key, pair.Value.ToCapabilities(), test)));
                }
            }

            return Task.WhenAll(tasks.ToArray());
        }

        private void GetTestSlot()
        {
            while (true)
            {
                lock (_runningTestLock)
                {
                    if (_runningTests < MAX_CONCURRENT_TESTS)
                    {
                        _runningTests++;
                        return;
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void RelinquishSlot()
        {
            lock (_runningTestLock)
            {
                --_runningTests;
            }
        }

        private void RunTestForDriver(string driverName, ICapabilities capabilities, TestDetails test)
        {
            try
            {
                // We'll throttle this so that we don't overwhelm the grid.
                GetTestSlot();


                var testName = $"{test.Name}/{driverName}";

                try
                {
                    // Each function should return the number of errors that occurred when trying to download images.
                    using (var driver = new RemoteWebDriver(_gridUri, capabilities, TimeSpan.FromSeconds(300)))
                    {
                        var context = new TestContext(driver, _testRun, testName);
                        object result = test.Run(context);

                        if (result == null || (result is bool && ((bool)result)))
                        {
                            // We'll consider the test successful because they either returned true or they returned void.
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // If something goes wrong, just ait a bit and try again.
                    Console.WriteLine("Error executing test: " + ex.ToString());
                    Console.WriteLine("RETRYING");

                    Thread.Sleep(5000);
                }

                using (var driver = new RemoteWebDriver(_gridUri, capabilities, TimeSpan.FromSeconds(300)))
                {
                    // The second time around, if anything goes wrong, we'll just let it stand.
                    var context = new TestContext(driver, _testRun, testName);
                    test.Run(context);
                }
            }
            finally
            {
                RelinquishSlot();
            }
        }

        private class TestDetails
        {
            public string Name { get; }
            public Func<TestContext, object> Run { get; }

            public TestDetails(string name, Func<TestContext, object> test)
            {
                Name = name;
                Run = test;
            }
        }
    }
}
