using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace teramako.IO.Tar
{
    public class TarHeader
    {
        /// <summary>
        /// File name
        /// </summary>
        public string Name { get; internal set; }
        /// <summary>
        /// UNIX/Linux permission
        /// </summary>
        public Permission Permission { get; internal set; }
        /// <summary>
        /// User ID
        /// </summary>
        public int Uid { get; internal set; }
        /// <summary>
        ///  Group ID
        /// </summary>
        public int Gid { get; internal set; }
        /// <summary>
        /// File size (byte)
        /// </summary>
        public long Size { get; internal set; }
        /// <summary>
        /// Last modified time
        /// </summary>
        public DateTime Mtime { get; internal set; }
        public int Checksum { get; internal set; }
        //        public byte Typeflag { get; internal set; }
        public string LinkName { get; internal set; }
        public string Magic { get; internal set; }
        public string Version { get; internal set; }
        /// <summary>
        /// User name
        /// </summary>
        public string Uname { get; internal set; }
        /// <summary>
        /// Group name
        /// </summary>
        public string Gname { get; internal set; }
        /// <summary>
        /// Entry type
        /// </summary>
        public TarEntryType Type { get; internal set; }
    }

    [Flags]
    public enum PermissionFlags
    {
        None = 0,
        S_IXOTH = 1 << 0,
        S_IWOTH = 1 << 1,
        S_IROTH = 1 << 2,
        S_IXGRP = 1 << 3,
        S_IWGRP = 1 << 4,
        S_IRGRP = 1 << 5,
        S_IXUSR = 1 << 6,
        S_IWUSR = 1 << 7,
        S_IRUSR = 1 << 8,
        S_ISVTX = 1 << 9,
        S_ISGID = 1 << 10,
        S_ISUID = 1 << 11,
    }
    /// <summary>
    /// Unix Permission
    /// </summary>
    public class Permission
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="permission"></param>
        public string Octet
        {
            get
            {
                return Convert.ToString((int)value__, 8).PadLeft(4, '0');
            }
        }
        private readonly PermissionFlags value__ = 0;
        private string str = "";
        public Permission(int permission)
        {
            value__ = (PermissionFlags)permission;
        }
        public Permission(PermissionFlags flags)
        {
            value__ = flags;
        }
        public bool HasFlags(PermissionFlags flags)
        {
            return flags.HasFlag(value__);
        }
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(str))
            {
                return str;
            }
            var sb = new StringBuilder();
            var m = value__;
            sb.Append(m.HasFlag(PermissionFlags.S_IRUSR) ? 'r' : '-');
            sb.Append(m.HasFlag(PermissionFlags.S_IWUSR) ? 'w' : '-');
            if (m.HasFlag(PermissionFlags.S_ISUID))
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXUSR) ? 's' : 'S');
            }
            else
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXUSR) ? 'x' : '-');
            }
            sb.Append(m.HasFlag(PermissionFlags.S_IRGRP) ? 'r' : '-');
            sb.Append(m.HasFlag(PermissionFlags.S_IWGRP) ? 'w' : '-');
            if (m.HasFlag(PermissionFlags.S_ISGID))
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXGRP) ? 's' : 'S');
            }
            else
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXGRP) ? 'x' : '-');
            }
            sb.Append(m.HasFlag(PermissionFlags.S_IROTH) ? 'r' : '-');
            sb.Append(m.HasFlag(PermissionFlags.S_IWOTH) ? 'w' : '-');
            if (m.HasFlag(PermissionFlags.S_ISVTX))
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXOTH) ? 't' : 'T');
            }
            else
            {
                sb.Append(m.HasFlag(PermissionFlags.S_IXOTH) ? 'x' : '-');
            }
            return str = sb.ToString();

        }
    }
}
