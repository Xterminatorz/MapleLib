using System;
using System.IO;
using System.Security.Cryptography;

namespace MapleLib.MapleCryptoLib {
    /// <summary>
    /// Class to handle the AES Encryption routines
    /// </summary>
    public class AESEncryption {
        /// <summary>
        /// Encrypt data using MapleStory's AES algorithm
        /// </summary>
        /// <param name="pIV">IV to use for encryption</param>
        /// <param name="pData">Data to encrypt</param>
        /// <param name="pLength">Length of data</param>
        /// <returns>Crypted data</returns>
        public static byte[] aesCrypt(byte[] pIV, byte[] pData, int pLength) {
            return aesCrypt(pIV, pData, pLength, CryptoConstants.TrimmedUserKey);
        }

        /// <summary>
        /// Encrypt data using MapleStory's AES method
        /// </summary>
        /// <param name="pIV">IV to use for encryption</param>
        /// <param name="pData">data to encrypt</param>
        /// <param name="pLength">length of data</param>
        /// <param name="pKey">the AES key to use</param>
        /// <returns>Crypted data</returns>
        public static byte[] aesCrypt(byte[] pIV, byte[] pData, int pLength, byte[] pKey) {
            AesManaged crypto = new AesManaged { KeySize = 256, Key = pKey, Mode = CipherMode.ECB };

            MemoryStream memStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memStream, crypto.CreateEncryptor(), CryptoStreamMode.Write);

            int remaining = pLength;
            int llength = 0x5B0;
            int start = 0;
            while (remaining > 0) {
                byte[] myIV = MapleCrypto.multiplyBytes(pIV, 4, 4);
                if (remaining < llength) {
                    llength = remaining;
                }
                for (int x = start; x < (start + llength); x++) {
                    if ((x - start) % myIV.Length == 0) {
                        cryptoStream.Write(myIV, 0, myIV.Length);
                        byte[] newIV = memStream.ToArray();
                        Array.Copy(newIV, myIV, myIV.Length);
                        memStream.Position = 0;
                    }
                    pData[x] ^= myIV[(x - start) % myIV.Length];
                }
                start += llength;
                remaining -= llength;
                llength = 0x5B4;
            }

            try {
                cryptoStream.Dispose();
                memStream.Dispose();
            } catch (Exception e) {
                Console.WriteLine("Error disposing AES streams" + e);
            }

            return pData;
        }
    }
}