using System;
using System.IO;
using System.Security.Cryptography;
using MapleLib.MapleCryptoLib;

namespace MapleLib.WzLib.Util {
	public class WzKeyGenerator {
		/// <summary>
		/// Generates the wz key used in the encryption from ZLZ.dll
		/// </summary>
		/// <param name="pPathToZlz">Path to ZLZ.dll</param>
		/// <returns>The wz key</returns>
		public static byte[] GenerateKeyFromZlz(string pPathToZlz) {
			FileStream zlzStream = File.OpenRead(pPathToZlz);
			byte[] wzKey = GenerateWzKey(GetIvFromZlz(zlzStream), GetAesKeyFromZlz(zlzStream));
			zlzStream.Close();
			return wzKey;
		}

		public static byte[] GetIvFromZlz(FileStream pZLZStream) {
			byte[] iv = new byte[4];

			pZLZStream.Seek(0x10040, SeekOrigin.Begin);
			pZLZStream.Read(iv, 0, 4);
			return iv;
		}

		private static byte[] GetAesKeyFromZlz(FileStream pZLZStream) {
			byte[] aes = new byte[32];

			pZLZStream.Seek(0x10060, SeekOrigin.Begin);
			for (int i = 0; i < 8; i++) {
				pZLZStream.Read(aes, i * 4, 4);
				pZLZStream.Seek(12, SeekOrigin.Current);
			}
			return aes;
		}

		public static byte[] GenerateWzKey(byte[] pWzIv) {
			return GenerateWzKey(pWzIv, CryptoConstants.TrimmedUserKey);
		}

		public static byte[] GenerateWzKey(byte[] pWzIv, byte[] pAesKey) {
			if (BitConverter.ToInt32(pWzIv, 0) == 0) {
				return new byte[ushort.MaxValue];
			}
			AesManaged crypto = new AesManaged { KeySize = 256, Key = pAesKey, Mode = CipherMode.ECB };

			MemoryStream memStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memStream, crypto.CreateEncryptor(), CryptoStreamMode.Write);

			byte[] input = MapleCrypto.multiplyBytes(pWzIv, 4, 4);
			byte[] wzKey = new byte[ushort.MaxValue];
			for (int i = 0; i < (wzKey.Length / 16); i++) {
				cryptoStream.Write(input, 0, 16);
				input = memStream.ToArray();
				Array.Copy(memStream.ToArray(), 0, wzKey, (i * 16), 16);
				memStream.Position = 0;
			}

			try {
				cryptoStream.Dispose();
				memStream.Dispose();
			} catch (Exception e) {
				Console.WriteLine("Error disposing AES streams" + e);
			}

			return wzKey;
		}
	}
}