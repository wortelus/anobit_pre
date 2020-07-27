using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    public class RootTransaction : Transaction {
        public const byte RootTransactionType = 1;
        public const byte RootTransactionDifficulty = 20;
        public override byte Difficulty => RootTransactionDifficulty;

        public const int RootTransactionBaseSize = 124;
        public const int RootTransactionMinSize = 124 + 1;
        public const int RootTransactionMaxSize = 124 + MaxSignatureSize;

        private byte[] representative;
        public byte[] Representative {
            get {
                return representative;
            }
            set {
                if (value.Length != 20) {
                    throw new AnoBITCryptoException("Target for root transaction is not 20 bytes.");
                }
                representative = value;
            }
        }

        public RootTransaction(byte[] privateKey, byte[] representative) {
            TxType = RootTransactionType;
            PreviousHash = GenesisBlock.GetHash();
            SenderPublicKey = AnoBITCrypto.ToPublicKey(privateKey);
            Representative = representative;
            RAP = GenesisBlock.RAP;
        }

        public RootTransaction (byte[] transaction) {
            TxType = GetTransactionType(transaction); //1 byte

            if (TxType == RootTransactionType && (transaction.Length < RootTransactionMinSize || transaction.Length > RootTransactionMaxSize)) {
                throw new Exception("Root transaction is not within the accepted bounds.");
            }

            if (TxType == RootTransactionType) {
                RAP = GetTransactionRAP(transaction); //2 bytes
                Nonce = GetTransactionNonce(transaction); //4 bytes
                PreviousHash = GetTransactionPublicKey(transaction); //32 bytes            
                SenderPublicKey = GetTransactionPublicKey(transaction); //65 bytes
                Representative = transaction.Skip(104).Take(20).ToArray(); //20 bytes
                Signature = transaction.Skip(124).Take(transaction.Length - 124).ToArray();
            }
            throw new Exception("Root transaction is not valid.");
        }

        public override bool HasValidNonce() {
            string binaryOutput = string.Concat(this.GetHash().Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

            if (binaryOutput.Substring(0, Difficulty).Replace("0", "") == string.Empty) {
                return true;
            } else {
                return false;
            }
        }

        public override byte[] GetTarget() {
            return Representative;
        }

        public new static byte[] GetTarget(byte[] transaction) {
            if (transaction.Length >= 125) {
                return transaction.Skip(104).Take(20).ToArray();
            }
            return null;
        }
    }

    internal static partial class ExtensionsTx {
        public static byte[] ToByteArray(this RootTransaction rootTransaction) {
            return BitConverter.GetBytes(rootTransaction.Nonce)
                    .Concat(new byte[] { rootTransaction.TxType })
                    .Concat(BitConverter.GetBytes(rootTransaction.RAP))
                    .Concat(rootTransaction.PreviousHash)
                    .Concat(rootTransaction.SenderPublicKey)
                    .Concat(rootTransaction.Representative)
                    .Concat(rootTransaction.Signature)
                    .ToArray();
        }

        public static byte[] ToByteArrayUnsigned(this RootTransaction rootTransaction) {
            return BitConverter.GetBytes(rootTransaction.Nonce)
                    .Concat(new byte[] { rootTransaction.TxType })
                    .Concat(BitConverter.GetBytes(rootTransaction.RAP))
                    .Concat(rootTransaction.PreviousHash)
                    .Concat(rootTransaction.SenderPublicKey)
                    .Concat(rootTransaction.Representative)
                    .ToArray();
        }

        public static byte[] GetHash(this RootTransaction rootTransaction) {
            byte[] unsigned = rootTransaction.ToByteArray();
            return new SHA256Managed().ComputeHash(unsigned);
        }
    }
}
