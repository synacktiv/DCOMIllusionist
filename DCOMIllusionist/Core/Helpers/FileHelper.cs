using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCOMIllusionist.Core.Helpers
{
    public static class FileHelper
    {
        public static byte[] ReadFileBytes(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            return File.ReadAllBytes(filePath);
        }

        public static string CompressAndEncodeToBase64(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return Convert.ToBase64String(outputStream.ToArray());
            }
        }
    }
}
