using MapleLib.WzLib.Util;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace MapleLib.WzLib.WzProperties
{
    /// <summary>
    /// A property that contains raw data
    /// </summary>
    public class WzVideoProperty : AWzImageProperty, IExtended
    {
        #region Fields

        internal string mName;
        internal byte[] mBytes;
        internal AWzObject mParent;
        internal WzImage mImgParent;
        internal WzBinaryReader mWzReader;
        internal long mOffsets;
        #endregion

        #region Inherited Members

        public override object WzValue {
            get { return GetBytes(); }
            set {
                if (value is byte[] v)
                    SetDataUnsafe(v);
            }
        }

        /// <summary>
        /// The parent of the object
        /// </summary>
        public override AWzObject Parent { get { return mParent; } internal set { mParent = value; } }

        /// <summary>
        /// The image that this property is contained in
        /// </summary>
        public override WzImage ParentImage { get { return mImgParent; } internal set { mImgParent = value; } }

        /// <summary>
        /// The name of the property
        /// </summary>
        public override string Name { get { return mName; } set { mName = value; } }

        /// <summary>
        /// The WzPropertyType of the property
        /// </summary>
        public override WzPropertyType PropertyType { get { return WzPropertyType.Video; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            byte[] data = GetBytes();
            pWriter.WriteStringValue("Canvas#Video", 0x73, 0x1B);
            pWriter.Write((byte)0);
            pWriter.WriteCompressedInt(data.Length);
            pWriter.Write(data);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedTag("Canvas#Video", Name));
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public override void Dispose() {
            mName = null;
            mBytes = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// Creates a blank WzRawDataProperty
        /// </summary>
        public WzVideoProperty() {
        }

        /// <summary>
        /// Creates a WzRawDataProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzVideoProperty(string pName) {
            mName = pName;
        }

        public void SetDataUnsafe(byte[] data) {
            mBytes = data;
        }

        #endregion

        #region Parsing Methods


        internal void ParseVideo(WzBinaryReader pReader) {
            pReader.BaseStream.Position++;
            int rawDataVer = pReader.ReadByte();
            if (rawDataVer == 1) {  // introduced in KMST 1177
                pReader.BaseStream.Position += 2;
                AddProperties(ParsePropertyList(ParentImage.Offset, pReader, this, mImgParent));
            }
            pReader.BaseStream.Position++;
            mOffsets = pReader.BaseStream.Position;
            int dataLen = pReader.ReadCompressedInt();
            pReader.BaseStream.Position += dataLen;
            mWzReader = pReader;
        }

        public byte[] GetBytes(bool pSaveInMemory = false) {
            if (mBytes != null)
                return mBytes;
            if (mWzReader == null)
                return null;
            long currentPos = mWzReader.BaseStream.Position;
            mWzReader.BaseStream.Position = mOffsets;
            int dataLen = mWzReader.ReadCompressedInt();
            mBytes = mWzReader.ReadBytes(dataLen);
            mWzReader.BaseStream.Position = currentPos;
            if (pSaveInMemory)
                return mBytes;
            byte[] result = mBytes;
            mBytes = null;
            return result;
        }

        public void SaveToFile(string pFilePath) {
            File.WriteAllBytes(pFilePath, GetBytes());
        }

        #endregion

        #region Cast Values

        internal override byte[] ToBytes(byte[] pDef = null) {
            return GetBytes();
        }

        #endregion
    }
}