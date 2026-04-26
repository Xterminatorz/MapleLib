using MapleLib.WzLib.Util;
using Microsoft.SqlServer.Server;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using MapleLib.WzLib.Utilities;

namespace MapleLib.WzLib.WzProperties
{
    /// <summary>
    /// A property that contains the information for a bitmap
    /// </summary>
    public class WzPngProperty : AWzImageProperty
    {
        #region Fields

        internal int mWidth, mHeight, mFormat, mScale, mPages, mUnk, mLength;
        internal byte[] mCompressedBytes;
        internal Bitmap mPNG;
        //internal bool mIsNew;
        internal AWzObject mParent;
        internal WzImage mImgParent;
        internal WzBinaryReader mWzReader;
        internal long mOffsets;

        #endregion

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
            Format = (Wz_TextureFormat)mFormat;
            mScale = pReader.ReadByte();
            mPages = pReader.ReadCompressedInt();
            mUnk = pReader.ReadCompressedInt();
            pReader.BaseStream.Position += 2;
            mOffsets = pReader.BaseStream.Position;
            mLength = pReader.ReadInt32();
            pReader.BaseStream.Position += mLength;
            mWzReader = pReader;
        }

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
        public Wz_TextureFormat Format { get; set; }

        public int Scale { get; set; }

        /// <summary>
        /// The actual width and height sacale is pow(2, value).
        /// </summary>
        public int ActualScale => this.Scale > 0 ? (1 << this.Scale) : 1;

        public int Pages { get; set; }

        public int ActualPages => this.Pages > 0 ? this.Pages : 1;

        public int GetRawDataSize() => this.GetRawDataSizePerPage() * this.ActualPages;

        public int GetRawDataSizePerPage() => GetUncompressedDataSize(this.Format, this.ActualScale, this.Width, this.Height);

        #endregion

        #region Parsing Methods

        public byte[] GetRawData() {
            int dataSize = this.GetRawDataSize();
            byte[] rawData = new byte[dataSize];
            int count = this.GetRawData(rawData);
            if (count != dataSize) {
                throw new Exception($"Data size mismatch. (expected: {dataSize}, actual: {count})");
            }
            return rawData;
        }

        public int GetRawData(Span<byte> buffer) {
            return this.GetRawData(0, buffer);
        }

        public int GetRawData(int skipBytes, Span<byte> buffer) {
            lock (mWzReader.BaseStream) {
                using (var zlib = this.UnsafeOpenRead()) {
                    if (skipBytes > 0) {
                        var pool = ArrayPool<byte>.Shared;
                        byte[] tempBuffer = pool.Rent(4096);
                        try {
                            while (skipBytes > 0) {
                                int len = zlib.Read(tempBuffer, 0, (int)Math.Min(skipBytes, tempBuffer.Length));
                                if (len == 0) {
                                    break;
                                }
                                skipBytes -= len;
                            }
                        } finally {
                            pool.Return(tempBuffer);
                        }
                    }
                    return StreamExtensions.ReadAvailableBytes(zlib, buffer);
                }
            }
        }

        public Stream UnsafeOpenRead() {
            DeflateStream zlib;
            BinaryReader reader = new BinaryReader(new MemoryStream(mCompressedBytes));
            reader.BaseStream.Position++;
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
            return zlib;
        }

