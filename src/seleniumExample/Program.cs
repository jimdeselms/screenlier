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
        public static void Main()
        {
            var tester = new HtmlUnitTester();

            var factory = new TestRunFactory(new Screenly.Client.Client());

            using (var testRun = factory.StartTestRun("sitecore-production").Result)
            {
                tester.Test(testRun);
            }

        }
    }

    public class HtmlUnitTester
    {
        private Dictionary<string, DriverOptions> _drivers;
        private readonly Uri _gridUri = new Uri("http://127.0.0.1:4444/wd/hub");

        public HtmlUnitTester()
        {
            _drivers = new Dictionary<string, DriverOptions>();

            _drivers["chrome"] = new ChromeOptions();
            _drivers["firefox"] = new FirefoxOptions();
        }

        private IWebDriver CreateDriver(string driver)
        {
            return new RemoteWebDriver(_gridUri, _drivers[driver]);
        }

        public void Test(TestRun testRun)
        {
            var tasks = new List<Task>();

            foreach (var pair in _drivers)
            {
                tasks.Add(Task.Factory.StartNew(() => RunTestsForDriver(testRun, pair.Key, pair.Value)));
            }

            Console.WriteLine("Waiting for tasks to complete");

            Task.WaitAll(tasks.ToArray());
        }

        private double GetScreenHeight(IJavaScriptExecutor js)
        {
            return Convert.ToDouble(js.ExecuteScript("return window.innerHeight"));
        }

        private double GetScrollPosition(IJavaScriptExecutor js)
        {
            return Convert.ToDouble(js.ExecuteScript("return window.scrollY"));
        }

        private void WaitUntil(IWebDriver driver, string booleanCondition, bool failOnTimeout=true)
        {
            var start = DateTime.UtcNow;
            while (!(bool)(((IJavaScriptExecutor)driver).ExecuteScript($"return {booleanCondition}") ?? false))
            {
                if (DateTime.UtcNow - start > TimeSpan.FromSeconds(5))
                {
                    if (failOnTimeout)
                    {
                        throw new Exception($"Timed out waiting for {booleanCondition}");
                    }
                }
                Thread.Sleep(100);
            }
        }

        private void RunTestsForDriver(TestRun testRun, string suffix, DriverOptions options)
        {
            RunSingleTest(
                _gridUri, 
                options,
                testRun, 
                suffix, 
                "https://www.vistaprint.com/signs-posters/posters", 
                "/signs-posters/posters", 
                "Posters GPP", 
                new [] { "main-nav", "inline-ratings-container"},
                new [] { "product-configurator", "short-description-container", "product-image-component", "breadcrumbs" });

            RunSingleTest(
                _gridUri, 
                options,
                testRun, 
                suffix, 
                "https://www.vistaprint.com/business-cards/standard", 
                "/business-cards/standard", 
                "Business Cards GPP", 
                new [] { "main-nav", "inline-ratings-container"},
                new [] { "product-configurator", "short-description-container", "product-image-component", "breadcrumbs", });

            RunSingleTest(
                _gridUri, 
                options,
                testRun, 
                suffix, 
                "https://www.vistaprint.com/holiday/christmas-cards", 
                "/holiday/christmas-cards", 
                "Holiday Cards GPP", 
                new [] { "main-nav", "inline-ratings-container"},
                new [] { "product-configurator", "short-description-container", "product-image-component", "breadcrumbs" });
        }
        private void RunSingleTest(Uri uri, DriverOptions options, TestRun testRun, string suffix, string url, string path, string name, string[] classesToHide, string[] classesToRead)
        {
            using (var driver = new RemoteWebDriver(uri, options))
            {
                try
                {
                    var fullPath = $"{path}/{suffix}";
                    Console.WriteLine(fullPath);

                    driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
                    driver.Manage().Window.Size = new System.Drawing.Size(967, 1000);
                    driver.Navigate().GoToUrl(url);

                    System.Threading.Thread.Sleep(1000);

                    WaitUntil(driver, "document.readyState === 'complete'");

                    var js = (IJavaScriptExecutor)driver;

                    foreach (var classToHide in classesToHide)
                    {
                        WaitUntil(driver, $"document.getElementsByClassName('{classToHide}').length > 0");
                        js.ExecuteScript($"[...document.getElementsByClassName('{classToHide}')].forEach(e => e.style = 'display:none');");
                        System.Threading.Thread.Sleep(25);
                    }

                    var screenshotTaker = ((ITakesScreenshot)driver);

                    using (var tempFile = new TemporaryFile())
                    {
                        var screenshot = screenshotTaker.GetScreenshot();

                        var png = tempFile.FileName + ".png";
                        screenshot.SaveAsFile(png, ScreenshotImageFormat.Png);
                        testRun.UploadReferenceImage($"{path}/{suffix}", File.ReadAllBytes(png), name);

                        Console.WriteLine("    reference image");
                    }

                    foreach (var classToRead in classesToRead)
                    {
                        try
                        {
                            Console.WriteLine($"  Reading class {fullPath}/{classToRead}");
                            WaitUntil(driver, $"document.getElementsByClassName('{classToRead}').length > 0");
                            js.ExecuteScript($"document.getElementsByClassName('{classToRead}')[0].scrollIntoView()");
                        
                            System.Threading.Thread.Sleep(25);

                            using (var tempFile = new TemporaryFile())
                            {
                                var screenshot = screenshotTaker.GetScreenshot();
                                var png = tempFile.FileName + ".png";

                                screenshot.SaveAsFile(png, ScreenshotImageFormat.Png);

                                var remoteWebDriver = (RemoteWebDriver)driver;

                                var el = remoteWebDriver.FindElementsByClassName(classToRead).FirstOrDefault();

                                var scrollPosition = GetScrollPosition(js);
                                var windowHeight = GetScreenHeight(js);

                                var top = el.Location.Y - scrollPosition;
                                var bottom = top + el.Size.Height;
                                var windowBottom = windowHeight + scrollPosition;
                                var visibleBottom = Math.Min(windowBottom, bottom);
                                var height = (int)(visibleBottom - top);

                                using (var image = Image.Load(png))
                                {
                                    var cropRect = new Rectangle(el.Location.X, (int)(el.Location.Y - scrollPosition), el.Size.Width, height);
                                    image.Mutate(x => 
                                        x.Crop(cropRect)
                                    );

                                    using (var fs = new FileStream(png, FileMode.Create, FileAccess.Write))
                                    {
                                        image.SaveAsPng(fs);
                                    }

                                    testRun.UploadTestImage($"{path}/{suffix}/{classToRead}", File.ReadAllBytes(png), classToRead);
                                    Console.WriteLine("    Uploaded image");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            testRun.UploadTestError($"{path}/{suffix}/{classToRead}", ex.ToString(), classToRead);
                            Console.WriteLine("    ERROR: " + ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    testRun.UploadTestError($"{path}/{suffix}", ex.ToString());
                }
            }
        }
    }
}
