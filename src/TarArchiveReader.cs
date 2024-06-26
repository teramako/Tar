﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Compression;

namespace teramako.IO.Tar
{
    public class TarArchiveReader : IDisposable
    {
        [Conditional("DEBUG")]
        static private void Dump(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("TarArchiveReader::" + message);
            Console.ForegroundColor = color;
        }
        private const int BLOCK_SIZE = 512;
        private readonly Stream BaseStream = null;
        #region IDispose Implemention
        private bool isDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected void Dispose(bool disposing)
        {
            if (isDisposed) return;
            Dump(string.Format("Dispose({0})", disposing));
            if (disposing)
            {
                BaseStream.Dispose();
            }
            isDisposed = true;
        }
        #endregion
        #region Constructor
        /// <summary>
        /// for read the <paramref name="stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        public TarArchiveReader(Stream stream) : this(stream, Encoding.UTF8)
        {
        }
        /// <summary>
        /// Read or Create a tar archvie
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="mode"></param>
        /// <param name="entryNameEncoding">Tarエントリのファイル名やリンク名の文字エンコーディング</param>
        public TarArchiveReader(Stream stream, Encoding entryNameEncoding)
        {
            BaseStream = stream;
            EntryNameEncoding = entryNameEncoding;
        }
        public TarArchiveReader(string Path) : this(new FileInfo(Path))
        {
        }
        public TarArchiveReader(FileInfo file)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException("Not Found", file.FullName);
            }
            var fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Extension == ".gz")
            {
                var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress, true);
                BaseStream = gzipStream;
            }
            else
            {
                BaseStream = fileStream;
            }
            EntryNameEncoding = Encoding.UTF8;

        }
        #endregion
        public Encoding EntryNameEncoding { get; set; }
        /// <summary>
        /// Enumerable tar entries.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TarEntry> GetEntries ()
        {
            Dump("GetEntries");
            long position = 0;
            while (BaseStream.CanRead)
            {
                var entry = new TarEntry(BaseStream, EntryNameEncoding);
                if (entry.Header.Type == TarEntryType.EndOfEntry)
                {
                    Dump("GetEntries Reach EndBlock");
                    break;
                }
                position += entry.HeaderBlockCount;
                yield return entry;
                position += SeekToEnd(entry.Position, entry.Length);
                Dump(string.Format("GetEntries: End Content(Position={0}[0x{1:x8}])", position, position*BLOCK_SIZE));
                entry.Dispose();
            }
            Dump("End GetEntries");
            BaseStream.Close();
        }
        /// <summary>
        /// Seek to the end of the current tar entry.
        /// with Stream.Seek() if available, otherwise Stream.Read()
        /// </summary>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <returns>Block counts</returns>
        private long SeekToEnd (long position, long length)
        {
            if (length == 0) return 0;
            long endPosition = length % BLOCK_SIZE == 0 ? length : length + (BLOCK_SIZE - (length % BLOCK_SIZE));
            long offset = endPosition - position;
            if (BaseStream.CanSeek)
            {
                BaseStream.Seek(offset, SeekOrigin.Current);
                Dump(string.Format("SeekToEnd: Seek({0})", offset));
            }
            else
            {
                var buf = new byte[BLOCK_SIZE];
                var mod = (int)(offset % BLOCK_SIZE);
                var blockCount = (offset - mod) / BLOCK_SIZE;
                BaseStream.Read(buf, 0, mod);
                Dump(string.Format("SeekToEnd: Read({0} bytes)", mod));
                for (long i = 0; i < blockCount; i++)
                {
                    BaseStream.Read(buf, 0, BLOCK_SIZE);
                    Dump(string.Format("SeekToEnd: Read({0} bytes)", BLOCK_SIZE));
                }
            }
            return endPosition / BLOCK_SIZE;
        }
    }
}
