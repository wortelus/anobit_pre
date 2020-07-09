using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    public static class Extensions {
        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="buffer">An array of bytes to convert</param>
        /// <returns>The byte as a hex string.</returns>
        public static string ToHexString(this byte[] buffer) {
            string output = string.Empty;
            foreach (byte b in buffer) {
                output += b.ToString("X2");
            }
            return output;
        }


        /// <summary>
        /// Return a padded array with leading zeroes on the left.
        /// </summary>
        /// <param name="input">Byte array input</param>
        /// <param name="n">Total size of the output array in bytes</param>
        /// <returns></returns>
        public static byte[] ZeroPad(byte[] input, int n) {
            if (input.Length > n) {
                throw new Exception("The byte array input cannot be padded with zeroes, n is smaller than the input.");
            }
            var output = new byte[n];
            var startAt = output.Length - input.Length;
            Buffer.BlockCopy(input, 0, output, startAt, input.Length);
            return output;
        }

        /// <summary>
        /// Converts a byte array to Base58.
        /// </summary>
        /// <param name="buffer">An array of bytes to convert</param>
        /// <returns>The byte as a Base58 string.</returns>
        public static string ToBase58(this byte[] buffer) {
            Org.BouncyCastle.Math.BigInteger addrremain = new Org.BouncyCastle.Math.BigInteger(1, buffer);

            Org.BouncyCastle.Math.BigInteger big0 = new Org.BouncyCastle.Math.BigInteger("0");
            Org.BouncyCastle.Math.BigInteger big58 = new Org.BouncyCastle.Math.BigInteger("58");

            string rv = "";

            while (addrremain.CompareTo(big0) > 0) {
                int d = Convert.ToInt32(addrremain.Mod(big58).ToString());
                addrremain = addrremain.Divide(big58);
                rv = AnoBITCrypto.Base58Charset.Substring(d, 1) + rv;
            }

            // handle leading zeroes
            foreach (byte b in buffer) {
                if (b != 0) break;
                rv = "1" + rv;

            }
            return rv;
        }

        /// <summary>
        /// Converts byte array to string, and also checks its integrity.
        /// </summary>
        /// <param name="buffer">Byte array</param>
        /// <returns>Base58 string</returns>
        public static string ToBase58Check(this byte[] array) {
            byte[] bb = new byte[array.Length + 4];
            Array.Copy(array, bb, array.Length);
            SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            byte[] thehash = sha256.ComputeHash(array);
            thehash = sha256.ComputeHash(thehash);
            for (int i = 0; i < 4; i++) bb[array.Length + i] = thehash[i];
            return bb.ToBase58();
        }

        /// <summary>
        /// Converts Base58 string to a byte array
        /// </summary>
        /// <param name="base58">Base58 String</param>
        /// <returns>Byte array</returns>
        public static byte[] Base58ToByteArray(this string base58) {
            Org.BouncyCastle.Math.BigInteger bi2 = new Org.BouncyCastle.Math.BigInteger("0");

            bool IgnoreChecksum = false;

            foreach (char c in base58) {
                if (AnoBITCrypto.Base58Charset.IndexOf(c) != -1) {
                    bi2 = bi2.Multiply(new Org.BouncyCastle.Math.BigInteger("58"));
                    bi2 = bi2.Add(new Org.BouncyCastle.Math.BigInteger(AnoBITCrypto.Base58Charset.IndexOf(c).ToString()));
                } else if (c == '?') {
                    IgnoreChecksum = true;
                } else {
                    return null;
                }
            }

            byte[] bb = bi2.ToByteArrayUnsigned();

            // interpret leading '1's as leading zero bytes
            foreach (char c in base58) {
                if (c != '1') break;
                byte[] bbb = new byte[bb.Length + 1];
                Array.Copy(bb, 0, bbb, 1, bb.Length);
                bb = bbb;
            }

            if (bb.Length < 4) return null;

            if (IgnoreChecksum == false) {
                SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
                byte[] checksum = sha256.ComputeHash(bb, 0, bb.Length - 4);
                checksum = sha256.ComputeHash(checksum);
                for (int i = 0; i < 4; i++) {
                    if (checksum[i] != bb[bb.Length - 4 + i]) return null;
                }
            }

            byte[] rv = new byte[bb.Length - 4];
            Array.Copy(bb, 0, rv, 0, bb.Length - 4);
            return rv;
        }

        public static byte[] DecodeBase58(string input, byte cointype) {
            var output = new byte[AnoBITCrypto.BinaryAddressSize];
            foreach (var t in input) {
                var p = AnoBITCrypto.Base58Charset.IndexOf(t);
                if (p == -1) {
                    throw new AnoBITCryptoException("Invalid character was found during Base58 decoding.");
                }

                var j = AnoBITCrypto.BinaryAddressSize;
                while (--j >= 0) {
                    p += 58 * output[j];
                    output[j] = (byte)(p % 256);
                    p /= 256;
                }
                if (p != 0 && t != input[input.Length - 1]) {
                    throw new AnoBITCryptoException("Address is too long");
                }
            }
            if (output[0] == cointype) {
                return output;
            } else {
                throw new AnoBITCryptoException("Address does not correspond with chain's prefix. Your current chain uses as a prefix byte " + cointype + " (" + cointype.ToString("X2") + ").");
            }
        }

        public static T[] SubArray<T>(this T[] data, int index, int length) {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }


        private static string ByteArrayToString(byte[] ba, int offset, int count) {
            string rv = "";
            int usedcount = 0;
            for (int i = offset; usedcount < count; i++, usedcount++) {
                rv += String.Format("{0:X2}", ba[i]) + " ";
            }
            return rv;
        }

        /// <summary>
        /// Converts hex string to byte array and adds leading zeroes.
        /// </summary>
        /// <param name="source">Hex string</param>
        /// <param name="leadzeroes">Number of leading zeroes that will be included at the start of the array</param>
        /// <returns></returns>
        public static byte[] GetHexBytes(string source, int leadzeroes) {
            byte[] hex = GetHexBytes(source);
            if (hex == null) return null;
            // assume leading zeroes if we're short a few bytes
            if (hex.Length > (leadzeroes - 6) && hex.Length < leadzeroes) {
                byte[] hex2 = new byte[leadzeroes];
                Array.Copy(hex, 0, hex2, leadzeroes - hex.Length, hex.Length);
                hex = hex2;
            }
            // clip off one overhanging leading zero if present
            if (hex.Length == leadzeroes + 1 && hex[0] == 0) {
                byte[] hex2 = new byte[leadzeroes];
                Array.Copy(hex, 1, hex2, 0, leadzeroes);
                hex = hex2;

            }

            return hex;
        }

        /// <summary>
        /// Converts hex string to byte array.
        /// </summary>
        /// <param name="source">Hex string</param>
        /// <returns>Byte array</returns>
        public static byte[] GetHexBytes(this string source) {
            List<byte> bytes = new List<byte>();
            // copy s into ss, adding spaces between each byte
            string s = source;
            string ss = "";
            int currentbytelength = 0;
            foreach (char c in s.ToCharArray()) {
                if (c == ' ') {
                    currentbytelength = 0;
                } else {
                    currentbytelength++;
                    if (currentbytelength == 3) {
                        currentbytelength = 1;
                        ss += ' ';
                    }
                }
                ss += c;
            }

            foreach (string b in ss.Split(' ')) {
                int v = 0;
                if (b.Trim() == "") continue;
                foreach (char c in b.ToCharArray()) {
                    if (c >= '0' && c <= '9') {
                        v *= 16;
                        v += (c - '0');

                    } else if (c >= 'a' && c <= 'f') {
                        v *= 16;
                        v += (c - 'a' + 10);
                    } else if (c >= 'A' && c <= 'F') {
                        v *= 16;
                        v += (c - 'A' + 10);
                    }

                }
                v &= 0xff;
                bytes.Add((byte)v);
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// Check if byte array is empty and filled with only '0' values.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsArrayEmpty(this byte[] input) {
            foreach (byte b in input) {
                if (b != 0) {
                    return false;
                }
            }
            return true;
        }
    }
}
