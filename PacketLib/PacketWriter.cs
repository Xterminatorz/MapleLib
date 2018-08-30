using System;
using System.IO;
using System.Text;

namespace MapleLib.PacketLib {
    /// <summary>
    /// Class to handle writing packets
    /// </summary>
    public class PacketWriter : AbstractPacket {
        /// <summary>
        /// The main writer tool
        /// </summary>
        private readonly BinaryWriter pBinWriter;

        /// <summary>
        /// Amount of data writen in the writer
        /// </summary>
        public short Length { get { return (short)mBuffer.Length; } }

        /// <summary>
        /// Creates a new instance of PacketWriter
        /// </summary>
        /// <param name="pSize">Starting size of the buffer</param>
        public PacketWriter(int pSize = 0) {
            mBuffer = new MemoryStream(pSize);
            pBinWriter = new BinaryWriter(mBuffer, Encoding.ASCII);
        }

        public PacketWriter(byte[] pData) {
            mBuffer = new MemoryStream(pData);
            pBinWriter = new BinaryWriter(mBuffer, Encoding.ASCII);
        }

        /// <summary>
        /// Restart writing from the point specified. This will overwrite data in the packet.
        /// </summary>
        /// <param name="pLength">The point of the packet to start writing from.</param>
        public void Reset(int pLength) {
            mBuffer.Seek(pLength, SeekOrigin.Begin);
        }

        /// <summary>
        /// Writes a byte to the stream
        /// </summary>
        /// <param name="@byte">The byte to write</param>
        public void WriteByte(int pByte) {
            pBinWriter.Write((byte)pByte);
        }

        /// <summary>
        /// Writes a byte array to the stream
        /// </summary>
        /// <param name="@bytes">The byte array to write</param>
        public void WriteBytes(byte[] pBytes) {
            pBinWriter.Write(pBytes);
        }

        /// <summary>
        /// Writes a boolean to the stream
        /// </summary>
        /// <param name="@bool">The boolean to write</param>
        public void WriteBool(bool pBool) {
            pBinWriter.Write(pBool);
        }

        /// <summary>
        /// Writes a short to the stream
        /// </summary>
        /// <param name="@short">The short to write</param>
        public void WriteShort(int pShort) {
            pBinWriter.Write((short)pShort);
        }

        /// <summary>
        /// Writes an int to the stream
        /// </summary>
        /// <param name="@int">The int to write</param>
        public void WriteInt(int pInt) {
            pBinWriter.Write(pInt);
        }

        /// <summary>
        /// Writes a long to the stream
        /// </summary>
        /// <param name="@long">The long to write</param>
        public void WriteLong(long pLong) {
            pBinWriter.Write(pLong);
        }

        /// <summary>
        /// Writes a string to the stream
        /// </summary>
        /// <param name="@string">The string to write</param>
        public void WriteString(String pString) {
            pBinWriter.Write(pString.ToCharArray());
        }

        /// <summary>
        /// Writes a string prefixed with a [short] length before it, to the stream
        /// </summary>
        /// <param name="@string">The string to write</param>
        public void WriteMapleString(String pString) {
            WriteShort((short)pString.Length);
            WriteString(pString);
        }

        /// <summary>
        /// Writes a hex-string to the stream
        /// </summary>
        /// <param name="@string">The hex-string to write</param>
        public void WriteHexString(String pHexString) {
            WriteBytes(HexEncoding.GetBytes(pHexString));
        }

        /// <summary>
        /// Sets a byte in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@byte">The byte to set</param>
        public void SetByte(long pIndex, int pByte) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteByte((byte)pByte);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a byte array in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@bytes">The bytes to set</param>
        public void SetBytes(long pIndex, byte[] pBytes) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteBytes(pBytes);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a bool in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@bool">The bool to set</param>
        public void SetBool(long pIndex, bool pBool) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteBool(pBool);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a short in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@short">The short to set</param>
        public void SetShort(long pIndex, int pShort) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteShort((short)pShort);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets an int in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@int">The int to set</param>
        public void SetInt(long pIndex, int pInt) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteInt(pInt);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a long in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@long">The long to set</param>
        public void SetLong(long pIndex, long pLong) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteLong(pLong);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a long in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@string">The long to set</param>
        public void SetString(long pIndex, string pString) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteString(pString);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a string prefixed with a [short] length before it, in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@string">The string to set</param>
        public void SetMapleString(long pIndex, string pString) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteMapleString(pString);
            mBuffer.Position = oldIndex;
        }

        /// <summary>
        /// Sets a hex-string in the stream
        /// </summary>
        /// <param name="index">The index of the stream to set data at</param>
        /// <param name="@string">The hex-string to set</param>
        public void SetHexString(long pIndex, string pString) {
            long oldIndex = mBuffer.Position;
            mBuffer.Position = pIndex;
            WriteHexString(pString);
            mBuffer.Position = oldIndex;
        }
    }
}