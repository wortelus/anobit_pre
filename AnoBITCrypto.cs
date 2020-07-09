using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    public class AnoBITCrypto {
        public const string Base58Charset = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        public const int BinaryAddressSize = 25;
        public const int Cointype = 23;
        public static readonly byte[] ZeroHash = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// WIF to Hex private key conversion.
        /// </summary>
        /// <param name="WIF">WIF Private Key</param>
        /// <returns>Hexadecimal representation of the private key</returns>
        public static byte[] GetHexPrivateKey(string WIF) {
            byte[] hex = WIF.Base58ToByteArray();
            if (hex == null) {
                int L = WIF.Length;
                if (L >= 50 && L <= 52) {
                    throw new AnoBITCryptoException("Private key is not valid. One character is typed wrong.");
                } else {
                    throw new AnoBITCryptoException("WIF private key is not valid.");
                }
            }
            if (hex.Length != 33) {
                throw new AnoBITCryptoException("WIF private key is not valid (wrong byte count, should be 33, was " + hex.Length + ")");
            }

            return hex.Skip(1).Take(32).ToArray();
        }

        /// <summary>
        /// Hex to WIF private key conversion.
        /// </summary>
        /// <param name="WIF">Hexadecimal representation of the private key</param>
        /// <returns>Private key in the WIF format</returns>
        public static string GetWIFPrivateKey(string hexString) {
            byte[] hex = ValidateAndGetPrivateKey(hexString, 0x80);
            if (hex == null) {
                return null;
            }
            return hex.ToBase58Check();
        }

        /// <summary>
        /// Validate Hexadecimal private key.
        /// </summary>
        /// <param name="hexPrivateKey"></param>
        /// <param name="leadingbyte"></param>
        /// <returns></returns>
        public static byte[] ValidateAndGetPrivateKey(string hexPrivateKey, byte leadingbyte) {
            byte[] hex = Extensions.GetHexBytes(hexPrivateKey, 32);

            if (hex == null || hex.Length < 32 || hex.Length > 33) {
                throw new AnoBITCryptoException("Hex is not 32 or 33 bytes.");
            }

            // if leading 00, change it to 0x80
            if (hex.Length == 33) {
                if (hex[0] == 0 || hex[0] == 0x80) {
                    hex[0] = 0x80;
                } else {
                    throw new AnoBITCryptoException("Not a valid private key");
                }
            }

            // add 0x80 byte if not present
            if (hex.Length == 32) {
                byte[] hex2 = new byte[33];
                Array.Copy(hex, 0, hex2, 1, 32);
                hex2[0] = 0x80;
                hex = hex2;
            }

            hex[0] = leadingbyte;
            return hex;
        }

        /// <summary>
        /// Validate hexadecimal public key
        /// </summary>
        /// <param name="hexpubkey"></param>
        /// <returns></returns>
        public static byte[] ValidateAndGetPublicKey(string hexpubkey) {
            byte[] hex = Extensions.GetHexBytes(hexpubkey, 64);

            if (hex == null || hex.Length < 64 || hex.Length > 65) {
                throw new AnoBITCryptoException("Hex is not 64 or 65 bytes.");
            }

            // if leading 00, change it to 0x80
            if (hex.Length == 65) {
                if (hex[0] == 0 || hex[0] == 4) {
                    hex[0] = 4;
                } else {
                    throw new AnoBITCryptoException("Not a valid public key");
                }
            }

            // add 0x80 byte if not present
            if (hex.Length == 64) {
                byte[] hex2 = new byte[65];
                Array.Copy(hex, 0, hex2, 1, 64);
                hex2[0] = 4;
                hex = hex2;
            }
            return hex;
        }

        public static byte[] ValidateAndGetPublicHash(byte[] ripeMD) {
            if (ripeMD == null || ripeMD.Length != 20) {
                return null;
            }
            return ripeMD;
        }

        public static bool ValidateAddress(string address, byte cointype) {
            try {
                if (address.Length < 26 || address.Length > 35) {
                    return false;
                }

                SHA256Managed SHA256 = new SHA256Managed();

                var decoded = Extensions.DecodeBase58(address, cointype);
                if (decoded == null) return false;
                var d1 = SHA256.ComputeHash(decoded.SubArray(0, 21));
                var d2 = SHA256.ComputeHash(d1);
                if (!decoded.SubArray(21, 4).SequenceEqual(d2.SubArray(0, 4))) {
                    return false;
                } else {
                    return true;
                }
            } catch {
                return false;
            }
        }

        public static byte[] ToPublicKey(string hexPrivateKey) {
            byte[] hex = ValidateAndGetPrivateKey(hexPrivateKey, 0x00);
            if (hex == null) {
                return null;
            }
            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            Org.BouncyCastle.Math.BigInteger Db = new Org.BouncyCastle.Math.BigInteger(hex);
            ECPoint dd = ps.G.Multiply(Db);

            byte[] pubaddr = new byte[65];
            byte[] Y = dd.Normalize().YCoord.ToBigInteger().ToByteArray();
            Array.Copy(Y, 0, pubaddr, 64 - Y.Length + 1, Y.Length);
            byte[] X = dd.Normalize().XCoord.ToBigInteger().ToByteArray();
            Array.Copy(X, 0, pubaddr, 32 - X.Length + 1, X.Length);
            pubaddr[0] = 4;

            return pubaddr;
        }

        public static byte[] ToPublicKey(byte[] privateKey) {
            if (privateKey == null) {
                return null;
            }
            var ps = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            Org.BouncyCastle.Math.BigInteger Db = new Org.BouncyCastle.Math.BigInteger(privateKey);
            ECPoint dd = ps.G.Multiply(Db);

            byte[] pubaddr = new byte[65];
            byte[] Y = dd.Normalize().YCoord.ToBigInteger().ToByteArray();
            Array.Copy(Y, 0, pubaddr, 64 - Y.Length + 1, Y.Length);
            byte[] X = dd.Normalize().XCoord.ToBigInteger().ToByteArray();
            Array.Copy(X, 0, pubaddr, 32 - X.Length + 1, X.Length);
            pubaddr[0] = 4;

            return pubaddr;
        }

        public static byte[] PublicKeyToRIPEMD160(byte[] publicKey) {
            if (publicKey == null) {
                return null;
            }

            SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            byte[] shaofpubkey = sha256.ComputeHash(publicKey);

            RIPEMD160 rip = System.Security.Cryptography.RIPEMD160.Create();
            byte[] ripofpubkey = rip.ComputeHash(shaofpubkey);

            return ripofpubkey;
        }

        public static byte[] PublicKeyToRIPEMD160(string publicKey) {
            byte[] hex = ValidateAndGetPublicKey(publicKey);
            if (publicKey == null) {
                return null;
            }

            SHA256CryptoServiceProvider sha256 = new SHA256CryptoServiceProvider();
            byte[] shaofpubkey = sha256.ComputeHash(hex);

            RIPEMD160 rip = System.Security.Cryptography.RIPEMD160.Create();
            byte[] ripofpubkey = rip.ComputeHash(shaofpubkey);

            return ripofpubkey;
        }


        public static byte[] AddressToRIPEMD160(string address) {
            byte[] hex = address.Base58ToByteArray();
            if (hex == null || hex.Length != 21) {
                int L = address.Length;
                if (L >= 33 && L <= 34) {
                    throw new AnoBITCryptoException("Address is not valid. There is a high chance that you mistyped one character.");
                } else {
                    throw new AnoBITCryptoException("Address is not valid.");
                }
            }
            return hex.Skip(1).ToArray();
        }

        public static string RIPEMD160ToAddress(byte[] ripeMDAddress) {
            byte[] hex = ValidateAndGetPublicHash(ripeMDAddress);
            if (hex == null) {
                return null;
            }

            byte[] hex2 = new byte[21];
            Array.Copy(hex, 0, hex2, 1, 20);

            hex2[0] = (byte)(Cointype & 0xff);
            return hex2.ToBase58Check();
        }

        public static byte[] GetSignature(byte[] data, byte[] privateKey) {
            var curve = SecNamedCurves.GetByName("secp256k1");
            var domain = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            Org.BouncyCastle.Math.BigInteger privateKeyBigInt = new Org.BouncyCastle.Math.BigInteger(privateKey.ToHexString(), 16);
            var keyParameters = new ECPrivateKeyParameters(privateKeyBigInt, domain);
            ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, keyParameters);
            signer.BlockUpdate(data, 0, data.Length);
            byte[] signature = signer.GenerateSignature();
            return signature;
        }
    }

    [Serializable]
    public class AnoBITCryptoException : Exception {
        public AnoBITCryptoException(string message) : base(message) { }

        public AnoBITCryptoException(string message, Exception innerException) : base(message, innerException) { }

        protected AnoBITCryptoException(SerializationInfo info, StreamingContext ctxt) : base(info, ctxt) { }
    }
}
