using MapleLib.WzLib.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapleLib.WzLib {
    /// <summary>
    /// A directory in the wz file, which may contain sub directories or wz images
    /// </summary>
    public class WzDirectory : AWzObject {
        #region Fields

        internal List<WzImage> mImages = new List<WzImage>();
        internal List<WzDirectory> mSubDirs = new List<WzDirectory>();
        internal WzBinaryReader mReader;
        internal uint mOffset;
        internal string mName;
        internal uint mHash;
        internal int mSize, mChecksum, mOffsetSize;
        internal byte[] mWzIv;
        internal AWzObject mParent;

        #endregion

        #region Inherited Members

        /// <summary>  
        /// The parent of the object
        /// </summary>
        public override AWzObject Parent { get { return mParent; } internal set { mParent = value; } }

        /// <summary>
        /// The name of the directory
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        /// <summary>
        /// The WzObjectType of the directory
        /// </summary>
        public override WzObjectType ObjectType { get { return WzObjectType.Directory; } }

        /// <summary>
        /// Disposes the obejct
        /// </summary>
        public override void Dispose() {
            mName = null;
            mReader = null;
            foreach (WzImage img in mImages)
                img.Dispose();
            foreach (WzDirectory dir in mSubDirs)
                dir.Dispose();
            mImages.Clear();
            mSubDirs.Clear();
            mImages = null;
            mSubDirs = null;
        }

        #endregion

        /// <summary>
        /// The size of the directory in the wz file
        /// </summary>
        public int BlockSize { get { return mSize; } set { mSize = value; } }

        /// <summary>
        /// The directory's chceksum
        /// </summary>
        public int Checksum { get { return mChecksum; } set { mChecksum = value; } }

        /// <summary>
        /// The wz images contained in the directory
        /// </summary>
        public WzImage[] WzImages { get { return mImages.ToArray(); } }

        /// <summary>
        /// The sub directories contained in the directory
        /// </summary>
        public WzDirectory[] WzDirectories { get { return mSubDirs.ToArray(); } }

        /// <summary>
        /// Offset of the folder
        /// </summary>
        public uint Offset { get { return mOffset; } set { mOffset = value; } }

        /// <summary>
        /// Returns a WzImage or a WzDirectory with the given name
        /// </summary>
        /// <param name="pName">The name of the img or dir to find</param>
        /// <returns>A WzImage or WzDirectory</returns>
        public AWzObject this[string pName] {
            get {
                foreach (WzImage i in mImages.Where(i => i.Name != null && (i.Name.ToLower() == (pName.ToLower() + ".img") || i.Name.ToLower() == pName.ToLower()))) {
                    return i;
                }
                return mSubDirs.FirstOrDefault(d => d.Name.ToLower() == pName.ToLower());
                //throw new KeyNotFoundException("No wz image or directory was found with the specified name");
            }
        }


        /// <summary>
        /// Creates a blank WzDirectory
        /// </summary>
        public WzDirectory() {
        }

        /// <summary>
        /// Creates a WzDirectory with the given name
        /// </summary>
        /// <param name="pName">The name of the directory</param>
        public WzDirectory(string pName) {
            mName = pName;
        }

        /// <summary>
        /// Creates a WzDirectory
        /// </summary>
        /// <param name="pReader">The BinaryReader that is currently reading the wz file</param>
        /// <param name="pDirName">The name of the directory</param>
        /// <param name="pVerHash">The version hash</param>
        /// <param name="pWzIv">The WZ IV</param>
        internal WzDirectory(WzBinaryReader pReader, string pDirName, uint pVerHash, byte[] pWzIv) {
            mReader = pReader;
            mName = pDirName;
            mHash = pVerHash;
            mWzIv = pWzIv;
        }

        /// <summary>
        /// Parses the WzDirectory
        /// </summary>
        internal void ParseDirectory(WzFile parent = null) {
            //Array.Copy(mReader.WzKey, keyCopy, mReader.WzKey.Length);
            int entryCount = mReader.ReadCompressedInt();
            for (int i = 0; i < entryCount; i++) {
                byte type = mReader.ReadByte();
                string fname = null;
                int fsize;
                int checksum;
                uint offset;
                long rememberPos = 0;
                switch (type) {
                    case 1: {
                            mReader.ReadInt32();
                            mReader.ReadInt16();
                            mReader.ReadOffset();
                            continue;
                        }
                    case 2: {
                            int stringOffset = mReader.ReadInt32();
                            rememberPos = mReader.BaseStream.Position;
                            mReader.BaseStream.Position = mReader.Header.FStart + stringOffset;
                            type = mReader.ReadByte();
                            fname = mReader.ReadString().Trim();
                        }
                        break;
                    case 3:
                    case 4:
                        fname = mReader.ReadString().Trim();
                        rememberPos = mReader.BaseStream.Position;
                        break;
                }
                mReader.BaseStream.Position = rememberPos;
                fsize = mReader.ReadCompressedInt();
                checksum = mReader.ReadCompressedInt();
                offset = mReader.ReadOffset();
                if (type == 3) {
                    WzDirectory subDir = new WzDirectory(mReader, fname, mHash, mWzIv) { BlockSize = fsize, Checksum = checksum, Offset = offset, Parent = parent ?? this };
                    if (parent != null)
                        parent.mSubDirs.Add(subDir);
                    mSubDirs.Add(subDir);
                } else {
                    WzImage img = new WzImage(fname, mReader) { BlockSize = fsize, Checksum = checksum, Offset = offset, Parent = parent ?? this };
                    if (parent != null)
                        parent.mImages.Add(img);
                    mImages.Add(img);
                }
            }
            foreach (WzDirectory subdir in mSubDirs) {
                mReader.BaseStream.Position = subdir.mOffset;
                subdir.ParseDirectory();
            }
        }

        internal void SaveImages(BinaryWriter pWzWriter, FileStream pFileStream) {
            foreach (WzImage img in mImages) {
                pFileStream.Position = img.mTempFileStart;
                byte[] buffer = new byte[img.mSize];
                pFileStream.Read(buffer, 0, img.mSize);
                pWzWriter.Write(buffer);
            }
            foreach (WzDirectory dir in mSubDirs)
                dir.SaveImages(pWzWriter, pFileStream);
        }

        internal int GenerateDataFile(string pFileName) {
            mSize = 0;
            int entryCount = mSubDirs.Count + mImages.Count;
            if (entryCount == 0) {
                mOffsetSize = 1;
                return (mSize = 0);
            }
            mSize = WzTool.GetCompressedIntLength(entryCount);
            mOffsetSize = WzTool.GetCompressedIntLength(entryCount);

            WzBinaryWriter imgWriter;
            MemoryStream memStream;
            FileStream fileWrite = new FileStream(pFileName, FileMode.Append, FileAccess.Write);
            foreach (WzImage t in mImages) {
                memStream = new MemoryStream();
                imgWriter = new WzBinaryWriter(memStream, mWzIv);
                t.SaveImage(imgWriter);
                t.checksum = 0;
                foreach (byte b in memStream.ToArray()) {
                    t.checksum += b;
                }
                t.mTempFileStart = fileWrite.Position;
                fileWrite.Write(memStream.ToArray(), 0, (int)memStream.Length);
                t.mTempFileEnd = fileWrite.Position;
                memStream.Dispose();
                t.UnparseImage();

                int nameLen = WzTool.GetWzObjectValueLength(t.mName, 4);
                mSize += nameLen;
                int imgLen = t.mSize;
                mSize += WzTool.GetCompressedIntLength(imgLen);
                mSize += imgLen;
                mSize += WzTool.GetCompressedIntLength(t.Checksum);
                mSize += 4;
                mOffsetSize += nameLen;
                mOffsetSize += WzTool.GetCompressedIntLength(imgLen);
                mOffsetSize += WzTool.GetCompressedIntLength(t.Checksum);
                mOffsetSize += 4;
                imgWriter.Close();
            }
            fileWrite.Close();

            foreach (WzDirectory t in mSubDirs) {
                int nameLen = WzTool.GetWzObjectValueLength(t.mName, 3);
                mSize += nameLen;
                mSize += t.GenerateDataFile(pFileName);
                mSize += WzTool.GetCompressedIntLength(t.mSize);
                mSize += WzTool.GetCompressedIntLength(t.mChecksum);
                mSize += 4;
                mOffsetSize += nameLen;
                mOffsetSize += WzTool.GetCompressedIntLength(t.mSize);
                mOffsetSize += WzTool.GetCompressedIntLength(t.mChecksum);
                mOffsetSize += 4;
            }
            return mSize;
        }

        internal void SaveDirectory(WzBinaryWriter pWriter) {
            mOffset = (uint)pWriter.BaseStream.Position;
            int entryCount = mSubDirs.Count + mImages.Count;
            if (entryCount == 0) {
                BlockSize = 0;
                return;
            }
            pWriter.WriteCompressedInt(entryCount);
            foreach (WzImage img in mImages) {
                pWriter.WriteWzObjectValue(img.mName, 4);
                pWriter.WriteCompressedInt(img.BlockSize);
                pWriter.WriteCompressedInt(img.Checksum);
                pWriter.WriteOffset(img.Offset);
            }
            foreach (WzDirectory dir in mSubDirs) {
                pWriter.WriteWzObjectValue(dir.mName, 3);
                pWriter.WriteCompressedInt(dir.BlockSize);
                pWriter.WriteCompressedInt(dir.Checksum);
                pWriter.WriteOffset(dir.Offset);
            }
            foreach (WzDirectory dir in mSubDirs)
                if (dir.BlockSize > 0)
                    dir.SaveDirectory(pWriter);
                else
                    pWriter.Write((byte)0);
        }

        internal uint GetOffsets(uint pCurOffset) {
            mOffset = pCurOffset;
            pCurOffset += (uint)mOffsetSize;
            return mSubDirs.Aggregate(pCurOffset, (current, dir) => dir.GetOffsets(current));
        }

        internal uint GetImgOffsets(uint pCurOffset) {
            foreach (WzImage img in mImages) {
                img.Offset = pCurOffset;
                pCurOffset += (uint)img.BlockSize;
            }
            return mSubDirs.Aggregate(pCurOffset, (current, dir) => dir.GetImgOffsets(current));
        }

        internal void ExportXml(StreamWriter pWriter, bool pOneFile, int pLevel, bool pIsDirectory) {
            if (!pOneFile)
                return;
            if (pIsDirectory) {
                pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.OpenNamedTag("WzDirectory", mName, true));
            }
            foreach (WzDirectory subDir in WzDirectories) {
                subDir.ExportXml(pWriter, pOneFile, pLevel + 1, pIsDirectory);
            }
            foreach (WzImage subImg in WzImages) {
                subImg.ExportXml(pWriter, pOneFile, pLevel + 1);
            }
            if (pIsDirectory) {
                pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.CloseTag("WzDirectory"));
            }
        }

        /// <summary>
        /// Parses the wz images
        /// </summary>
        public void ParseImages() {
            foreach (WzImage img in mImages) {
                mReader.BaseStream.Position = img.Offset;
                img.ParseImage();
            }
            foreach (WzDirectory subdir in mSubDirs) {
                mReader.BaseStream.Position = subdir.Offset;
                subdir.ParseImages();
            }
        }

        internal void SetHash(uint pNewHash) {
            mHash = pNewHash;
            foreach (WzDirectory dir in mSubDirs)
                dir.SetHash(pNewHash);
        }

        /// <summary>
        /// Adds a WzImage to the list of wz images
        /// </summary>
        /// <param name="pImg">The WzImage to add</param>
        public void AddImage(WzImage pImg) {
            mImages.Add(pImg);
        }

        /// <summary>
        /// Adds a WzDirectory to the list of sub directories
        /// </summary>
        /// <param name="pDir">The WzDirectory to add</param>
        public void AddDirectory(WzDirectory pDir) {
            mSubDirs.Add(pDir);
        }

        /// <summary>
        /// Clears the list of images
        /// </summary>
        public void ClearImages() {
            mImages.Clear();
        }

        /// <summary>
        /// Clears the list of sub directories
        /// </summary>
        public void ClearDirectories() {
            mSubDirs.Clear();
        }

        /// <summary>
        /// Gets all child images of a WzDirectory
        /// </summary>
        /// <returns></returns>
        public WzImage[] GetChildImages() {
            List<WzImage> imgFiles = new List<WzImage>();
            imgFiles.AddRange(mImages);
            foreach (WzDirectory subDir in mSubDirs) {
                imgFiles.AddRange(subDir.mImages);
            }
            return imgFiles.ToArray();
        }

        /// <summary>
        /// Removes an image from the list with the specified name
        /// </summary>
        /// <param name="pImage">The image to remove</param>
        public void RemoveImage(WzImage pImage) {
            mImages.Remove(pImage);
        }

        /// <summary>
        /// Removes a sub directory from the list with the specified name
        /// </summary>
        /// <param name="pDirectory">The directory to remove</param>
        public void RemoveDirectory(WzDirectory pDirectory) {
            mSubDirs.Remove(pDirectory);
        }
    }
}