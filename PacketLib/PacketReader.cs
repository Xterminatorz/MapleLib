using System.IO;
using System.Text;

namespace MapleLib.PacketLib {
    /// <summary>
    /// Class to handle reading data from a packet
    /// </summary>
    public class PacketReader : AbstractPacket {
        /// <summary>
        /// The main reader tool
        /// </summary>
        private readonly BinaryReader mBinReader;

        /// <summary>
        /// Amount of data left in the reader
        /// </summary>
        public short Length { get { return (short)mBuffer.Length; } }

        /// <summary>
        /// Creates a new instance of PacketReader
        /// </summary>
        /// <param name="pArrayOfBytes">Starting byte array</param>
        public PacketReader(byte[] pArrayOfBytes) {
            mBuffer = new MemoryStream(pArrayOfBytes, false);
            mBinReader = new BinaryReader(mBuffer, Encoding.ASCII);
        }

        /// <summary>
        /// Restart reading from the point specified.
        /// </summary>
        /// <param name="pLength">The point of the packet to start reading from.</param>
        public void Reset(int pLength) {
            mBuffer.Seek(pLength, SeekOrigin.Begin);
        }

        public void Skip(int pLength) {
            mBuffer.Position += pLength;
        }

        /// <summary>
        /// Reads an unsigned byte from the stream
        /// </summary>
        /// <returns> an unsigned byte from the stream</returns>
        public byte ReadByte() {
            return mBinReader.ReadByte();
        }

        /// <summary>
        /// Reads a byte array from the stream
        /// </summary>
        /// <param name="length">Amount of bytes</param>
        /// <returns>A byte array</returns>
        public byte[] ReadBytes(int pCount) {
            return mBinReader.ReadBytes(pCount);
        }

        /// <summary>
        /// Reads a bool from the stream
        /// </summary>
        /// <returns>A bool</returns>
        public bool ReadBool() {
            return mBinReader.ReadBoolean();
        }

        /// <summary>
        /// Reads a signed short from the stream
        /// </summary>
        /// <returns>A signed short</returns>
        public short ReadShort() {
            return mBinReader.ReadInt16();
        }

        /// <summary>
        /// Reads a signed int from the stream
        /// </summary>
        /// <returns>A signed int</returns>
        public int ReadInt() {
            return mBinReader.ReadInt32();
        }

        /// <summary>
        /// Reads a signed long from the stream
        /// </summary>
        /// <returns>A signed long</returns>
        public long ReadLong() {
            return mBinReader.ReadInt64();
        }

        /// <summary>
        /// Reads an ASCII string from the stream
        /// </summary>
        /// <param name="pLength">Amount of bytes</param>
        /// <returns>An ASCII string</returns>
        public string ReadString(int pLength) {
            return Encoding.ASCII.GetString(ReadBytes(pLength));
        }

        /// <summary>
        /// Reads a maple string from the stream
        /// </summary>
        /// <returns>A maple string</returns>
        public string ReadMapleString() {
            return ReadString(ReadShort());
        }
    }
}