using System;
using System.Threading.Tasks;

namespace Screenly.Differ
{
    class Program
    {
        static void Main()
        {
            var differenceFinder = new DifferenceFinder("c:\\temp.png");

            Run(differenceFinder, testMode: false).Wait();
        }

        static async Task Run(DifferenceFinder differenceFinder, bool testMode=false)
        {
            var client = new DifferClient();
            bool waiting = false;

            while (true)
            {
                try
                {
                    var testImage = await client.ClaimImage("jim");
                    if (testImage == null)
                    {
                        if (!waiting)
                        {
                            waiting = true;
                            Console.Write("No images found. Waiting.");
                        }
                        else
                        {
                            Console.Write(".");
                        }
                        System.Threading.Thread.Sleep(5000);
                        continue;
                    }

                    if (waiting)
                    {
                        waiting = false;
                        Console.WriteLine();
                    }

                    Console.WriteLine($"image {testImage.TestRunId}/{testImage.Path}: claimed");

                    try
                    {
                        var benchmarkBits = await client.GetBenchmark(testImage.Application, testImage.Path);
                        var testImageBits = await client.GetTestImage(testImage.TestRunId, testImage.Path);

                        var difference = differenceFinder.FindDifferences(benchmarkBits, testImageBits);

                        if (testMode)
                        {
                            Console.WriteLine("Exiting - test mode");
                            break;
                        }

                        if (difference == null)
                        {
                            Console.WriteLine($"image {testImage.TestRunId}/{testImage.Path}: SUCCESS");
                            await client.PostSuccess(testImage.TestRunId, testImage.Path);
                        }
                        else
                        {
                            Console.WriteLine($"image {testImage.TestRunId}/{testImage.Path}: DIFFERENT");
                            await client.PostDifference(testImage.TestRunId, testImage.Path, difference);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("IMAGE ERROR: " + ex.ToString());
                        await client.PostError(testImage.TestRunId, testImage.Path, ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.ToString());
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }
    }
}
