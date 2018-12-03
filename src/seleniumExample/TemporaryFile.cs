using System;
using System.IO;

namespace Screenly.SeleniumExample
{
    internal class TemporaryFile : IDisposable
    {
        public string FileName { get; private set; }

        public TemporaryFile()
        {
            FileName = Path.GetTempFileName();
        }

        public void Dispose()
        {
            if (FileName != null)
            {
                File.Delete(FileName);
                FileName = null;
            }
        }
    }
}