        public byte[] GetCompressedBytes(bool pSaveInMemory = false) {
            if (mCompressedBytes == null) {
                long pos = mWzReader.BaseStream.Position;
                mWzReader.BaseStream.Position = mOffsets;
                int len = mWzReader.ReadInt32();
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
                int len = mWzReader.ReadInt32();
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
            ParsePng(0);
        }

        internal void ParsePng(int page) {
            if (this.Pages > 0) {
                if (page < 0 || page >= this.Pages) {
                    throw new ArgumentOutOfRangeException(nameof(page));
                }
            } else {
                // ignore it, always pick the first page.
                page = 0;
            }
            Bitmap bmp = null;
            BitmapData bmpData;
            int dataSizePerPage = this.GetRawDataSizePerPage();
            byte[] decBuf = new byte[dataSizePerPage];
            int actualBytes = this.GetRawData(page * dataSizePerPage, decBuf);
            if (actualBytes != dataSizePerPage)
                throw new ArgumentException($"Not enough bytes have been read. (actual:{actualBytes}, expected:{dataSizePerPage})");
            switch (this.Format) {
                case Wz_TextureFormat.ARGB4444 when this.ActualScale == 1:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.BGRA4444ToBGRA32(decBuf, output);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.ARGB8888 when this.ActualScale == 1:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        decBuf.CopyTo(output);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.ARGB1555 when this.ActualScale == 1:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppArgb1555);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    CopyBmpDataWithStride(decBuf, bmp.Width * 2, bmpData);
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.RGB565 when this.ActualScale == 1 || this.ActualScale == 16:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppRgb565);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    if (this.ActualScale == 1) // old form(513)
                    {
                        CopyBmpDataWithStride(decBuf, bmp.Width * 2, bmpData);
                    } else if (this.ActualScale == 16) // old form(517)
                      {
                        unsafe {
                            Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                            int rawDataWidth = this.Width / this.ActualScale;
                            int rawDataHeight = this.Height / this.ActualScale;
                            ImageCodec.ScalePixels(decBuf, 2, rawDataWidth, rawDataWidth * 2, rawDataHeight, this.ActualScale, this.ActualScale, output, bmpData.Stride);
                        }
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.DXT3:
                    if (this.ActualScale != 1)
                        throw new Exception("DXT3 does not support scale.");
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.DXT3ToBGRA32(decBuf, output, this.Width, this.Width * 4, this.Height);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.DXT5:
                    if (this.ActualScale != 1)
                        throw new Exception("DXT5 does not support scale.");
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.DXT5ToBGRA32(decBuf, output, this.Width, this.Width * 4, this.Height);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.RGBA1010102 when this.ActualScale == 1:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        //int pageSize = this.Width * this.Height * 4;
                        Span<byte> outputPixels = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.R10G10B10A2ToBGRA32(decBuf, outputPixels);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.BC7:
                    if (this.ActualScale != 1)
                        throw new Exception("BC7 does not support scale.");
                    bmp = new Bitmap(this.Width & ~3, this.Height & ~3, PixelFormat.Format32bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe {
                        Span<byte> outputPixels = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.BC7ToRGBA32(decBuf, this.Width * 4, outputPixels, bmpData.Width, bmpData.Stride, bmpData.Height);
                        ImageCodec.RGBA32ToBGRA32(outputPixels, outputPixels);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                case Wz_TextureFormat.R16:
                    bmp = new Bitmap(this.Width, this.Height, PixelFormat.Format64bppArgb);
                    bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    unsafe {
                        Span<byte> output = new Span<byte>(bmpData.Scan0.ToPointer(), bmpData.Stride * bmpData.Height);
                        ImageCodec.R16ToBGRA64(decBuf, output);
                    }
                    bmp.UnlockBits(bmpData);
                    break;
                default:
                    Console.WriteLine($"Unsupported format ({this.Format}, scale={this.ActualScale}).");
                    break;
            }
            mPNG = bmp;
        }

        private static void CopyBmpDataWithStride(byte[] source, int stride, BitmapData bmpData) {
            if (bmpData.Stride == stride) {
                Marshal.Copy(source, 0, bmpData.Scan0, source.Length);
            } else {
                for (int y = 0; y < bmpData.Height; y++) {
                    Marshal.Copy(source, stride * y, bmpData.Scan0 + bmpData.Stride * y, stride);
                }
            }
        }

        public static int GetUncompressedDataSize(Wz_TextureFormat format, int scale, int width, int height) {
            if (scale > 1) {
                if ((width % scale) != 0 || (height % scale) != 0) {
                    throw new ArgumentException("Width or height cannot be divided by scale");
                }
                width /= scale;
                height /= scale;
            }
            return GetUncompressedDataSize(format, width, height);
        }

        public static int GetUncompressedDataSize(Wz_TextureFormat format, int width, int height) {
            switch (format) {
                case Wz_TextureFormat.ARGB4444:
                case Wz_TextureFormat.ARGB1555:
                case Wz_TextureFormat.RGB565:
                case Wz_TextureFormat.R16:
                    return width * height * 2;
                case Wz_TextureFormat.ARGB8888:
                case Wz_TextureFormat.RGBA1010102:
                    return width * height * 4;
                case Wz_TextureFormat.DXT3:
                case Wz_TextureFormat.DXT5:
                    return ((width + 3) / 4) * ((height + 3) / 4) * 16;
                // TMST v1272, width and height for BC7 format are not always multiples of 4, NX will add row padding and discard the tail rows.
                case Wz_TextureFormat.BC7:
                    return width * (height & ~3);
                case Wz_TextureFormat.DXT1:
                    return ((width + 3) / 4) * ((height + 3) / 4) * 8;
                case Wz_TextureFormat.A8:
                    return width * height;
                case Wz_TextureFormat.RGBA32Float:
                    return width * height * 16;
                default:
                    throw new ArgumentException($"Unknown texture format {(int)format}.");
            }
        }

        #endregion

        public enum Wz_TextureFormat
        {
            Unknown = 0,
            ARGB4444 = 1,
            ARGB8888 = 2,
            ARGB1555 = 257,
            RGB565 = 513,
            /* introduced in KMST 1197 */
            R16 = 769,
            DXT3 = 1026,
            DXT5 = 2050,
            /* introduced in KMST 1186 */
            A8 = 2304,
            RGBA1010102 = 2562,
            DXT1 = 4097,
            BC7 = 4098,
            RGBA32Float = 4100
        }

        internal void CompressPng(Bitmap pBmp) {
            byte[] buf = new byte[pBmp.Width * pBmp.Height * 8];
            mFormat = 2;
            mScale = 0;
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

    internal static class StreamExtensions {

        public static int ReadAvailableBytes(Stream stream, Span<byte> buffer) {
            int totalRead = 0;
            byte[] tempBuffer = buffer.Length > 0 ? new byte[buffer.Length] : Array.Empty<byte>();
            while (totalRead < buffer.Length) {
                int read = stream.Read(tempBuffer, totalRead, buffer.Length - totalRead);
                if (read == 0) {
                    break;
                }
                totalRead += read;
            }
            if (buffer.Length > 0) {
                tempBuffer.AsSpan(0, buffer.Length).CopyTo(buffer);
            }
            return totalRead;
        }
    }
}