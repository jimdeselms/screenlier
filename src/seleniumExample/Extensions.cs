using System;
using System.Threading;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Screenly.Client;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.Primitives;
using SixLabors.ImageSharp.Processing;

namespace Screenly.SeleniumExample
{
    public static class Extensions
    {
        private static int SCREENSHOT_INDEX = 1;

        public static void WaitUntil(this RemoteWebDriver driver, string booleanCondition, TimeSpan timeout, bool failOnTimeout=true)
        {
            var start = DateTime.UtcNow;
            int currWaitMs = 100;
            while (true)
            {
                try
                {
                    if (driver.EvaluateJavascript<bool>(booleanCondition))
                    {
                        return;
                    }
                }
                catch
                {
                    // This wait until is particularly flaky; so we'll just swallow this error.
                }

                if (DateTime.UtcNow - start > timeout)
                {
                    if (failOnTimeout)
                    {
                        var screenshotFile = $"c:\\temp{SCREENSHOT_INDEX.ToString()}.png";

                        Console.WriteLine($"TIMEOUT: Look at {screenshotFile} to see what's going on");

                        // This timed out; let's take a picture so we can see what's going on.
                        driver.GetScreenshot().SaveAsFile(screenshotFile, ScreenshotImageFormat.Png);

                        Console.WriteLine("SCREENSHOT SAVED");

                        SCREENSHOT_INDEX++;
                        throw new Exception($"Timed out waiting for {booleanCondition}");
                    }
                }
                Thread.Sleep(currWaitMs);
                currWaitMs += 100;
            }
        }

        public static void RemoveClasses(this RemoteWebDriver driver, params string[] classesToHide)
        {
            foreach (var classToHide in classesToHide)
            {
                driver.WaitUntil($"document.getElementsByClassName('{classToHide}').length > 0", TimeSpan.FromSeconds(2));
                driver.ExecuteScript($"[...document.getElementsByClassName('{classToHide}')].forEach(e => e.style = 'display:none');");
                System.Threading.Thread.Sleep(25);
            }
        }

        public static void HideClasses(this RemoteWebDriver driver, params string[] classesToHide)
        {
            foreach (var classToHide in classesToHide)
            {
                driver.WaitUntil($"document.getElementsByClassName('{classToHide}').length > 0", TimeSpan.FromSeconds(2));
                driver.ExecuteScript($"[...document.getElementsByClassName('{classToHide}')].forEach(e => e.style = 'display:hidden');");
                System.Threading.Thread.Sleep(25);
            }
        }

        public static T EvaluateJavascript<T>(this RemoteWebDriver driver, string js)
        {
            var result = driver.ExecuteScript("return " + js);
            return (T)Convert.ChangeType(result, typeof(T));
        }
        
        public static void GoToUrl(this RemoteWebDriver driver, string url, int width=967, int height = 1000)
        {
            Console.WriteLine("Going to " + url);
            driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            driver.Manage().Window.Size = new System.Drawing.Size(967, 1000);
            driver.Navigate().GoToUrl(url);

            System.Threading.Thread.Sleep(3000);
            driver.WaitUntil("document.readyState === 'complete'", TimeSpan.FromSeconds(15));
        }

        public static void TakeReferenceImage(this RemoteWebDriver driver, TestRun testRun, string path, string name)
        {
            using (var tempFile = new TemporaryFile())
            {
                var screenshot = driver.GetScreenshot();

                var png = tempFile.FileName + ".png";
                screenshot.SaveAsFile(png, ScreenshotImageFormat.Png);
                testRun.UploadReferenceImage(path, File.ReadAllBytes(png), name);

                Console.WriteLine($"    reference image {path}");
            }
        }

        private static void CaptureClass(RemoteWebDriver driver, TestRun testRun, string path, string classToRead)
        {
            Console.WriteLine($"  capturing class image {path}/{classToRead}");
            driver.WaitUntil($"document.getElementsByClassName('{classToRead}').length > 0", TimeSpan.FromSeconds(2));
            driver.ExecuteScript($"document.getElementsByClassName('{classToRead}')[0].scrollIntoView()");
        
            System.Threading.Thread.Sleep(100);

            using (var tempFile = new TemporaryFile())
            {
                var screenshot = driver.GetScreenshot();
                var png = tempFile.FileName + ".png";

                screenshot.SaveAsFile(png, ScreenshotImageFormat.Png);

                var remoteWebDriver = (RemoteWebDriver)driver;

                // FindElements is kind of flaky; instead, wait for the element to actualy be there.
                driver.WaitUntil($"document.getElementsByClassName('{classToRead}').length > 0", TimeSpan.FromSeconds(2));
                var el = remoteWebDriver.FindElementsByClassName(classToRead).FirstOrDefault();

                var scrollPosition = driver.EvaluateJavascript<double>("window.scrollY");
                var windowHeight = driver.EvaluateJavascript<double>("window.innerHeight");

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

            
                    testRun.UploadTestImage($"{path}/{classToRead}", File.ReadAllBytes(png), classToRead);
                }
            }
        }

        public static bool CaptureClasses(this RemoteWebDriver driver, TestRun testRun, string path, params string[] classesToRead)
        {
            bool isSuccess = true;

            foreach (var classToRead in classesToRead)
            {
                try
                {
                    CaptureClass(driver, testRun, path, classToRead);
                    continue;
                }
                catch
                {
                    // First time, let it go.
                }

                // Just wait a wee bit and retry
                Console.WriteLine($"Reading {classToRead} failed the first time. Trying again");
                Thread.Sleep(250);

                try
                {
                    CaptureClass(driver, testRun, path, classToRead);
                    continue;
                }
                catch (Exception ex)
                {
                    testRun.UploadTestError($"{path}/{classToRead}", ex.ToString(), classToRead);
                    Console.WriteLine($"    ERROR downloading {path}/{classToRead}" + ex.ToString());

                    isSuccess = false;
                }
            }
            
            return isSuccess;
        }
    }
}