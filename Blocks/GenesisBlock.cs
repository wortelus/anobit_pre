using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    internal class GenesisBlock {
        //1 ANO = 100000000 raw
        public const ulong MintingStart = 5000000000;
        public const int MintingCycleNum = 61;
        public const double MintingCycleMultiplier = 0.9;
        public const ulong MintingMinimalReward = 100000000;
        public const string MintingAddress = "ANoBitvrviU6VrWXPBCitwrLM8d3jXNBLR";
        public const ushort RAP = 2207;
        public const ulong TransactionFee = 0;
        /*
        private byte[] mintingPublicKey;
        public byte[] MintingPublicKey {
            get {
                return mintingPublicKey;
            }
            set {
                if (value.Length != 65) {
                    throw new AnoBITCryptoException("Genesis block public key is not 65 bytes.");
                } else if (value[0] != 4) {
                    throw new AnoBITCryptoException("Genesis block public key doesn't start with '4' byte");
                }
                mintingPublicKey = value;
            }
        }*/

        public static byte[] ToByteArray() {
            return BitConverter.GetBytes(MintingStart)
                .Concat(BitConverter.GetBytes(MintingCycleNum))
                .Concat(BitConverter.GetBytes(MintingCycleMultiplier))
                .Concat(BitConverter.GetBytes(MintingMinimalReward))
                .Concat(AnoBITCrypto.AddressToRIPEMD160(MintingAddress))
                .Concat(BitConverter.GetBytes(RAP))
                .Concat(BitConverter.GetBytes(TransactionFee))
                .ToArray();
        }

        public static byte[] GetHash() {
            return new SHA256Managed().ComputeHash(ToByteArray());
        }
    }

    internal static partial class ExtensionsTx {
        public static byte[] ToByteArray(this GenesisBlock genesisBlock) {
            return BitConverter.GetBytes(GenesisBlock.MintingStart)
                .Concat(BitConverter.GetBytes(GenesisBlock.MintingCycleNum))
                .Concat(BitConverter.GetBytes(GenesisBlock.MintingCycleMultiplier))
                .Concat(BitConverter.GetBytes(GenesisBlock.MintingMinimalReward))
                .Concat(AnoBITCrypto.AddressToRIPEMD160(GenesisBlock.MintingAddress))
                .Concat(BitConverter.GetBytes(GenesisBlock.RAP))
                .Concat(BitConverter.GetBytes(GenesisBlock.TransactionFee))
                .ToArray();
        }

        public static byte[] GetHash(this GenesisBlock genesisBlock) {
            return new SHA256Managed().ComputeHash(GenesisBlock.ToByteArray());
        }
    }
}
