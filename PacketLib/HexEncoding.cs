using System;
using System.Globalization;

namespace MapleLib.PacketLib {
    /// <summary>
    /// Class to handle Hex Encoding and Hex Conversions
    /// </summary>
    public class HexEncoding {
        /// <summary>
        /// Checks if a character is a hex digit
        /// </summary>
        /// <param name="pChar">Char to check</param>
        /// <returns>Char is a hex digit</returns>
        public static bool IsHexDigit(Char pChar) {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            pChar = Char.ToUpper(pChar);
            numChar = Convert.ToInt32(pChar);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }

        /// <summary>
        /// Convert a hex string to a byte
        /// </summary>
        /// <param name="pHex">Byte as a hex string</param>
        /// <returns>Byte representation of the string</returns>
        private static byte HexToByte(string pHex) {
            if (pHex.Length > 2 || pHex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(pHex, NumberStyles.HexNumber);
            return newByte;
        }

        /// <summary>
        /// Convert a hex string to a byte array
        /// </summary>
        /// <param name="hex">byte array as a hex string</param>
        /// <returns>Byte array representation of the string</returns>
        public static byte[] GetBytes(string pHexString) {
            string newString = string.Empty;
            char c;
            // remove all none A-F, 0-9, characters
            for (int i = 0; i < pHexString.Length; i++) {
                c = pHexString[i];
                if (IsHexDigit(c))
                    newString += c;
            }
            // if odd number of characters, discard last character
            if (newString.Length % 2 != 0) {
                newString = newString.Substring(0, newString.Length - 1);
            }

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++) {
                hex = new String(new[] { newString[j], newString[j + 1] });
                bytes[i] = HexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        /// <summary>
        /// Convert byte array to ASCII
        /// </summary>
        /// <param name="pBytes">Bytes to convert to ASCII</param>
        /// <returns>The byte array as an ASCII string</returns>
        public static String ToStringFromAscii(byte[] pBytes) {
            char[] ret = new char[pBytes.Length];
            for (int x = 0; x < pBytes.Length; x++) {
                if (pBytes[x] < 32 && pBytes[x] >= 0) {
                    ret[x] = '.';
                } else {
                    int chr = (pBytes[x]) & 0xFF;
                    ret[x] = (char)chr;
                }
            }
            return new String(ret);
        }
    }
}