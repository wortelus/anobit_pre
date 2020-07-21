using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    public class ReceiveTransaction : Transaction {
        public const byte ReceiveTransactionType = 12;
        public const byte ReceiveTransactionDiffiulty = 16;
        public override byte Difficulty => ReceiveTransactionDiffiulty;

        public const int ReceiveTransactionMinSize = 144 + 1;
        public const int ReceiveTransactionMaxSize = 144 + MaxSignatureSize;

        private byte[] target;
        public byte[] Target {
            get {
                return target;
            }
            set {
                if (value.Length != 32) {
                    throw new AnoBITCryptoException("Target for receive transaction is not 32 bytes.");
                }
                target = value;
            }
        }
        public ulong Amount { get; set; }

        public ReceiveTransaction(byte[] privateKey, ushort rap, byte[] previousHash, byte[] target, ulong amount) {
            TxType = ReceiveTransactionType;
            RAP = rap;
            PreviousHash = previousHash;
            SenderPublicKey = AnoBITCrypto.ToPublicKey(privateKey);
            Target = target;
            Amount = amount;

            SignTransaction(privateKey);
        }

        public ReceiveTransaction(byte[] transaction) {
            TxType = GetTransactionType(transaction);

            if (TxType == ReceiveTransactionType && (transaction.Length < ReceiveTransactionMinSize || transaction.Length > ReceiveTransactionMaxSize)) {
                throw new Exception("Receive transaction is not within the accepted bounds.");
            }

            if (TxType == ReceiveTransactionType) {
                RAP = GetTransactionRAP(transaction);
                Nonce = GetTransactionNonce(transaction);
                PreviousHash = GetTransactionPublicKey(transaction);
                SenderPublicKey = GetTransactionPublicKey(transaction);
                Target = transaction.Skip(104).Take(32).ToArray();
                Amount = BitConverter.ToUInt64(transaction.Skip(136).Take(8).ToArray(), 0);
                Signature = transaction.Skip(144).Take(transaction.Length - 144).ToArray();
            }
            throw new Exception("Receive transaction is not valid.");
        }

        public void SignTransaction(byte[] privateKey) {
            byte[] unsigned = this.ToByteArrayUnsigned();
            Signature = AnoBITCrypto.GetSignature(unsigned, privateKey);
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
            return Target;
        }

        public new static byte[] GetTarget(byte[] transaction) {
            if (transaction.Length >= 145) {
                return transaction.Skip(104).Take(32).ToArray();
            }
            return null;
        }
    }

    internal static partial class ExtensionsTx {
        public static ReceiveTransaction GetByHash(this IEnumerable<ReceiveTransaction> transactions, byte[] hash) {
            foreach (ReceiveTransaction receiveTransaction in transactions) {
                if (receiveTransaction.GetHash().SequenceEqual(hash)) {
                    return receiveTransaction;
                }
            }
            return null;
        }

        public static ReceiveTransaction GetByPreviousHash(this IEnumerable<ReceiveTransaction> transactions, byte[] hash) {
            foreach (ReceiveTransaction receiveTransaction in transactions) {
                if (receiveTransaction.PreviousHash.SequenceEqual(hash)) {
                    return receiveTransaction;
                }
            }
            return null;
        }

        public static byte[] ToByteArray(this ReceiveTransaction receiveTransaction) {
            return BitConverter.GetBytes(receiveTransaction.Nonce)
                .Concat(new byte[] { receiveTransaction.TxType } )
                .Concat(BitConverter.GetBytes(receiveTransaction.RAP))
                .Concat(receiveTransaction.PreviousHash)
                .Concat(receiveTransaction.SenderPublicKey)
                .Concat(receiveTransaction.Target)
                .Concat(BitConverter.GetBytes(receiveTransaction.Amount))
                .Concat(receiveTransaction.Signature)
                .ToArray();
        }

        public static byte[] ToByteArrayUnsigned(this ReceiveTransaction receiveTransaction) {
            return BitConverter.GetBytes(receiveTransaction.Nonce)
                .Concat(new byte[] { receiveTransaction.TxType })
                .Concat(BitConverter.GetBytes(receiveTransaction.RAP))
                .Concat(receiveTransaction.PreviousHash)
                .Concat(receiveTransaction.SenderPublicKey)
                .Concat(receiveTransaction.Target)
                .Concat(BitConverter.GetBytes(receiveTransaction.Amount))
                .ToArray();
        }

        public static byte[] GetHash(this ReceiveTransaction receiveTransaction) {
            byte[] unsigned = receiveTransaction.ToByteArray();
            return new SHA256Managed().ComputeHash(unsigned);
        }

        public static bool VerifyWithSendTransaction(this ReceiveTransaction receiveTransaction, SendTransaction sendTransaction) {
            //verify amount
            if (receiveTransaction.Amount + GenesisBlock.TransactionFee != sendTransaction.Amount) {
                return false;
            }

            //verify receive target and hash
            if (!receiveTransaction.Target.SequenceEqual(sendTransaction.GetHash())) {
                return false;
            }

            //verify send target
            if (!receiveTransaction.SenderPublicKey.SequenceEqual(sendTransaction.Receiver)) {
                return false;
            }

            //verify is send transaction is already spent
            if (sendTransaction.SpentBy != null /*&& sendTransaction.SpentBy.SequenceEqual(receiveTransaction.GetHash()) == false*/) {
                return false;
            }

            return true;
        }
    }
}
