using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace teramako.IO.Tar
{
    public class TarHeader
    {
        [Conditional("DEBUG")]
        private void Dump(string message)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("TarHeader::" + message);
            Console.ForegroundColor = color;
        }
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
        public string Name { get; private set; }
        /// <summary>
        /// UNIX/Linux permission
        /// </summary>
        public int Mode { get; private set; }
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

        private const int BLOCK_SIZE = 512;
        /// <summary>
        /// Parsing a tar header from the <paramref name="inputStream"/>.
        /// And set properties to this instance.
        /// </summary>
        /// <param name="inputStream">the whole of tar stream</param>
        /// <returns>block(512byte) count.
        /// if the count is 0, indicates the <paramref name="inputStream"/> reached end of tar data.
        /// </returns>
        internal int Parse(Stream inputStream)
        {
            int blockCount = 0;
            bool isCompleted = false;
            TarEntryType typeExtension = TarEntryType.Incompleted;
            Dump("Start Parsing");
            do
            {
                blockCount++;
                byte[] buffer = new byte[BLOCK_SIZE];
                if (inputStream.Read(buffer, 0, BLOCK_SIZE) != BLOCK_SIZE)
                {
                    throw new Exception("Faild parsing TarHEader");
                }
                if (buffer[0] == 0) // reached end of tar data.
                {
                    Dump("this block is the end of tar data.");
                    return 0;
                }
                int offset = 0;
                switch (Type)
                {
                    case TarEntryType.GNU_LongName:
                        Dump("Parsing GNU_LongName");
                        Name = ParseString(buffer, 0, (int)Size);
                        Type = TarEntryType.Incompleted;
                        break;
                    case TarEntryType.GNU_LongLink:
                        Dump("Parsing GNU_LongLink");
                        LinkName = ParseString(buffer, 0, (int)Size);
                        Type = TarEntryType.Incompleted;
                        break;
                    case TarEntryType.Incompleted:
                        if ((typeExtension & TarEntryType.GNU_LongName) != TarEntryType.GNU_LongName)
                        {
                            Name = ParseString(buffer, offset, NAME_LENGTH);
                        }
                        offset += NAME_LENGTH;
                        Mode = (int)ParseOctet(buffer, offset, MODE_LENGTH);
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
                        SetEntryType(buffer[offset++]);
                        if ((typeExtension & TarEntryType.GNU_LongLink) != TarEntryType.GNU_LongLink)
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
                        break;
                }
                typeExtension |= Type;

                switch (Type)
                {
                    case TarEntryType.Incompleted:
                    case TarEntryType.GNU_LongName:
                    case TarEntryType.GNU_LongLink:
                        isCompleted = false;
                        break;
                    default:
                        isCompleted = true;
                        break;
                }
            } while (!isCompleted);
            Dump("End Parsing");
            Dump(ToString());
            return blockCount;

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
            sb.Append(ModeToString());
            sb.Append(" ");
            sb.Append(string.Format("{0}:{1}", Uname, Gname));
            sb.Append(" ");
            sb.Append(string.Format("{0,10:D} {1} {2}", Size, Mtime.ToString(), Name));
            if (Type == TarEntryType.SymbolicLink)
            {
                sb.Append(string.Format(" -> {1}", Name, LinkName));
            }
            return sb.ToString();
        }
        /// <summary>
        /// Unix like permission string
        /// </summary>
        /// <returns></returns>
        private string ModeToString()
        {
            var sb = new StringBuilder();
            var m = Mode;
            sb.Append(((m & 0b100000000) != 0) ? 'r' : '-');
            sb.Append(((m & 0b010000000) != 0) ? 'w' : '-');
            sb.Append(((m & 0b001000000) != 0) ? 'x' : '-');
            sb.Append(((m & 0b000100000) != 0) ? 'r' : '-');
            sb.Append(((m & 0b000010000) != 0) ? 'w' : '-');
            sb.Append(((m & 0b000001000) != 0) ? 'x' : '-');
            sb.Append(((m & 0b000000100) != 0) ? 'r' : '-');
            sb.Append(((m & 0b000000010) != 0) ? 'w' : '-');
            sb.Append(((m & 0b000000001) != 0) ? 'x' : '-');
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
            var sb = new StringBuilder();
            for (var i = offset; i < offset + length; ++i)
            {
                if (buffer[i] == 0) { break; }
                sb.Append((char)buffer[i]);
            }
            return sb.ToString();
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
            var date = new DateTime(1970, 1, 1, 0, 0, 0);
            return date.AddSeconds(epoch);
        }
        /// <summary>
        /// 1byte(<paramref name="typeflag"/>)からTypeを設定
        /// </summary>
        /// <param name="typeflag"></param>
        /// <see cref="TarEntryType"/>
        private void SetEntryType (byte typeflag)
        {
            switch (typeflag)
            {
                case 0:
                case 48:
                    Type = TarEntryType.Regular;
                    break;
                case 49:
                    Type = TarEntryType.Link;
                    break;
                case 50:
                    Type = TarEntryType.SymbolicLink;
                    break;
                case 51:
                    Type = TarEntryType.Character;
                    break;
                case 52:
                    Type = TarEntryType.Block;
                    break;
                case 53:
                    Type = TarEntryType.Directory;
                    break;
                case 54:
                    Type = TarEntryType.FIFO;
                    break;
                case 75: // "K"(0x4b)
                    Type = TarEntryType.GNU_LongLink;
                    break;
                case 76: // "L"
                    Type = TarEntryType.GNU_LongName;
                    break;
                default:
                    Type = TarEntryType.Unkown;
                    break;
            }
        }
    }
}
