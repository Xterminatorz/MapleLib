using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib {
	/// <summary>
	/// A .img contained in a wz directory
	/// </summary>
	public class WzImage : WzSubProperty {
		#region Fields

		internal bool mParsed;
        internal bool initialParse;
        internal int mSize, checksum;
		internal uint mOffset;
		internal WzBinaryReader mReader;
		internal int mBlockStart;
		internal long mTempFileStart;
		internal long mTempFileEnd;
        internal HashSet<WzImage> referencedImgs = new HashSet<WzImage>();

        #endregion

        /// <summary>
        /// The parent of the object
        /// </summary>
        public override AWzObject Parent { get { return mParent; } internal set { mParent = value; } }

		/// <summary>
		/// The name of the image
		/// </summary>
		public override string Name { get { return mName; } set { mName = value; } }

		/// <summary>
		/// Is the object parsed
		/// </summary>
		public bool Parsed { get { return mParsed; } }

        /// <summary>
        /// Is this the initial parse
        /// </summary>
        public bool InitialParse { get { return initialParse; } }

        /// <summary>
        /// The size in the wz file of the image
        /// </summary>
        public int BlockSize { get { return mSize; } set { mSize = value; } }

		/// <summary>
		/// The checksum of the image
		/// </summary>
		public int Checksum { get { return checksum; } set { checksum = value; } }

		/// <summary>
		/// The offset of the image
		/// </summary>
		public uint Offset { get { return mOffset; } set { mOffset = value; } }

		public int BlockStart { get { return mBlockStart; } }

		/// <summary>
		/// The properties contained in the image
		/// </summary>
		public override List<AWzImageProperty> WzProperties {
			get {
				if (mReader != null && !mParsed) {
					ParseImage();
				}
				return mProperties;
			}
		}

		/// <summary>
		/// Gets a wz property by it's name
		/// </summary>
		/// <param name="pName">The name of the property</param>
		/// <returns>The wz property with the specified name</returns>
		public override AWzImageProperty this[string pName] {
			get {
				if (mReader != null && !mParsed)
					ParseImage();
				return mProperties.FirstOrDefault(iwp => iwp.Name.ToLower() == pName.ToLower());
			}
		}

		/// <summary>
		/// The WzObjectType of the image
		/// </summary>
		public override WzObjectType ObjectType {
			get {
				if (mReader != null && !mParsed)
					ParseImage();
				return WzObjectType.Image;
			}
		}

		/// <summary>
		/// Creates a blank WzImage
		/// </summary>
		public WzImage() {
		}

		/// <summary>
		/// Creates a WzImage with the given name
		/// </summary>
		/// <param name="pName">The name of the image</param>
		public WzImage(string pName) {
			mName = pName;
            initialParse = true;
		}

		public WzImage(string pName, Stream pDataStream, WzMapleVersion pMapleVersion) {
			mName = pName;
			mReader = new WzBinaryReader(pDataStream, WzTool.GetIvByMapleVersion(pMapleVersion));
            initialParse = true;
        }

		internal WzImage(string pName, WzBinaryReader pReader) {
			mName = pName;
			mReader = pReader;
			mBlockStart = (int) pReader.BaseStream.Position;
            initialParse = true;
        }

        /*internal WzImage(string pName, WzBinaryReader pReader, byte[] wzKey) {
			mName = pName;
			mReader = pReader;
			mBlockStart = (int) pReader.BaseStream.Position;
			WzKey = wzKey;
		}*/

        public void PartialDispose() {
            if (mProperties == null)
                return;
            foreach (AWzImageProperty prop in mProperties)
                prop.Dispose();
            mProperties.Clear();
            foreach (WzImage img in referencedImgs)
                img.PartialDispose();
            referencedImgs.Clear();
            UnparseImage();
            initialParse = false;
        }

        public override void Dispose() {
			mName = null;
			mReader = null;
            if (mProperties != null) {
                foreach (AWzImageProperty prop in mProperties)
                    prop.Dispose();
                mProperties.Clear();
                mProperties = null;
            }
            if (referencedImgs != null) {
                foreach (WzImage img in referencedImgs)
                    img.Dispose();
                referencedImgs.Clear();
                referencedImgs = null;
            }
            mParsed = false;
        }

		/// <summary>
		/// Parses the image from the wz filetod
		/// </summary>
		public void ParseImage() {
			//long originalPos = mReader.BaseStream.Position;
			//byte[] keyCopy = mReader.WzKey;
			//Array.Copy(mReader.WzKey, keyCopy, mReader.WzKey.Length);
			mReader.BaseStream.Position = mOffset;
			byte b = mReader.ReadByte();
			if (b != 0x73 || mReader.ReadString() != "Property" || mReader.ReadUInt16() != 0)
				return;
            List<AWzImageProperty> properties = ParsePropertyList(mOffset, mReader, this, this);
            mProperties.AddRange(properties);
            properties.Clear();
            mParsed = true;
		}

		public byte[] DataBlock {
			get {
				byte[] blockData = null;
				if (mReader != null && mSize > 0) {
					blockData = mReader.ReadBytes(mSize);
					mReader.BaseStream.Position = mBlockStart;
				}
				return blockData;
			}
		}

		public void UnparseImage() {
			mParsed = false;
			mProperties = new List<AWzImageProperty>();
		}

        public void AddReferencedImage(WzImage img) {
            if (!Equals(img) && !img.InitialParse)
                referencedImgs.Add(img);
        }

		internal void SaveImage(WzBinaryWriter pWriter) {
			if (mReader != null && !mParsed)
				ParseImage();
			long startPos = pWriter.BaseStream.Position;
			WriteValue(pWriter);
			pWriter.StringCache.Clear();
			mSize = (int) (pWriter.BaseStream.Position - startPos);
		}

		public void ExportXml(StreamWriter pWriter, bool pOneFile, int pLevel) {
			if (pOneFile) {
				pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.OpenNamedTag("WzImage", mName, true));
				DumpPropertyList(pWriter, pLevel, WzProperties);
				pWriter.WriteLine(XmlUtil.Indentation(pLevel) + XmlUtil.CloseTag("WzImage"));
			} else {
				throw new Exception("Under Construction");
			}
		}
	}
}