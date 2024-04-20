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
        private void Dump(string message)
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
            BaseStream = baseStream;
            EntryNameEncoding = entryEncoding;
            Parse();
        }
        #endregion
        public int HeaderBlockCount { get; private set; }
        /// <summary>
        /// Encoding of <Name or LinkName
        /// </summary>
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
                return Size;
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
        /// <summary>
        /// File name
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// UNIX/Linux permission
        /// </summary>
        public int Permission { get; private set; }
        /// <summary>
        /// User ID
        /// </summary>
        public int Uid { get; private set; }
        /// <summary>
        ///  Group ID
        /// </summary>
        public int Gid { get; private set; }
        /// <summary>
        /// File size (byte)
        /// </summary>
        public long Size { get; private set; }
        /// <summary>
        /// Last modified time
        /// </summary>
        public DateTime Mtime { get; private set; }
        public int Checksum { get; private set; }
        //        public byte Typeflag { get; private set; }
        public string LinkName { get; private set; }
        public string Magic { get; private set; }
        public string Version { get; private set; }
        /// <summary>
        /// User name
        /// </summary>
        public string Uname { get; private set; }
        /// <summary>
        /// Group name
        /// </summary>
        public string Gname { get; private set; }
        /// <summary>
        /// Entry type
        /// </summary>
        public TarEntryType Type { get; private set; }
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
            bool isCompleted = false;
            TarEntryType currentType = TarEntryType.Incompleted;
            Dump("Start Parsing");
            do
            {
                switch (currentType)
                {
                    case TarEntryType.GNU_LongName:
                        Dump("Parsing GNU_LongName");
                        Name = ParseString(ReadHeader((int)Size), 0, (int)Size);
                        currentType = TarEntryType.Incompleted;
                        break;
                    case TarEntryType.GNU_LongLink:
                        Dump("Parsing GNU_LongLink");
                        LinkName = ParseString(ReadHeader((int)Size), 0, (int)Size);
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
                            string.Format("Unkown tar header type: {0}", Type));
                    case TarEntryType.EndOfEntry:
                        Type = currentType;
                        isCompleted = true;
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
            var type = TarEntryType.Incompleted;

            var buffer = ReadHeader();
            if (buffer[0] == 0) // reached end of tar data.
            {
                Dump("this block is the end of tar data.");
                return TarEntryType.EndOfEntry;
            }
            var offset = 0;
            if (!Type.HasFlag(TarEntryType.GNU_LongName))
            {
                Name = ParseString(buffer, offset, NAME_LENGTH);
            }
            offset += NAME_LENGTH;
            Permission = (int)ParseOctet(buffer, offset, MODE_LENGTH);
            offset += MODE_LENGTH;
            Uid = (int)ParseOctet(buffer, offset, UID_LENGTH);
            offset += UID_LENGTH;
            Gid = (int)ParseOctet(buffer, offset, GID_LENGTH);
            offset += GID_LENGTH;
            Size = ParseOctet(buffer, offset, SIZE_LENGTH);
            offset += SIZE_LENGTH;
            Mtime = Epoch2Date(ParseOctet(buffer, offset, MTIME_ELNGTH));
            offset += MTIME_ELNGTH;
            Checksum = (int)ParseOctet(buffer, offset, CHECKSUM_LENGTH);
            offset += CHECKSUM_LENGTH;
            type = GetEntryType(buffer[offset++]);
            Type |= type;
            if (!Type.HasFlag(TarEntryType.GNU_LongLink))
            {
                LinkName = ParseString(buffer, offset, LINKNAME_LENGTH);
            }
            offset += LINKNAME_LENGTH;
            Magic = ParseString(buffer, offset, MAGIC_LENGTH).Trim();
            offset += MAGIC_LENGTH;
            Version = ParseString(buffer, offset, VERSION_LENGTH);
            offset += VERSION_LENGTH;
            Uname = ParseString(buffer, offset, UNAME_LENGTH);
            offset += UNAME_LENGTH;
            Gname = ParseString(buffer, offset, GNAME_LENGTH);
            offset += GNAME_LENGTH;

            return type;
        }
        /// <summary>
        /// Returns summary of this header likes "<code>ls -l</code>" Unix command.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            switch (Type)
            {
                case TarEntryType.Directory: sb.Append("d"); break;
                case TarEntryType.SymbolicLink: sb.Append("l"); break;
                case TarEntryType.Character: sb.Append("c"); break;
                case TarEntryType.Block: sb.Append("b"); break;
                default:
                    sb.Append("-"); break;
            }
            sb.Append(PermissionToString());
            sb.Append(" ");
            sb.Append(string.Format("{0}:{1}", Uname, Gname));
            sb.Append(" ");
            sb.Append(string.Format("{0,10:D} {1} {2}", Size, Mtime.ToString(), Name));
            if (Type.HasFlag(TarEntryType.SymbolicLink))
            {
                sb.Append(string.Format(" -> {0}", LinkName));
            }
            return sb.ToString();
        }
        /// <summary>
        /// Unix like permission string
        /// </summary>
        /// <returns></returns>
        private string PermissionToString()
        {
            var sb = new StringBuilder();
            var m = Permission;
            sb.Append(((m & 1<<8) != 0) ? 'r' : '-');
            sb.Append(((m & 1<<7) != 0) ? 'w' : '-');
            if ((m & 1<<15) == 0) // setUID
            {
                sb.Append(((m & 1<<6) != 0) ? 'x' : '-');
            }
            else
            {
                sb.Append(((m & 1<<6) != 0) ? 's' : 'S');
            }
            sb.Append(((m & 1<<5) != 0) ? 'r' : '-');
            sb.Append(((m & 1<<4) != 0) ? 'w' : '-');
            if ((m & 1<<12) == 0) // setGID
            {
                sb.Append(((m & 1<<3) != 0) ? 'x' : '-');
            }
            else
            {
                sb.Append(((m & 1<<3) != 0) ? 's' : 'S');
            }
            sb.Append(((m & 1<<2) != 0) ? 'r' : '-');
            sb.Append(((m & 1<<1) != 0) ? 'w' : '-');
            if ((m & 1<<9) == 0) // sticky bit
            {
                sb.Append(((m & 1<<0) != 0) ? 'x' : '-');
            }
            else
            {
                sb.Append(((m & 1<<0) != 0) ? 't' : 'T');
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
        private DateTime Epoch2Date(long epoch)
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
        private TarEntryType GetEntryType (byte typeflag)
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
