using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using teramako.IO.Tar;

namespace Sample
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1) {
				Console.Error.WriteLine("No arguments.");
				System.Environment.Exit(1);
			}
			try
			{
				using (var fs = File.OpenRead(args[0]))
				using (var gs = new GZipStream(fs, CompressionMode.Decompress))
				using (var tar = new TarArchiveReader(gs))
				{
					foreach (var tarEntry in tar.GetEntries())
					{
						if (tarEntry.Type.HasFlag(TarEntryType.Regular))
						{
							Console.WriteLine(tarEntry);
							Console.WriteLine("# =====================================");
							using (var ts = new StreamReader(tarEntry, Encoding.UTF8))
							{
								string line;
								while ((line = ts.ReadLine()) != null)
								{
									Console.WriteLine(line);
								}
							}
							Console.WriteLine("# =====================================");
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.ToString());
			}
		}
	}
}
