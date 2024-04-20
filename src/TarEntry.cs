using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace teramako.IO.Tar
{
    public class TarEntry : Stream
    {
        [Conditional("DEBUG")]
        static private void Dump(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("TarEntry::" + message);
            Console.ForegroundColor = color;
        }
        #region Constructors
        /// <summary>
        /// Read the tar entry from the <paramref name="baseStream"/>,
        /// if <paramref name="mode"/> is read mode or not specified.
        /// If not, write the tar entry to the <paramref name="baseStream"/>.
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="mode"></param>
        public TarEntry(Stream baseStream) :this(baseStream, Encoding.UTF8)
        {
        }
        public TarEntry(Stream baseStream, Encoding entryEncoding)
        {
            Header = new TarHeader();
            BaseStream = baseStream;
            EntryNameEncoding = entryEncoding;
            Parse();
        }
        #endregion
        public int HeaderBlockCount { get; private set; }
        public Encoding EntryNameEncoding { get; set; }
        #region Stream Implemention
        private Stream BaseStream = null;
        private long position = 0;
        private bool isDisposed = false;
        protected override void Dispose(bool disposing)
        {
            if (isDisposed) { return; }
            Dump(string.Format("Dispose({0})", disposing));
            if (disposing)
            {
                BaseStream = null;
            }
            isDisposed = true;
        }
        public override bool CanWrite { get { return false; } }
        public override bool CanRead {
            get {
                return BaseStream != null && BaseStream.CanRead;
            }
        }
        public override bool CanSeek { get { return false; } }
        public override long Position {
            get { return position; }
            set { throw new NotSupportedException(); }
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
        public override void Flush()
        {
            throw new NotImplementedException();
        }
        public override long Length
        {
            get
            {
                return Header.Size;
            }
        }
        public override int ReadByte()
        {
            var buf = new byte[1];
            var result = Read(buf, 0, 1);
            if (result <= 0)
            {
                return -1;
            }
            return (int)buf[0];
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= Length)
            {
                return 0;
            }
            long readCount = count;
            if (readCount + position > Length)
            {
                readCount = Length - position;
            }
            position += readCount;
            Dump(string.Format("Read(byte[{0}], {1}, {2}) Pos={3}", buffer.Length, offset, count, position));
            return BaseStream.Read(buffer, offset, (int)readCount);
        }

        #endregion
        #region TarEntry Properties and Fields
        private const int NAME_LENGTH = 100;
        private const int MODE_LENGTH = 8;
        private const int UID_LENGTH = 8;
        private const int GID_LENGTH = 8;
        private const int SIZE_LENGTH = 12;
        private const int MTIME_ELNGTH = 12;
        private const int CHECKSUM_LENGTH = 8;
        private const int LINKNAME_LENGTH = 100;
        private const int MAGIC_LENGTH = 6;
        private const int VERSION_LENGTH = 2;
        private const int UNAME_LENGTH = 32;
        private const int GNAME_LENGTH = 32;
        private const int DEVMAJOR_LENGTH = 8;
        private const int DEVMINOR_LENGTH = 8;
        private const int PREFIX_LENGTH = 155;
        public TarHeader Header { get; private set; }
        #endregion

        private const int BLOCK_SIZE = 512;
        /// <summary>
        /// <paramref name="needSize"/>分を含むブロック(512byte)量のデータをBaseStreamから読む
        /// </summary>
        /// <param name="needSize"></param>
        /// <returns></returns>
        private byte[] ReadHeader(int needSize = BLOCK_SIZE)
        {
            int count = needSize / BLOCK_SIZE;
            if (needSize % BLOCK_SIZE > 0)
            {
                count += 1;
            }
            var bufferSize = count * BLOCK_SIZE;
            var buffer = new byte[bufferSize];
            var readSize = BaseStream.Read(buffer, 0, bufferSize);
            if (readSize != bufferSize)
            {
                    throw new TarHeaderParsingException(
                        string.Format("Read data[{0}byte] is too short. Required {1}byte", readSize, bufferSize));
            }
            HeaderBlockCount += count;
            Dump(string.Format("ReadHeader: Read {0} block(s)", count));
            return buffer;
        }
        /// <summary>
        /// Parsing a tar header from the <code>BaseStream</code>.
        /// And set properties to this instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="TarHeaderParsingException"></exception>
        private void Parse()
        {
            TarEntryType currentType = TarEntryType.Incompleted;
            Dump("Start Parsing");
            bool isCompleted;
            do
            {
                switch (currentType)
                {
                    case TarEntryType.GNU_LongName:
                        Dump("Parsing GNU_LongName");
                        Header.Name = ParseString(ReadHeader((int)Header.Size), 0, (int)Header.Size);
                        currentType = TarEntryType.Incompleted;
                        break;
                    case TarEntryType.GNU_LongLink:
                        Dump("Parsing GNU_LongLink");
                        Header.LinkName = ParseString(ReadHeader((int)Header.Size), 0, (int)Header.Size);
                        currentType = TarEntryType.Incompleted;
                        break;
                    case TarEntryType.Incompleted:
                        currentType = ParseStandardHeader();
                        break;
                }

                switch (currentType)
                {
                    case TarEntryType.Incompleted:
                    case TarEntryType.GNU_LongName:
                    case TarEntryType.GNU_LongLink:
                        isCompleted = false;
                        break;
                    case TarEntryType.Unkown:
                        throw new TarHeaderParsingException(
                            string.Format("Unkown tar header type: {0}", Header.Type));
                    case TarEntryType.EndOfEntry:
                        Header.Type = currentType;
                        return;
                    default:
                        isCompleted = true;
                        break;
                }
            } while (!isCompleted);
            Dump("End Parsing");
            Dump(ToString());
        }
        private TarEntryType ParseStandardHeader()
        {
            var buffer = ReadHeader();
            if (buffer[0] == 0) // reached end of tar data.
            {
                Dump("this block is the end of tar data.");
                return TarEntryType.EndOfEntry;
            }
            var offset = 0;
            if (!Header.Type.HasFlag(TarEntryType.GNU_LongName))
            {
                Header.Name = ParseString(buffer, offset, NAME_LENGTH);
            }
            offset += NAME_LENGTH;
            Header.Permission = new Permission((int)ParseOctet(buffer, offset, MODE_LENGTH));
            offset += MODE_LENGTH;
            Header.Uid = (int)ParseOctet(buffer, offset, UID_LENGTH);
            offset += UID_LENGTH;
            Header.Gid = (int)ParseOctet(buffer, offset, GID_LENGTH);
            offset += GID_LENGTH;
            Header.Size = ParseOctet(buffer, offset, SIZE_LENGTH);
            offset += SIZE_LENGTH;
            Header.Mtime = Epoch2Date(ParseOctet(buffer, offset, MTIME_ELNGTH));
            offset += MTIME_ELNGTH;
            Header.Checksum = (int)ParseOctet(buffer, offset, CHECKSUM_LENGTH);
            offset += CHECKSUM_LENGTH;
            TarEntryType type = GetEntryType(buffer[offset++]);
            Header.Type |= type;
            if (!Header.Type.HasFlag(TarEntryType.GNU_LongLink))
            {
                Header.LinkName = ParseString(buffer, offset, LINKNAME_LENGTH);
            }
            offset += LINKNAME_LENGTH;
            Header.Magic = ParseString(buffer, offset, MAGIC_LENGTH);
            offset += MAGIC_LENGTH;
            Header.Version = ParseString(buffer, offset, VERSION_LENGTH);
            offset += VERSION_LENGTH;
            Header.Uname = ParseString(buffer, offset, UNAME_LENGTH);
            offset += UNAME_LENGTH;
            Header.Gname = ParseString(buffer, offset, GNAME_LENGTH);

            return type;
        }
        /// <summary>
        /// Returns summary of this header likes "<code>ls -l</code>" Unix command.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            switch (Header.Type)
            {
                case TarEntryType.Directory: sb.Append('d'); break;
                case TarEntryType.SymbolicLink: sb.Append('l'); break;
                case TarEntryType.Character: sb.Append('c'); break;
                case TarEntryType.Block: sb.Append('b'); break;
                default:
                    sb.Append('-'); break;
            }
            sb.Append(Header.Permission.ToString());
            sb.Append(string.Format(" {0}:{1}", Header.Uname, Header.Gname));
            sb.Append(string.Format(" {0,10:D} {1} {2}", Header.Size, Header.Mtime.ToString(), Header.Name));
            if (Header.Type.HasFlag(TarEntryType.SymbolicLink))
            {
                sb.Append(string.Format(" -> {0}", Header.LinkName));
            }
            return sb.ToString();
        }
        /// <summary>
        /// byteデータを0x00まで読んで文字列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private string ParseString(byte[] buffer, int offset, int length)
        {
            int count = 0;
            for (; count < length; ++count)
            {
                if (buffer[offset + count] == 0x00) break;
            }
            return EntryNameEncoding.GetString(buffer, offset, count).Trim();
        }
        /// <summary>
        /// byteデータを8進数の文字列として数値化
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private long ParseOctet(byte[] buffer, int offset, int length)
        {
            return Convert.ToInt64(ParseString(buffer, offset, length), 8);
        }
        static private DateTime Epoch2Date(long epoch)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return date.AddSeconds(epoch).ToLocalTime();
        }
        /// <summary>
        /// 1byte(<paramref name="typeflag"/>)からTypeを得る
        /// </summary>
        /// <param name="typeflag"></param>
        /// <returns></returns>
        /// <see cref="TarEntryType"/>
        static private TarEntryType GetEntryType (byte typeflag)
        {
            switch (typeflag)
            {
                case 0:
                case 48:
                    return TarEntryType.Regular;
                case 49:
                    return TarEntryType.Link;
                case 50:
                    return TarEntryType.SymbolicLink;
                case 51:
                    return TarEntryType.Character;
                case 52:
                    return TarEntryType.Block;
                case 53:
                    return TarEntryType.Directory;
                case 54:
                    return TarEntryType.FIFO;
                case 75: // "K"(0x4b)
                    return TarEntryType.GNU_LongLink;
                case 76: // "L"
                    return TarEntryType.GNU_LongName;
            }
            return TarEntryType.Unkown;
        }
    }

    public class TarHeaderParsingException : Exception
    {
        private const string msg = "Failed to parsing the tar header";
        public TarHeaderParsingException()
            : base(msg)
        {

        }
        public TarHeaderParsingException(string message)
            : base(message)
        {
        }
        public TarHeaderParsingException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
