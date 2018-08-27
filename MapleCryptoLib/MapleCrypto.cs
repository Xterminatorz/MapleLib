using System;

namespace MapleLib.MapleCryptoLib {
	/// <summary>
	/// Class to manage Encryption and IV generation
	/// </summary>
	public class MapleCrypto {
		#region Properties

		/// <summary>
		/// (private) IV used in the packet encryption
		/// </summary>
		private byte[] mIV;

		/// <summary>
		/// Version of MapleStory used in encryption
		/// </summary>
		private readonly short mMapleVersion;

		/// <summary>
		/// (public) IV used in the packet encryption
		/// </summary>
		public byte[] IV { get { return mIV; } set { mIV = value; } }

		#endregion

		#region Methods

		/// <summary>
		/// Creates a new MapleCrypto class
		/// </summary>
		/// <param name="pIV">Intializing Vector</param>
		/// <param name="pMapleVersion">Version of MapleStory</param>
		public MapleCrypto(byte[] pIV, short pMapleVersion) {
			mIV = pIV;
			mMapleVersion = pMapleVersion;
		}

		/// <summary>
		/// Updates the current IV
		/// </summary>
		public void updateIV() {
			mIV = getNewIV(mIV);
		}

		/// <summary>
		/// Encrypts data with AES and updates the IV
		/// </summary>
		/// <param name="pData">The data to crypt</param>
		public void crypt(byte[] pData) {
			AESEncryption.aesCrypt(mIV, pData, pData.Length);
			updateIV();
		}

		/// <summary>
		/// Generates a new IV
		/// </summary>
		/// <param name="pOldIV">The Old IV used to generate the new IV</param>
		/// <returns>A new IV</returns>
		public static byte[] getNewIV(byte[] pOldIV) {
			//byte[] start = CryptoConstants.bDefaultAESKeyValue;
			byte[] start = new byte[] { 0xf2, 0x53, 0x50, 0xc6 }; //TODO: ADD GLOBAL VAR BACK
			for (int i = 0; i < 4; i++) {
				shuffle(pOldIV[i], start);
			}
			return start;
		}

		/// <summary>
		/// Shuffle the bytes in the IV
		/// </summary>
		/// <param name="pInputByte">Byte of the old IV</param>
		/// <param name="pStart">The Default AES Key</param>
		/// <returns>The shuffled bytes</returns>
		public static byte[] shuffle(byte pInputByte, byte[] pStart) {
			byte a = pStart[1];
			byte b = a;
			uint c, d;
			b = CryptoConstants.bShuffle[b];
			b -= pInputByte;
			pStart[0] += b;
			b = pStart[2];
			b ^= CryptoConstants.bShuffle[pInputByte];
			a -= b;
			pStart[1] = a;
			a = pStart[3];
			b = a;
			a -= pStart[0];
			b = CryptoConstants.bShuffle[b];
			b += pInputByte;
			b ^= pStart[2];
			pStart[2] = b;
			a += CryptoConstants.bShuffle[pInputByte];
			pStart[3] = a;

			c = (uint) (pStart[0] + pStart[1] * 0x100 + pStart[2] * 0x10000 + pStart[3] * 0x1000000);
			d = c;
			c >>= 0x1D;
			d <<= 0x03;
			c |= d;
			pStart[0] = (byte) (c % 0x100);
			c /= 0x100;
			pStart[1] = (byte) (c % 0x100);
			c /= 0x100;
			pStart[2] = (byte) (c % 0x100);
			pStart[3] = (byte) (c / 0x100);

			return pStart;
		}

		/// <summary>
		/// Get a packet header for a packet being sent to the server
		/// </summary>
		/// <param name="pSize">Size of the packet</param>
		/// <returns>The packet header</returns>
		public byte[] getHeaderToClient(int pSize) {
			byte[] header = new byte[4];
			int a = mIV[3] * 0x100 + mIV[2];
			a ^= -(mMapleVersion + 1);
			int b = a ^ pSize;
			header[0] = (byte) (a % 0x100);
			header[1] = (byte) ((a - header[0]) / 0x100);
			header[2] = (byte) (b ^ 0x100);
			header[3] = (byte) ((b - header[2]) / 0x100);
			return header;
		}

		/// <summary>
		/// Get a packet header for a packet being sent to the client
		/// </summary>
		/// <param name="pSize">Size of the packet</param>
		/// <returns>The packet header</returns>
		public byte[] getHeaderToServer(int pSize) {
			byte[] header = new byte[4];
			int a = IV[3] * 0x100 + IV[2];
			a = a ^ (mMapleVersion);
			int b = a ^ pSize;
			header[0] = Convert.ToByte(a % 0x100);
			header[1] = Convert.ToByte(a / 0x100);
			header[2] = Convert.ToByte(b % 0x100);
			header[3] = Convert.ToByte(b / 0x100);
			return header;
		}

		/// <summary>
		/// Gets the length of a packet from the header
		/// </summary>
		/// <param name="pPacketHeader">Header of the packet</param>
		/// <returns>The length of the packet</returns>
		public static int getPacketLength(int pPacketHeader) {
			return getPacketLength(BitConverter.GetBytes(pPacketHeader));
		}

		/// <summary>
		/// Gets the length of a packet from the header
		/// </summary>
		/// <param name="pPacketHeader">Header of the packet</param>
		/// <returns>The length of the packet</returns>
		public static int getPacketLength(byte[] pPacketHeader) {
			if (pPacketHeader.Length < 4) {
				return -1;
			}
			return (pPacketHeader[0] + (pPacketHeader[1] << 8)) ^ (pPacketHeader[2] + (pPacketHeader[3] << 8));
		}

		/// <summary>
		/// Checks to make sure the packet is a valid MapleStory packet
		/// </summary>
		/// <param name="pPacket">The header of the packet received</param>
		/// <returns>The packet is valid</returns>
		public bool checkPacketToServer(byte[] pPacket) {
			int a = pPacket[0] ^ mIV[2];
			int b = mMapleVersion;
			int c = pPacket[1] ^ mIV[3];
			int d = mMapleVersion >> 8;
			return (a == b && c == d);
		}

		/// <summary>
		/// Multiplies bytes
		/// </summary>
		/// <param name="pInput">Bytes to multiply</param>
		/// <param name="pCount">Amount of bytes to repeat</param>
		/// <param name="pMult">Times to repeat the packet</param>
		/// <returns>The multiplied bytes</returns>
		public static byte[] multiplyBytes(byte[] pInput, int pCount, int pMult) {
			byte[] ret = new byte[pCount * pMult];
			for (int x = 0; x < ret.Length; x++) {
				ret[x] = pInput[x % pCount];
			}
			return ret;
		}

		#endregion
	}
}