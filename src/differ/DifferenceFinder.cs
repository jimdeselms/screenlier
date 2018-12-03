using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Primitives;
using System.IO;

namespace Screenly.Differ
{
    public class DifferenceFinder
    {
        private readonly string _saveToFile;

        public DifferenceFinder(string saveToFile=null)
        {
            _saveToFile = saveToFile;
        }
        
        public byte[] FindDifferences(byte[] benchmark, byte[] testImage)
        {
            if (benchmark.Length == testImage.Length && AreBinarySame(benchmark, testImage))
            {
                return null;
            }

            return BuildDifferenceImage(benchmark, testImage);
        }

        private bool AreBinarySame(byte[] benchmark, byte[] testImage)
        {
            return true;
        }

        private const int HIGHLIGHT_SIZE = 10;

        // This will do something clever with an image library to generate an image that shows the difference
        private byte[] BuildDifferenceImage(byte[] benchmarkBits, byte[] testBits)
        {
            using (var benchmarkImage = Image.Load(benchmarkBits))
            using (var testImage = Image.Load(testBits))
            using (var mask = new Image<Rgba32>(null, Math.Min(benchmarkImage.Width, testImage.Width), Math.Min(benchmarkImage.Height, testImage.Height)))
            using (var redLayer = new Image<Rgba32>(null, Math.Min(benchmarkImage.Width, testImage.Width), Math.Min(benchmarkImage.Height, testImage.Height)))
            {
                redLayer.Mutate(i => i.FillPolygon(Brushes.Solid(new Rgba32(255, 255, 255, 0)), new PointF(0, 0), new PointF(mask.Width, 0), new PointF(mask.Width, mask.Height), new PointF(0, mask.Height)));
                mask.Mutate(i => i.FillPolygon(Brushes.Solid(new Rgba32(255, 255, 255, 120)), new PointF(0, 0), new PointF(mask.Width, 0), new PointF(mask.Width, mask.Height), new PointF(0, mask.Height)));

                var opaque = Brushes.Solid(new Rgba32(255, 255, 255, 0));
                var red = Brushes.Solid(new Rgba32(255, 0, 0, 255));

                var maskOptions = new GraphicsOptions { BlenderMode = PixelBlenderMode.Src };
                var diffImageOptions = new GraphicsOptions { BlenderMode = PixelBlenderMode.Darken };

                for (int y = 0; y < mask.Height; y++)
                {
                    for (int x = 0; x < mask.Width; x++)
                    {
                        if (benchmarkImage[x,y] != testImage[x,y])
                        {
                            var rect = new [] { new PointF(x-HIGHLIGHT_SIZE/2, y-HIGHLIGHT_SIZE/2), new PointF(x+HIGHLIGHT_SIZE/2, y-HIGHLIGHT_SIZE/2), new PointF(x+HIGHLIGHT_SIZE/2, y+HIGHLIGHT_SIZE/2), new PointF(x-HIGHLIGHT_SIZE/2, y+HIGHLIGHT_SIZE/2) };
                            mask.Mutate(i => i.FillPolygon(maskOptions, opaque, rect));
                            redLayer.Mutate(i => i.FillPolygon(diffImageOptions, red, rect));

                            x += HIGHLIGHT_SIZE;
                        }
                    }
                }

                testImage.Mutate(i => i.DrawImage(redLayer, PixelBlenderMode.Darken, 0.20f).DrawImage(mask, 1f));

                return SaveImage(testImage);
            }
        }

        private byte[] SaveImage(Image<Rgba32> testImage)
        {
            if (_saveToFile != null)
            {
                using (var stream = new FileStream(_saveToFile, FileMode.Create))
                {
                    testImage.SaveAsPng(stream);
                }

                return File.ReadAllBytes(_saveToFile);
            }
            else
            {
                using (var stream = new MemoryStream())
                {
                    testImage.SaveAsPng(stream);

                    var buff = new byte[stream.Length];
                    stream.GetBuffer().CopyTo(buff, buff.Length);

                    return buff;
                }
            }
        }
    }
}
