using System;

namespace teramako.IO.Tar
{
    [Flags]
    public enum TarEntryType
    {
        Incompleted  = 0,
        /// <summary>
        /// 通常のファイル
        /// </summary>
        Regular      = 1 << 0,
        /// <summary>
        /// ハードリンク
        /// </summary>
        Link         = 1 << 1,
        /// <summary>
        /// シンボリックリンク
        /// </summary>
        SymbolicLink = 1 << 2,
        /// <summary>
        /// キャラクタ型デバイス
        /// </summary>
        Character    = 1 << 3,
        /// <summary>
        /// ブロック型デバイス
        /// </summary>
        Block        = 1 << 4,
        /// <summary>
        /// ディレクトリ
        /// </summary>
        Directory    = 1 << 5,
        /// <summary>
        /// FIFO
        /// </summary>
        FIFO         = 1 << 6,
        /// <summary>
        /// 予約された値（用途なし？）
        /// </summary>
        Reserved     = 1 << 7,
        /// <summary>
        /// GNU LongLink
        /// </summary>
        GNU_LongLink = 1 << 8,
        /// <summary>
        /// GNU LongName
        /// </summary>
        GNU_LongName = 1 << 9,
        /// <summary>
        /// その他不明なもの
        /// </summary>
        Unkown       = 1 << 10,
        EndOfEntry   = -1
    }
}
