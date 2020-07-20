using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    internal class ChangeTransaction : Transaction {
        public const byte ChangeTransactionType = 2;

        public const byte ChangeTransactionDifficulty = 16;
        public override byte Difficulty => ChangeTransactionDifficulty;

        public const int ChangeTransactionMinSize = 124 + 1;
        public const int ChangeTransactionMaxSize = 124 + MaxSignatureSize;

        private byte[] representative;
        public byte[] Representative {
            get {
                return representative;
            }
            set {
                if (value.Length != 20) {
                    throw new AnoBITCryptoException("Target for change transaction is not 20 bytes.");
                }
                representative = value;
            }
        }

        public ChangeTransaction(byte[] privateKey, byte[] previousHash, byte[] representative) {
            TxType = ChangeTransactionType;
            PreviousHash = previousHash;
            SenderPublicKey = AnoBITCrypto.ToPublicKey(privateKey);
            RAP = GenesisBlock.RAP;
        }

        public ChangeTransaction(byte[] transaction) {
            TxType = GetTransactionType(transaction);

            if (TxType == ChangeTransactionType && (transaction.Length < ChangeTransactionMinSize || transaction.Length > ChangeTransactionMaxSize)) {
                throw new Exception("Root transaction is not within the accepted bounds.");
            }

            if (TxType == ChangeTransactionType) {
                RAP = GetTransactionRAP(transaction);
                Nonce = GetTransactionNonce(transaction);
                PreviousHash = GetTransactionPublicKey(transaction);
                SenderPublicKey = GetTransactionPublicKey(transaction);
                Representative = transaction.Skip(104).Take(20).ToArray();
                Signature = transaction.Skip(124).Take(transaction.Length - 124).ToArray();
            }
            throw new Exception("Change transaction is not valid.");
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
        public static ChangeTransaction GetByHash(this IEnumerable<ChangeTransaction> transactions, byte[] hash) {
            foreach (ChangeTransaction changeTransaction in transactions) {
                if (changeTransaction.GetHash().SequenceEqual(hash)) {
                    return changeTransaction;
                }
            }
            return null;
        }

        public static ChangeTransaction GetByPreviousHash(this IEnumerable<ChangeTransaction> transactions, byte[] hash) {
            foreach (ChangeTransaction changeTransaction in transactions) {
                if (changeTransaction.PreviousHash.SequenceEqual(hash)) {
                    return changeTransaction;
                }
            }
            return null;
        }

        public static byte[] ToByteArray(this ChangeTransaction changeTransaction) {
            return BitConverter.GetBytes(changeTransaction.Nonce)
                    .Concat(new byte[] { changeTransaction.TxType })
                    .Concat(BitConverter.GetBytes(changeTransaction.RAP))
                    .Concat(changeTransaction.PreviousHash)
                    .Concat(changeTransaction.SenderPublicKey)
                    .Concat(changeTransaction.Representative)
                    .Concat(changeTransaction.Signature)
                    .ToArray();
        }

        public static byte[] ToByteArrayUnsigned(this ChangeTransaction changeTransaction) {
            return BitConverter.GetBytes(changeTransaction.Nonce)
                    .Concat(new byte[] { changeTransaction.TxType })
                    .Concat(BitConverter.GetBytes(changeTransaction.RAP))
                    .Concat(changeTransaction.PreviousHash)
                    .Concat(changeTransaction.SenderPublicKey)
                    .Concat(changeTransaction.Representative)
                    .ToArray();
        }

        public static byte[] GetHash(this ChangeTransaction changeTransaction) {
            byte[] unsigned = changeTransaction.ToByteArray();
            return new SHA256Managed().ComputeHash(unsigned);
        }
    }
}
