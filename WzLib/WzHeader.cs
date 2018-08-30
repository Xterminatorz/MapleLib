namespace MapleLib.WzLib {
    public class WzHeader {
        private string mIdent;
        private string mCopyright;
        private ulong mFSize;
        private uint mFStart;
        private uint mExtraBytes;

        public string Ident { get { return mIdent; } set { mIdent = value; } }

        public string Copyright { get { return mCopyright; } set { mCopyright = value; } }

        public ulong FSize { get { return mFSize; } set { mFSize = value; } }

        public uint FStart { get { return mFStart; } set { mFStart = value; } }

        public uint ExtraBytes { get { return mExtraBytes; } set { mExtraBytes = value; } }

        public void RecalculateFileStart() {
            mFStart = (uint)(mIdent.Length + sizeof(ulong) + sizeof(uint) + mCopyright.Length + 1) + mExtraBytes;
        }

        public static WzHeader GetDefault() {
            return new WzHeader { mIdent = "PKG1", mCopyright = "Package file v1.0 Copyright 2002 Wizet, ZMS", mFStart = 60, mFSize = 0, mExtraBytes = 0 };
        }
    }
}