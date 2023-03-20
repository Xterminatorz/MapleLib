using MapleLib.WzLib.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace MapleLib.WzLib.WzProperties {
    /// <summary>
    /// A property that contains the information for a bitmap
    /// </summary>
    public class WzPngProperty : AWzImageProperty {
        #region Fields

        internal int mWidth, mHeight, mFormat, mFormat2;
        internal byte[] mCompressedBytes;
        internal Bitmap mPNG;
        //internal bool mIsNew;
        internal AWzObject mParent;
        internal WzImage mImgParent;
        internal WzBinaryReader mWzReader;
        internal long mOffsets;

        #endregion

        #region Inherited Members

        public override object WzValue {
            get { return GetPNG(); }
            set {
                if (value is Bitmap bitmap)
                    SetPNG(bitmap);
                else
                    mCompressedBytes = (byte[])value;
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
        public override string Name { get { return "PNG"; } set { } }

        /// <summary>
        /// The WzPropertyType of the property
        /// </summary>
        public override WzPropertyType PropertyType { get { return WzPropertyType.PNG; } }

        public override void WriteValue(WzBinaryWriter pWriter) {
            throw new NotImplementedException("Cannot write a PngProperty");
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        public override void Dispose() {
            mCompressedBytes = null;
            if (mPNG == null)
                return;
            mPNG.Dispose();
            mPNG = null;
        }

        #endregion

        #region Custom Members

        /// <summary>
        /// The width of the bitmap
        /// </summary>
        public int Width { get { return mWidth; } set { mWidth = value; } }

        /// <summary>
        /// The height of the bitmap
        /// </summary>
        public int Height { get { return mHeight; } set { mHeight = value; } }

        /// <summary>
        /// The format of the bitmap
        /// </summary>
        public int Format {
            get { return mFormat + mFormat2; }
            set {
                mFormat = value;
                mFormat2 = 0;
            }
        }

        /// <summary>
        /// Creates a blank WzPngProperty
        /// </summary>
        public WzPngProperty() {
        }

        internal WzPngProperty(WzBinaryReader pReader) {
            // Read compressed bytes
            mWidth = pReader.ReadCompressedInt();
            mHeight = pReader.ReadCompressedInt();
            mFormat = pReader.ReadCompressedInt();
            mFormat2 = pReader.ReadByte();
            pReader.BaseStream.Position += 4;
            mOffsets = pReader.BaseStream.Position;
            int len = pReader.ReadInt32() - 1;
            pReader.BaseStream.Position += 1;

            if (len > 0)
                pReader.BaseStream.Position += len;
            mWzReader = pReader;
        }

        #endregion

        #region Parsing Methods

        public byte[] GetCompressedBytes(bool pSaveInMemory = false) {
            if (mCompressedBytes == null) {
                long pos = mWzReader.BaseStream.Position;
                mWzReader.BaseStream.Position = mOffsets;
                int len = mWzReader.ReadInt32() - 1;
                mWzReader.BaseStream.Position += 1;
                if (len > 0)
                    mCompressedBytes = mWzReader.ReadBytes(len);
                mWzReader.BaseStream.Position = pos;
                if (!pSaveInMemory) {
                    mCompressedBytes = null;
                    return mCompressedBytes;
                }
            }
            return mCompressedBytes;
        }

        public void SetPNG(Bitmap pPng) {
            mPNG = pPng;
            CompressPng(pPng);
        }

        public Bitmap GetPNG(bool pSaveInMemory = false) {
            if (mPNG == null) {
                long pos = mWzReader.BaseStream.Position;
                mWzReader.BaseStream.Position = mOffsets;
                int len = mWzReader.ReadInt32() - 1;
                mWzReader.BaseStream.Position += 1;
                if (len > 0)
                    mCompressedBytes = mWzReader.ReadBytes(len);
                ParsePng();
                mWzReader.BaseStream.Position = pos;
                if (!pSaveInMemory) {
                    Bitmap pngImage = mPNG;
                    mPNG = null;
                    mCompressedBytes = null;
                    return pngImage;
                }
            }
            return mPNG;
        }

        internal byte[] Decompress(byte[] pCompressedBuffer, int pDecompressedSize) {
            MemoryStream memStream = new MemoryStream();
            memStream.Write(pCompressedBuffer, 2, pCompressedBuffer.Length - 2);
            byte[] buffer = new byte[pDecompressedSize];
            memStream.Position = 0;
            DeflateStream zip = new DeflateStream(memStream, CompressionMode.Decompress);
            zip.Read(buffer, 0, buffer.Length);
            zip.Close();
            zip.Dispose();
            memStream.Close();
            memStream.Dispose();
            return buffer;
        }

        internal byte[] Compress(byte[] pDecompressedBuffer) {
            MemoryStream memStream = new MemoryStream();
            DeflateStream zip = new DeflateStream(memStream, CompressionMode.Compress, true);
            zip.Write(pDecompressedBuffer, 0, pDecompressedBuffer.Length);
            zip.Close();
            memStream.Position = 0;
            byte[] buffer = new byte[memStream.Length + 2];
            Console.WriteLine(BitConverter.ToString(memStream.ToArray()));
            memStream.Read(buffer, 2, buffer.Length - 2);
            memStream.Close();
            memStream.Dispose();
            zip.Dispose();
            Buffer.BlockCopy(new byte[] { 0x78, 0x9C }, 0, buffer, 0, 2);
            return buffer;
        }

        internal void ParsePng() {
            DeflateStream zlib;
            int uncompressedSize;
            int x = 0, y = 0, b, g;
            Bitmap bmp = null;
            BitmapData bmpData;
            byte[] decBuf;

            BinaryReader reader = new BinaryReader(new MemoryStream(mCompressedBytes));
            ushort header = reader.ReadUInt16();
            if (header == 0x9C78) {
                zlib = new DeflateStream(reader.BaseStream, CompressionMode.Decompress);
            } else {
                reader.BaseStream.Position -= 2;
                MemoryStream dataStream = new MemoryStream();
                int blocksize;
                int endOfPng = mCompressedBytes.Length;
                while (reader.BaseStream.Position < endOfPng) {
                    blocksize = reader.ReadInt32();
                    for (int i = 0; i < blocksize; i++) {
                        dataStream.WriteByte((byte)(reader.ReadByte() ^ mWzReader.WzKey[i]));
                    }
                }
                dataStream.Position = 2;
                zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
            }

            switch (mFormat + mFormat2) {
                case 1:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    uncompressedSize = mWidth * mHeight * 2;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    byte[] argb = new Byte[uncompressedSize * 2];
                    for (int i = 0; i < uncompressedSize; i++) {
                        b = decBuf[i] & 0x0F;
                        b |= (b << 4);
                        argb[i * 2] = (byte)b;
                        g = decBuf[i] & 0xF0;
                        g |= (g >> 4);
                        argb[i * 2 + 1] = (byte)g;
                    }
                    Marshal.Copy(argb, 0, bmpData.Scan0, argb.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                case 2:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    uncompressedSize = mWidth * mHeight * 4;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                case 3:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format32bppArgb);
                    decBuf = new byte[((int)Math.Ceiling(mWidth / 4.0)) * 4 * ((int)Math.Ceiling(mHeight / 4.0)) * 4 / 8];
                    zlib.Read(decBuf, 0, decBuf.Length);
                    int[] argb2 = new int[mWidth * mHeight]; {
                        int index;
                        int index2;
                        int p;
                        int w = ((int)Math.Ceiling(mWidth / 4.0));
                        int h = ((int)Math.Ceiling(mWidth / 4.0));
                        for (int y1 = 0; y1 < h; y1++) {
                            for (int x2 = 0; x2 < w; x2++) {
                                index = (x2 + y1 * w) * 2;
                                index2 = x2 * 4 + y1 * mWidth * 4;
                                p = (decBuf[index] & 0x0F) | ((decBuf[index] & 0x0F) << 4);
                                p |= ((decBuf[index] & 0xF0) | ((decBuf[index] & 0xF0) >> 4)) << 8;
                                p |= ((decBuf[index + 1] & 0x0F) | ((decBuf[index + 1] & 0x0F) << 4)) << 16;
                                p |= ((decBuf[index + 1] & 0xF0) | ((decBuf[index] & 0xF0) >> 4)) << 24;

                                for (int i = 0; i < 4; i++) {
                                    if (x2 * 4 + i < mWidth) {
                                        argb2[index2 + i] = p;
                                    } else {
                                        break;
                                    }
                                }
                            }
                            index2 = y1 * mWidth * 4;
                            for (int j = 1; j < 4; j++) {
                                if (y1 * 4 + j < mHeight) {
                                    Array.Copy(argb2, index2, argb2, index2 + j * mWidth, mWidth);
                                } else {
                                    break;
                                }
                            }
                        }
                    }
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb2, 0, bmpData.Scan0, argb2.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                case 257:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format16bppArgb1555);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    uncompressedSize = mWidth * mHeight * 2;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, decBuf.Length);
                    int stride = bmp.Width * 2;
                    if (bmpData.Stride == stride) {
                        Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                    } else {
                        for (int y3 = 0; y3 < bmpData.Height; y3++) {
                            Marshal.Copy(decBuf, stride * y3, bmpData.Scan0 + bmpData.Stride * y3, stride);
                        }
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case 513:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format16bppRgb565);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    uncompressedSize = mWidth * mHeight * 2;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                case 517:
                    bmp = new Bitmap(mWidth, mHeight);
                    uncompressedSize = mWidth * mHeight / 128;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    byte iB;
                    for (int i = 0; i < uncompressedSize; i++) {
                        for (byte j = 0; j < 8; j++) {
                            iB = Convert.ToByte(((decBuf[i] & (0x01 << (7 - j))) >> (7 - j)) * 0xFF);
                            for (int k = 0; k < 16; k++) {
                                if (x == mWidth) {
                                    x = 0;
                                    y++;
                                }
                                bmp.SetPixel(x, y, Color.FromArgb(0xFF, iB, iB, iB));
                                x++;
                            }
                        }
                    }
                    break;
                case 1026:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    uncompressedSize = mWidth * mHeight;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    decBuf = GetPixelDataDXT3(decBuf, Width, Height);
                    Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                case 2050:
                    bmp = new Bitmap(mWidth, mHeight, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, mWidth, mHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    uncompressedSize = mWidth * mHeight;
                    decBuf = new byte[uncompressedSize];
                    zlib.Read(decBuf, 0, uncompressedSize);
                    decBuf = GetPixelDataDXT5(decBuf, Width, Height);
                    Marshal.Copy(decBuf, 0, bmpData.Scan0, decBuf.Length);
                    bmp.UnlockBits(bmpData);
                    break;
                default:
                    Console.WriteLine(string.Format("Unknown PNG format {0} {1}", mFormat, mFormat2));
                    break;
            }
            mPNG = bmp;
        }

        #region DXT Format Parser
        private static byte[] GetPixelDataDXT3(byte[] rawData, int width, int height) {
            byte[] pixel = new byte[width * height * 4];

            Color[] colorTable = new Color[4];
            int[] colorIdxTable = new int[16];
            byte[] alphaTable = new byte[16];
            for (int y = 0; y < height; y += 4) {
                for (int x = 0; x < width; x += 4) {
                    int off = x * 4 + y * width;
                    ExpandAlphaTableDXT3(alphaTable, rawData, off);
                    ushort u0 = BitConverter.ToUInt16(rawData, off + 8);
                    ushort u1 = BitConverter.ToUInt16(rawData, off + 10);
                    ExpandColorTable(colorTable, u0, u1);
                    ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

                    for (int j = 0; j < 4; j++) {
                        for (int i = 0; i < 4; i++) {
                            SetPixel(pixel,
                                x + i,
                                y + j,
                                width,
                                colorTable[colorIdxTable[j * 4 + i]],
                                alphaTable[j * 4 + i]);
                        }
                    }
                }
            }

            return pixel;
        }

        public static byte[] GetPixelDataDXT5(byte[] rawData, int width, int height) {
            byte[] pixel = new byte[width * height * 4];

            Color[] colorTable = new Color[4];
            int[] colorIdxTable = new int[16];
            byte[] alphaTable = new byte[8];
            int[] alphaIdxTable = new int[16];
            for (int y = 0; y < height; y += 4) {
                for (int x = 0; x < width; x += 4) {
                    int off = x * 4 + y * width;
                    ExpandAlphaTableDXT5(alphaTable, rawData[off + 0], rawData[off + 1]);
                    ExpandAlphaIndexTableDXT5(alphaIdxTable, rawData, off + 2);
                    ushort u0 = BitConverter.ToUInt16(rawData, off + 8);
                    ushort u1 = BitConverter.ToUInt16(rawData, off + 10);
                    ExpandColorTable(colorTable, u0, u1);
                    ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

                    for (int j = 0; j < 4; j++) {
                        for (int i = 0; i < 4; i++) {
                            SetPixel(pixel,
                                x + i,
                                y + j,
                                width,
                                colorTable[colorIdxTable[j * 4 + i]],
                                alphaTable[alphaIdxTable[j * 4 + i]]);
                        }
                    }
                }
            }

            return pixel;
        }

        private static void SetPixel(byte[] pixelData, int x, int y, int width, Color color, byte alpha) {
            int offset = (y * width + x) * 4;
            pixelData[offset + 0] = color.B;
            pixelData[offset + 1] = color.G;
            pixelData[offset + 2] = color.R;
            pixelData[offset + 3] = alpha;
        }

        private static void ExpandColorTable(Color[] color, ushort u0, ushort u1) {
            color[0] = RGB565ToColor(u0);
            color[1] = RGB565ToColor(u1);
            color[2] = Color.FromArgb(0xff, (color[0].R * 2 + color[1].R + 1) / 3, (color[0].G * 2 + color[1].G + 1) / 3, (color[0].B * 2 + color[1].B + 1) / 3);
            color[3] = Color.FromArgb(0xff, (color[0].R + color[1].R * 2 + 1) / 3, (color[0].G + color[1].G * 2 + 1) / 3, (color[0].B + color[1].B * 2 + 1) / 3);
        }

        private static void ExpandColorIndexTable(int[] colorIndex, byte[] rawData, int offset) {
            for (int i = 0; i < 16; i += 4, offset++) {
                colorIndex[i + 0] = (rawData[offset] & 0x03);
                colorIndex[i + 1] = (rawData[offset] & 0x0c) >> 2;
                colorIndex[i + 2] = (rawData[offset] & 0x30) >> 4;
                colorIndex[i + 3] = (rawData[offset] & 0xc0) >> 6;
            }
        }

        private static void ExpandAlphaTableDXT3(byte[] alpha, byte[] rawData, int offset) {
            for (int i = 0; i < 16; i += 2, offset++) {
                alpha[i + 0] = (byte)(rawData[offset] & 0x0f);
                alpha[i + 1] = (byte)((rawData[offset] & 0xf0) >> 4);
            }
            for (int i = 0; i < 16; i++) {
                alpha[i] = (byte)(alpha[i] | (alpha[i] << 4));
            }
        }

        private static void ExpandAlphaTableDXT5(byte[] alpha, byte a0, byte a1) {
            alpha[0] = a0;
            alpha[1] = a1;
            if (a0 > a1) {
                for (int i = 2; i < 8; i++) {
                    alpha[i] = (byte)(((8 - i) * a0 + (i - 1) * a1 + 3) / 7);
                }
            } else {
                for (int i = 2; i < 6; i++) {
                    alpha[i] = (byte)(((6 - i) * a0 + (i - 1) * a1 + 2) / 5);
                }
                alpha[6] = 0;
                alpha[7] = 255;
            }
        }

        private static void ExpandAlphaIndexTableDXT5(int[] alphaIndex, byte[] rawData, int offset) {
            for (int i = 0; i < 16; i += 8, offset += 3) {
                int flags = rawData[offset]
                    | (rawData[offset + 1] << 8)
                    | (rawData[offset + 2] << 16);
                for (int j = 0; j < 8; j++) {
                    int mask = 0x07 << (3 * j);
                    alphaIndex[i + j] = (flags & mask) >> (3 * j);
                }
            }
        }
        #endregion

        private static Color RGB565ToColor(ushort val) {
            const int rgb565_mask_r = 0xf800;
            const int rgb565_mask_g = 0x07e0;
            const int rgb565_mask_b = 0x001f;
            int r = (val & rgb565_mask_r) >> 11;
            int g = (val & rgb565_mask_g) >> 5;
            int b = (val & rgb565_mask_b);
            var c = Color.FromArgb(
                (r << 3) | (r >> 2),
                (g << 2) | (g >> 4),
                (b << 3) | (b >> 2));
            return c;
        }
        #endregion

        internal void CompressPng(Bitmap pBmp) {
            byte[] buf = new byte[pBmp.Width * pBmp.Height * 8];
            mFormat = 2;
            mFormat2 = 0;
            mWidth = pBmp.Width;
            mHeight = pBmp.Height;

            int curPos = 0;
            for (int i = 0; i < mHeight; i++)
                for (int j = 0; j < mWidth; j++) {
                    Color curPixel = pBmp.GetPixel(j, i);
                    buf[curPos] = curPixel.B;
                    buf[curPos + 1] = curPixel.G;
                    buf[curPos + 2] = curPixel.R;
                    buf[curPos + 3] = curPixel.A;
                    curPos += 4;
                }
            mCompressedBytes = Compress(buf);
            //if (!mIsNew)
            //	return;
            MemoryStream memStream = new MemoryStream();
            WzBinaryWriter writer = new WzBinaryWriter(memStream, WzTool.GetIvByMapleVersion(WzMapleVersion.GMS));
            writer.Write(2);
            for (int i = 0; i < 2; i++) {
                writer.Write((byte)(mCompressedBytes[i] ^ writer.WzKey[i]));
            }
            writer.Write(mCompressedBytes.Length - 2);
            for (int i = 2; i < mCompressedBytes.Length; i++)
                writer.Write((byte)(mCompressedBytes[i] ^ writer.WzKey[i - 2]));
            mCompressedBytes = memStream.GetBuffer();
            writer.Close();
        }

        #region Cast Values

        internal override WzPngProperty ToPngProperty(WzPngProperty pDef = null) {
            return this;
        }

        internal override Bitmap ToBitmap(Bitmap pDef = null) {
            return GetPNG();
        }

        #endregion
    }
}