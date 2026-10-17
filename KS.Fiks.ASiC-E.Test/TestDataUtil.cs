using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace KS.Fiks.ASiC_E.Test
{
    public static class TestDataUtil
    {

        public static Stream ReadValidAsiceCadesFromResource()
        {
            return ReadResource("cades-valid.asice");
        }

        public static Stream ReadResource(string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().Single(str =>
                str.EndsWith(resource, StringComparison.CurrentCulture));
            return assembly.GetManifestResourceStream(resourceName);
        }

        public static List<ZipEntry> ReadEntries(byte[] package)
        {
            using var zipArchive = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read);
            return zipArchive.Entries.Select(entry =>
            {
                using var entryStream = entry.Open();
                using var buffer = new MemoryStream();
                entryStream.CopyTo(buffer);
                return new ZipEntry(entry.FullName, buffer.ToArray());
            }).ToList();
        }

        public static byte[] WriteEntries(IEnumerable<ZipEntry> entries)
        {
            using var zipStream = new MemoryStream();
            using (var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                foreach (var entry in entries)
                {
                    using var entryStream = zipArchive.CreateEntry(entry.Name).Open();
                    entryStream.Write(entry.Data, 0, entry.Data.Length);
                }
            }

            return zipStream.ToArray();
        }
    }
}