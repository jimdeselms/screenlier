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

namespace Screenly.SeleniumExample
{
    class Program
    {
        public static void Main(string[] args)
        {
            var environment = (args.Length > 0) ? args[0] : "prod";
            var proxy = (args.Length > 1) ? args[1] : null;

            var factory = new TestRunFactory(new Screenly.Client.Client());

            using (var testRun = factory.StartTestRun("sitecore-production").Result)
            {
                var tester = new TestRunner(testRun, "http://127.0.0.1:4444/wd/hub");

                var testClass = new Tests(environment, proxy);

                tester.Test(testClass);
            }
        }
    }
}
