# Tar
TarArchive Parser

## Usage

```csharp
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using teramako.IO.Tar;

class Program
{
	public void ReadTar(string tgzFilePath)
    {
        var file = new FileInfo(tgzFilePath);
        using (var fileStream = file.OpenRead())
        using (var gzStream = new GZipStream(fileStream, CompressionMode.Decompress)
        using (var tarArchive = new .TarArchiveReader(gzStream, Encoding.UTF8))
        {
            foreach (var tarEntry in tarArchive.GetEntries()) {
                if (!tarEntry.Header.Type.HasFlag(TarEntryType.Regular)) continue;

                Console.WriteLine($"Read {tarEntry.Header.Name}");
                using (var streamReader = new StreamReader(tarEntry, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                }

            }
        }

    }
}

```
