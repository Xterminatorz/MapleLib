﻿using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapleLib.WzLib {
    /// <summary>
    /// A class that contains all the information of a wz file
    /// </summary>
    public class WzFile : WzDirectory {
        #region Fields

        internal string mPath;
        internal WzHeader mHeader;
        internal ushort mVersion;
        internal uint mVersionHash;
        internal short mFileVersion;
        internal const ushort wzVersionHeader64bit_start = 777;
        internal WzMapleVersion mMapleVersion;
        internal List<WzFile> fileExts = new List<WzFile>();
        internal bool b64BitClient = false; // KMS update after Q4 2021, ver 1.2.357
        private bool b64BitClient_withVerHeader = false;

        #endregion

        /// <summary>
        /// Name of the WzFile
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        /// <summary>
        /// The WzObjectType of the file
        /// </summary>
        public override WzObjectType ObjectType { get { return WzObjectType.File; } }

        public WzHeader Header { get { return mHeader; } set { mHeader = value; } }

        public short Version { get { return mFileVersion; } }

        public string FilePath { get { return mPath; } }

        public WzMapleVersion MapleVersion { get { return mMapleVersion; } }

        // Used to determine external WZ file as a directory
        public Boolean SubFile { get; set; }

        public override void Dispose() {
            base.Dispose();
            mReader?.Close();
            Header = null;
            mPath = null;
            mName = null;
            if (fileExts != null) {
                foreach (WzFile f in fileExts)
                    f.Dispose();
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public WzFile(short pGameVersion, WzMapleVersion pVersion) {
            Header = WzHeader.GetDefault();
            mFileVersion = pGameVersion;
            mMapleVersion = pVersion;
            mWzIv = WzTool.GetIvByMapleVersion(pVersion);
        }

        /// <summary>
        /// Open a wz file from a file on the disk
        /// </summary>
        /// <param name="pFilePath">Path to the wz file</param>
        /// <param name="pVersion">Maple Version</param>
        public WzFile(string pFilePath, WzMapleVersion pVersion, List<WzFile> extensions = null) {
            mName = Path.GetFileName(pFilePath);
            mPath = pFilePath;
            mFileVersion = -1;
            mMapleVersion = pVersion;
            fileExts = extensions;
            /*if (pVersion == WzMapleVersion.LOAD_FROM_ZLZ) {
				FileStream zlzStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(pFilePath), "ZLZ.dll"));
				mWzIv = WzKeyGenerator.GetIvFromZlz(zlzStream);
				zlzStream.Close();
			} else {*/
            mWzIv = WzTool.GetIvByMapleVersion(pVersion);
            //}
        }

        /// <summary>
        /// Open a wz file from a file on the disk
        /// </summary>
        /// <param name="pFilePath">Path to the wz file</param>
        public WzFile(string pFilePath, short pGameVersion, WzMapleVersion pVersion) {
            mName = Path.GetFileName(pFilePath);
            mPath = pFilePath;
            mFileVersion = pGameVersion;
            mMapleVersion = pVersion;
            /*if (pVersion == WzMapleVersion.LOAD_FROM_ZLZ) {
				FileStream zlzStream = File.OpenRead(Path.Combine(Path.GetDirectoryName(pFilePath), "ZLZ.dll"));
				mWzIv = WzKeyGenerator.GetIvFromZlz(zlzStream);
				zlzStream.Close();
			} else {*/
            mWzIv = WzTool.GetIvByMapleVersion(pVersion);
            //}
        }

        /// <summary>
        /// Parses the wz file, if the wz file is a list.wz file, WzDirectory will be a WzListDirectory, if not, it'll simply be a WzDirectory
        /// </summary>
        public void ParseWzFile() {
            if (mMapleVersion == WzMapleVersion.GENERATE)
                throw new InvalidOperationException("Cannot call ParseWzFile() if WZ file type is GENERATE");
            GetWzExtensionFiles();
            ParseMainWzDirectory();
            if (fileExts != null) {
                foreach (WzFile f in fileExts)
                    f.ParseMainWzDirectory(this);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ParseWzFile(byte[] pWzIv) {
            if (mMapleVersion != WzMapleVersion.GENERATE)
                throw new InvalidOperationException("Cannot call ParseWzFile(byte[] generateKey) if WZ file type is not GENERATE");
            mWzIv = pWzIv;
            ParseMainWzDirectory();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void GetWzExtensionFiles() {
            FileInfo wzFileInfo = new FileInfo(mPath);
            string selFileName = Path.GetFileNameWithoutExtension(wzFileInfo.Name);
            var extFiles = Directory.GetFiles(wzFileInfo.DirectoryName, selFileName + "*???.wz");
            foreach (string extFile in extFiles) {
                if (Regex.IsMatch(extFile, selFileName + ".?[0-9]{3}.wz$", RegexOptions.IgnoreCase))
                    fileExts.Add(new WzFile(extFile, mFileVersion, mMapleVersion));
            }
        }

        internal void ParseMainWzDirectory(WzFile parentFile = null) {
            if (mPath == null) {
                Console.WriteLine("[Error] Path is null");
                return;
            }
            byte[] key = WzKeyGenerator.GenerateWzKey(mWzIv);
            mReader = new WzBinaryReader(File.Open(mPath, FileMode.Open, FileAccess.Read, FileShare.Read), key, true);
            Header = new WzHeader { Ident = mReader.ReadString(4), FSize = mReader.ReadUInt64(), FStart = mReader.ReadUInt32(), Copyright = mReader.ReadNullTerminatedString() };
            int bytesToRead = (int)(Header.FStart - mReader.BaseStream.Position);
            if (bytesToRead < 0) {
                throw new Exception("Unable to parse WZ file header");
            }
            mReader.ReadBytes(bytesToRead);
            mReader.Header = Header;
            Check64BitClient(mReader);
            mVersion = b64BitClient && !b64BitClient_withVerHeader ? wzVersionHeader64bit_start : mReader.ReadUInt16();

            if (mFileVersion == -1) {
                int j = b64BitClient ? wzVersionHeader64bit_start : 0;
                for (; j < short.MaxValue; j++) {
                    mFileVersion = (short)j;
                    if (parentFile != null)
                        mFileVersion = parentFile.mFileVersion;
                    mVersionHash = GetVersionHash(mVersion, mFileVersion);
                    if (mVersionHash == 0)
                        continue;
                    mReader.Hash = mVersionHash;
                    long position = mReader.BaseStream.Position;
                    WzDirectory testDirectory;
                    try {
                        testDirectory = new WzDirectory(mReader, mName, mVersionHash, mWzIv);
                        testDirectory.ParseDirectory();
                    } catch {
                        mReader.BaseStream.Position = position;
                        continue;
                    }

                    WzImage[] childImages = testDirectory.GetChildImages();
                    if (childImages.Length > 0) {
                        foreach (WzImage s in childImages) {
                            if (s.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
                                testDirectory.Dispose();
                                throw new Exception("Invalid file names were detected. An invalid encryption may have been used.");
                            }
                        }
                        WzImage testImage = childImages[0];
                        try {
                            mReader.BaseStream.Position = testImage.Offset;
                            byte checkByte = mReader.ReadByte();
                            mReader.BaseStream.Position = position;
                            testDirectory.Dispose();
                            switch (checkByte) {
                                case 0x73:
                                case 0x1b: {
                                        mHash = mVersionHash;
                                        ParseDirectory(parentFile);
                                        return;
                                    }
                            }
                            mReader.BaseStream.Position = position;
                        } catch {
                            mReader.BaseStream.Position = position;
                        }
                    } else {
                        Boolean hasValidDir = false;
                        foreach (WzDirectory dir in testDirectory.WzDirectories) {
                            if (testDirectory.BlockSize == 0 && testDirectory.Checksum == 0) { // external directory
                                string extDir = Path.Combine(Path.GetDirectoryName(mPath), dir.Name);
                                if (Directory.Exists(extDir)) {
                                    hasValidDir = true;
                                    break;
                                }
                            }
                        }
                        if (testDirectory.mReader.BaseStream.Position == testDirectory.mReader.BaseStream.Length || hasValidDir) {
                            mHash = mVersionHash;
                            mReader.BaseStream.Position = position;
                            ParseDirectory(this);
                            return;
                        }
                        testDirectory.Dispose();
                    }
                }
                throw new Exception("Error with game version hash : The specified game version is incorrect and WzLib was unable to determine the version itself");
            }
            mVersionHash = GetVersionHash(mVersion, mFileVersion);
            mReader.Hash = mVersionHash;
            mHash = mVersionHash;
            ParseDirectory(SubFile ? this : parentFile);
        }

        /// <summary>
        /// encVer detecting:
        /// Since KMST1132 (GMSv230, 2022/02/09), wz removed the 2-byte encVer at 0x3C, and use a fixed encVer 777.
        /// Here we try to read the first 2 bytes from data part (0x3C) and guess if it looks like an encVer.
        ///
        /// Credit: WzComparerR2 project
        /// </summary>
        private void Check64BitClient(WzBinaryReader reader) {
            if (this.Header.FSize >= 2) {
                ushort wzVersionHeader = reader.ReadUInt16();
                if (wzVersionHeader > 0xff) {
                    b64BitClient = true;
                } else if (wzVersionHeader == 0x80) {
                    // there's an exceptional case that the first field of data part is a compressed int which determines the property count,
                    // if the value greater than 127 and also to be a multiple of 256, the first 5 bytes will become to
                    // 80 00 xx xx xx
                    // so we additional check the int value, at most time the child node count in a WzFile won't greater than 65536 (0xFFFF).
                    if (this.Header.FSize >= 5) {
                        reader.BaseStream.Position = this.mHeader.FStart; // go back to 0x3C
                        int propCount = reader.ReadCompressedInt();
                        if (propCount > 0 && (propCount & 0xFF) == 0 && propCount <= 0xFFFF) {
                            b64BitClient = true;
                        }
                    }
                } else if (wzVersionHeader == 0x21) // or 33
                {
                    b64BitClient = true;
                    // but read the header
                    // the latest KMS seems to include this back in again.. damn 
                    this.b64BitClient_withVerHeader = true; // ugly hack, but until i've found a better way without breaking compatibility of old WZs.
                }
            } else {
                // Obviously, if data part have only 1 byte, encVer must be deleted.
                b64BitClient = true;
            }

            // reset position
            reader.BaseStream.Position = this.Header.FStart;
        }

        private static uint GetVersionHash(int pEncVer, int pRealVer) {
            int EncryptedVersionNumber = pEncVer;
            int VersionNumber = pRealVer;
            int VersionHash = 0;
            int DecryptedVersionNumber;
            string VersionNumberStr;
            int a, b, c, d, l;

            VersionNumberStr = VersionNumber.ToString();

            l = VersionNumberStr.Length;
            for (int i = 0; i < l; i++) {
                VersionHash = (32 * VersionHash) + VersionNumberStr[i] + 1;
            }
            if (pEncVer == wzVersionHeader64bit_start)
                return (uint)VersionHash;
            a = (VersionHash >> 24) & 0xFF;
            b = (VersionHash >> 16) & 0xFF;
            c = (VersionHash >> 8) & 0xFF;
            d = VersionHash & 0xFF;
            DecryptedVersionNumber = (0xff ^ a ^ b ^ c ^ d);

            return EncryptedVersionNumber == DecryptedVersionNumber ? Convert.ToUInt32(VersionHash) : 0;
        }

        private void CreateVersionHash() {
            mVersionHash = 0;
            foreach (char ch in mFileVersion.ToString()) {
                mVersionHash = (mVersionHash * 32) + (byte)ch + 1;
            }
            uint a = (mVersionHash >> 24) & 0xFF, b = (mVersionHash >> 16) & 0xFF, c = (mVersionHash >> 8) & 0xFF, d = mVersionHash & 0xFF;
            mVersion = (byte)~(a ^ b ^ c ^ d);
        }

        /// <summary>
        /// Saves a wz file to the disk, AKA repacking.
        /// </summary>
        /// <param name="pPath">Path to the output wz file</param>
        public void SaveToDisk(string pPath) {
            mWzIv = WzTool.GetIvByMapleVersion(mMapleVersion);
            CreateVersionHash();
            SetHash(mVersionHash);
            string tempFile = Path.GetFileNameWithoutExtension(pPath) + ".TEMP";
            File.Create(tempFile).Close();
            GenerateDataFile(tempFile);
            WzTool.StringCache.Clear();
            uint totalLen = GetImgOffsets(GetOffsets(Header.FStart + 2));
            WzBinaryWriter wzWriter = new WzBinaryWriter(File.Create(pPath), mWzIv) { Hash = mVersionHash };
            Header.FSize = totalLen - Header.FStart;
            wzWriter.Write(Header.Ident, false);
            wzWriter.Write((long)Header.FSize);
            wzWriter.Write(Header.FStart);
            wzWriter.WriteNullTerminatedString(Header.Copyright);
            wzWriter.Write(new byte[Header.ExtraBytes]);
            wzWriter.Write(mVersion);
            wzWriter.Header = Header;
            SaveDirectory(wzWriter);
            wzWriter.StringCache.Clear();
            FileStream fs = File.OpenRead(tempFile);
            SaveImages(wzWriter, fs);
            fs.Close();
            File.Delete(tempFile);
            wzWriter.StringCache.Clear();
            wzWriter.Close();
        }

        public void ExportXml(string pPath, bool pOneFile) {
            if (!pOneFile) {
                throw new Exception("Under Construction");
            }
            FileStream fs = File.Create(pPath + "/" + mName + ".xml");
            StreamWriter writer = new StreamWriter(fs);

            int level = 0;
            writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzFile", mName, true));
            ExportXml(writer, pOneFile, level, false);
            writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzFile"));

            writer.Close();
        }

        #region Search Methods

        /// <summary>
        /// Returns an array of objects from a given path. Wild cards are supported
        /// For example :
        /// GetObjectsFromPath("Map.wz/Map0/*");
        /// Would return all the objects (in this case images) from the sub directory Map0
        /// </summary>
        /// <param name="path">The path to the object(s)</param>
        /// <returns>An array of AWzObjects containing the found objects</returns>
        public AWzObject[] GetObjectsFromWildcardPath(string path) {
            if (path.ToLower() == mName.ToLower())
                return new AWzObject[] { this };
            if (path == "*") {
                List<AWzObject> fullList = new List<AWzObject> {
                    this
                };
                fullList.AddRange(GetObjectsFromDirectory(this));
                return fullList.ToArray();
            }
            if (!path.Contains("*"))
                return new[] { GetObjectFromPath(path) };
            string[] seperatedNames = path.Split("/".ToCharArray());
            if (seperatedNames.Length == 2 && seperatedNames[1] == "*")
                return GetObjectsFromDirectory(this);
            List<AWzObject> objList = (from img in WzImages from spath in GetPathsFromImage(img, mName + "/" + img.Name) where StrMatch(path, spath) select GetObjectFromPath(spath)).ToList();
            objList.AddRange(from dir in WzDirectories from spath in GetPathsFromDirectory(dir, mName + "/" + dir.Name) where StrMatch(path, spath) select GetObjectFromPath(spath));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return objList.ToArray();
        }

        public AWzObject[] GetObjectsFromRegexPath(string path) {
            if (path.ToLower() == mName.ToLower())
                return new AWzObject[] { this };
            List<AWzObject> objList = (from img in WzImages from spath in GetPathsFromImage(img, mName + "/" + img.Name) where Regex.Match(spath, path).Success select GetObjectFromPath(spath)).ToList();
            objList.AddRange(from dir in WzDirectories from spath in GetPathsFromDirectory(dir, mName + "/" + dir.Name) where Regex.Match(spath, path).Success select GetObjectFromPath(spath));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return objList.ToArray();
        }

        public AWzObject[] GetObjectsFromDirectory(WzDirectory dir) {
            List<AWzObject> objList = new List<AWzObject>();
            foreach (WzImage img in dir.WzImages) {
                objList.Add(img);
                objList.AddRange(GetObjectsFromImage(img));
            }
            foreach (WzDirectory subdir in dir.WzDirectories) {
                objList.Add(subdir);
                objList.AddRange(GetObjectsFromDirectory(subdir));
            }
            return objList.ToArray();
        }

        public AWzObject[] GetObjectsFromImage(WzImage img) {
            List<AWzObject> objList = new List<AWzObject>();
            foreach (AWzImageProperty prop in img.WzProperties) {
                objList.Add(prop);
                objList.AddRange(GetObjectsFromProperty(prop));
            }
            return objList.ToArray();
        }

        public static AWzObject[] GetObjectsFromProperty(AWzImageProperty prop) {
            List<AWzObject> objList = new List<AWzObject>();
            switch (prop.PropertyType) {
                case WzPropertyType.Canvas:
                    objList.AddRange(prop.WzProperties);
                    objList.Add(((WzCanvasProperty)prop).PngProperty);
                    break;
                case WzPropertyType.Convex:
                    objList.AddRange(prop.WzProperties);
                    break;
                case WzPropertyType.SubProperty:
                    objList.AddRange(prop.WzProperties);
                    break;
                case WzPropertyType.Vector:
                    objList.Add(((WzVectorProperty)prop).X);
                    objList.Add(((WzVectorProperty)prop).Y);
                    break;
            }
            return objList.ToArray();
        }

        internal string[] GetPathsFromDirectory(WzDirectory dir, string curPath) {
            List<string> objList = new List<string>();
            foreach (WzImage img in dir.WzImages) {
                objList.Add(curPath + "/" + img.Name);

                objList.AddRange(GetPathsFromImage(img, curPath + "/" + img.Name));
            }
            foreach (WzDirectory subdir in dir.WzDirectories) {
                objList.Add(curPath + "/" + subdir.Name);
                objList.AddRange(GetPathsFromDirectory(subdir, curPath + "/" + subdir.Name));
            }
            return objList.ToArray();
        }

        internal string[] GetPathsFromImage(WzImage img, string curPath) {
            List<string> objList = new List<string>();
            foreach (AWzImageProperty prop in img.WzProperties) {
                objList.Add(curPath + "/" + prop.Name);
                objList.AddRange(GetPathsFromProperty(prop, curPath + "/" + prop.Name));
            }
            return objList.ToArray();
        }

        internal string[] GetPathsFromProperty(AWzImageProperty prop, string curPath) {
            List<string> objList = new List<string>();
            switch (prop.PropertyType) {
                case WzPropertyType.Canvas:
                    foreach (AWzImageProperty canvasProp in prop.WzProperties) {
                        objList.Add(curPath + "/" + canvasProp.Name);
                        objList.AddRange(GetPathsFromProperty(canvasProp, curPath + "/" + canvasProp.Name));
                    }
                    objList.Add(curPath + "/PNG");
                    break;
                case WzPropertyType.Convex:
                    foreach (AWzImageProperty conProp in prop.WzProperties) {
                        objList.Add(curPath + "/" + conProp.Name);
                        objList.AddRange(GetPathsFromProperty(conProp, curPath + "/" + conProp.Name));
                    }
                    break;
                case WzPropertyType.SubProperty:
                    foreach (AWzImageProperty subProp in prop.WzProperties) {
                        objList.Add(curPath + "/" + subProp.Name);
                        objList.AddRange(GetPathsFromProperty(subProp, curPath + "/" + subProp.Name));
                    }
                    break;
                case WzPropertyType.Vector:
                    objList.Add(curPath + "/X");
                    objList.Add(curPath + "/Y");
                    break;
            }
            return objList.ToArray();
        }

        public AWzObject GetObjectFromPath(string path) {
            string[] seperatedPath = path.Split('/');
            if (seperatedPath[0].ToLower() != mName.ToLower())
                return null;
            if (seperatedPath.Length == 1)
                return this;
            AWzObject curObj = this;
            for (int i = 1; i < seperatedPath.Length; i++) {
                if (curObj == null) {
                    return null;
                }
                switch (curObj.ObjectType) {
                    case WzObjectType.Directory:
                        curObj = ((WzDirectory)curObj)[seperatedPath[i]];
                        continue;
                    case WzObjectType.Image:
                        curObj = ((WzImage)curObj)[seperatedPath[i]];
                        continue;
                    case WzObjectType.Property:
                        switch (((AWzImageProperty)curObj).PropertyType) {
                            case WzPropertyType.Canvas:
                                curObj = ((WzCanvasProperty)curObj)[seperatedPath[i]];
                                continue;
                            case WzPropertyType.Convex:
                                curObj = ((WzConvexProperty)curObj)[seperatedPath[i]];
                                continue;
                            case WzPropertyType.SubProperty:
                                curObj = ((WzSubProperty)curObj)[seperatedPath[i]];
                                continue;
                            case WzPropertyType.Vector:
                                if (seperatedPath[i] == "X")
                                    return ((WzVectorProperty)curObj).X;
                                return seperatedPath[i] == "Y" ? ((WzVectorProperty)curObj).Y : null;
                            default: // Wut?
                                return null;
                        }
                }
            }
            return curObj;
        }

        internal bool StrMatch(string strWildCard, string strCompare) {
            if (strWildCard.Length == 0)
                return strCompare.Length == 0;
            if (strCompare.Length == 0)
                return false;
            if (strWildCard[0] == '*' && strWildCard.Length > 1)
                for (int index = 0; index < strCompare.Length; index++) {
                    if (StrMatch(strWildCard.Substring(1), strCompare.Substring(index)))
                        return true;
                } else if (strWildCard[0] == '*')
                return true;
            else if (strWildCard[0] == strCompare[0])
                return StrMatch(strWildCard.Substring(1), strCompare.Substring(1));
            return false;
        }

        #endregion
    }
}