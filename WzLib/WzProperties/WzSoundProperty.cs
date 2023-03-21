using MapleLib.WzLib.Util;
using NAudio.Wave;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that contains data for an MP3 file
    /// </summary>
    public class WzSoundProperty : AWzImageProperty, IExtended {
        #region Fields

        internal string mName;
        internal byte[] mMp3bytes;
        internal AWzObject mParent;
        internal int mLenMs;
        internal WzImage mImgParent;
        internal WzBinaryReader mWzReader;
        internal long mOffsets;
        internal WaveFormat wavFormat;
        internal int headerLen;
        public static readonly byte[] SoundHeader = new byte[] { 0x02,
            0x83, 0xEB, 0x36, 0xE4, 0x4F, 0x52, 0xCE, 0x11, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70,
            0x8B, 0xEB, 0x36, 0xE4, 0x4F, 0x52, 0xCE, 0x11, 0x9F, 0x53, 0x00, 0x20, 0xAF, 0x0B, 0xA7, 0x70,
            0x00,
            0x01,
            0x81, 0x9F, 0x58, 0x05, 0x56, 0xC3, 0xCE, 0x11, 0xBF, 0x01, 0x00, 0xAA, 0x00, 0x55, 0x59, 0x5A
        };
        #endregion

        #region Inherited Members

        public override object WzValue {
            get { return GetBytes(); }
            set {
                if (value is byte[] v)
                    SetDataUnsafe(v);
                else
                    SetDataUnsafe(CreateCustomProperty("temp", (string)value).GetBytes());
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
        public override WzPropertyType PropertyType { get { return WzPropertyType.Sound; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            byte[] data = GetBytes();
            pWriter.WriteStringValue("Sound_DX8", 0x73, 0x1B);
            pWriter.Write((byte)0);
            pWriter.WriteCompressedInt(data.Length);
            pWriter.WriteCompressedInt(0);
            pWriter.Write(data);
        }

        public override void ExportXml(StreamWriter pWriter, int pLevel) {
            pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.EmptyNamedTag("WzSound", Name));
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public override void Dispose() {
            mName = null;
            mMp3bytes = null;
            wavFormat = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// Length of the mp3 file in milliseconds
        /// </summary>
        public int Length { get { return mLenMs; } }

        /// <summary>
        /// Creates a blank WzSoundProperty
        /// </summary>
        public WzSoundProperty() {
        }

        /// <summary>
        /// Creates a WzSoundProperty with the specified name
        /// </summary>
        /// <param name="pName">The name of the property</param>
        public WzSoundProperty(string pName) {
            mName = pName;
        }

        public void SetDataUnsafe(byte[] data) {
            mMp3bytes = data;
        }

        public static WzSoundProperty CreateCustomProperty(string name, string file) {
            WzSoundProperty newProp = new WzSoundProperty(name);
            MP3Header header = new MP3Header();
            header.ReadMP3Information(file);
            newProp.mLenMs = header.intLength * 1000;
            byte[] frequencyBytes = BitConverter.GetBytes(header.intFrequency);
            byte[] headerBytes = new byte[SoundHeader.Length];
            Array.Copy(SoundHeader, headerBytes, headerBytes.Length);
            for (int i = 0; i < 4; i++) {
                headerBytes[56 + i] = frequencyBytes[i];
            }
            newProp.mMp3bytes = WzTool.Combine(headerBytes, File.ReadAllBytes(file));
            return newProp;
        }

        #endregion

        #region Parsing Methods

        private static T BytesToStruct<T>(byte[] data) where T : new() {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            } finally {
                handle.Free();
            }
        }

        private static T BytesToStructConstructorless<T>(byte[] data) {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                T obj = (T)FormatterServices.GetUninitializedObject(typeof(T));
                Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject(), obj);
                return obj;
            } finally {
                handle.Free();
            }
        }

        private void ParseWzSoundPropertyHeader(byte[] wavHeader) {
            if (wavHeader.Length < Marshal.SizeOf<WaveFormat>())
                return;

            WaveFormat wavFmt = BytesToStruct<WaveFormat>(wavHeader);
            // parse to mp3 header
            if (wavFmt.Encoding == WaveFormatEncoding.MpegLayer3 && wavHeader.Length >= Marshal.SizeOf<Mp3WaveFormat>()) {
                this.wavFormat = BytesToStructConstructorless<Mp3WaveFormat>(wavHeader);
            } else if (wavFmt.Encoding == WaveFormatEncoding.Pcm) {
                this.wavFormat = wavFmt;
            }
        }

        internal void ParseSound(WzBinaryReader pReader) {
            pReader.BaseStream.Position++;
            mOffsets = pReader.BaseStream.Position;
            int soundDataLen = pReader.ReadCompressedInt();
            mLenMs = pReader.ReadCompressedInt();

            pReader.BaseStream.Position += SoundHeader.Length; // Skip GUIDs
            int wavHeaderLen = pReader.ReadByte();
            headerLen = SoundHeader.Length + 1 + wavHeaderLen;
            byte[] wavHeader = pReader.ReadBytes(wavHeaderLen);
            ParseWzSoundPropertyHeader(wavHeader);
            // mMp3bytes = pReader.ReadBytes(soundDataLen); // Save memory

            pReader.BaseStream.Position += soundDataLen;
            mWzReader = pReader;
        }

        public byte[] GetBytes(bool pSaveInMemory = false) {
            if (mMp3bytes != null)
                return mMp3bytes;
            if (mWzReader == null)
                return null;
            long currentPos = mWzReader.BaseStream.Position;
            mWzReader.BaseStream.Position = mOffsets;
            int soundDataLen = mWzReader.ReadCompressedInt();
            mWzReader.ReadCompressedInt();
            mWzReader.BaseStream.Position += headerLen;
            if (wavFormat != null && wavFormat.Encoding == WaveFormatEncoding.Pcm && wavFormat.SampleRate != soundDataLen && wavFormat.SampleRate != wavFormat.AverageBytesPerSecond) {
                byte[] soundData = mWzReader.ReadBytes(soundDataLen);
                using (MemoryStream ms = new MemoryStream()) {
                    using (WaveFileWriter writer = new WaveFileWriter(ms, wavFormat)) {
                        writer.Write(soundData, 0, soundDataLen);
                    }
                    mMp3bytes = ms.ToArray();
                }
            } else {
                mMp3bytes = mWzReader.ReadBytes(soundDataLen);
            }
            mWzReader.BaseStream.Position = currentPos;
            if (pSaveInMemory)
                return mMp3bytes;
            byte[] result = mMp3bytes;
            mMp3bytes = null;
            return result;
        }

        public void SaveToFile(string pFilePath) {
            File.WriteAllBytes(pFilePath, GetBytes());
        }
        public string GetExtension() {
            if (wavFormat != null && wavFormat.Encoding == WaveFormatEncoding.MpegLayer3)
                return ".mp3";
            if (mName.Equals("FONT_DATA"))
                return ".ttf";
            if (wavFormat != null && wavFormat.Encoding == WaveFormatEncoding.Pcm && wavFormat.SampleRate != wavFormat.AverageBytesPerSecond)
                return ".wav";
            return "";
        }

        #endregion

        #region Cast Values

        internal override byte[] ToBytes(byte[] pDef = null) {
            return GetBytes();
        }

        #endregion
    }
}