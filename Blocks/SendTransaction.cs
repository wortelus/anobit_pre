using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    internal class SendTransaction : Transaction {
        public const byte SendTransactionType = 10;
        public const byte SendTransactionTypeMessage = 11;

        public const byte SendTransactionDifficulty = 18;
        public const byte SendTransactionMessageDifficulty = 20;

        public const int SendTransactionMinSize = 132 + 1;
        public const int SendTransactionMessageMinSize = 164 + 1;

        public const int SendTransactionMaxSize = 132 + MaxSignatureSize;
        public const int SendTransactionMessageMaxSize = 164 + MaxSignatureSize;

        public override byte Difficulty {
            get {
                if (TxType == SendTransactionType) {
                    return SendTransactionDifficulty;
                } else {
                    return SendTransactionMessageDifficulty;
                }
            }
        }

        public byte[] Receiver { get; set; }
        public ulong Amount { get; set; }
        public byte[] Message { get; set; }
        public byte[] SpentBy { get; set; }

        public SendTransaction(byte[] privateKey, byte[] previousHash, byte[] receiver, ulong amount) {
            TxType = SendTransactionType;
            RAP = GenesisBlock.RAP;
            PreviousHash = previousHash;
            SenderPublicKey = AnoBITCrypto.ToPublicKey(privateKey);
            Receiver = receiver;
            Amount = amount;
        }

        public SendTransaction(byte[] privateKey, byte[] previousHash, byte[] receiver, ulong amount, byte[] message) {
            TxType = SendTransactionTypeMessage;
            RAP = GenesisBlock.RAP;
            PreviousHash = previousHash;
            SenderPublicKey = AnoBITCrypto.ToPublicKey(privateKey);
            Receiver = receiver;
            Message = message;
            Amount = amount;
        }

        public SendTransaction(byte[] transaction) {
            TxType = GetTransactionType(transaction); //1 byte

            if (TxType == SendTransactionType && (transaction.Length < SendTransactionMinSize || transaction.Length > SendTransactionMaxSize)) {
                throw new Exception("Send transaction is not within the accepted bounds.");
            } else if (TxType == SendTransactionTypeMessage && (transaction.Length < SendTransactionMessageMinSize || transaction.Length > SendTransactionMessageMaxSize)) {
                throw new Exception("Send transaction with message is not within the accepted bounds.");
            }

            if (TxType == SendTransactionTypeMessage) {
                RAP = GetTransactionRAP(transaction); //2 bytes
                Nonce = GetTransactionNonce(transaction); //4 bytes
                PreviousHash = GetTransactionPublicKey(transaction); //32 bytes
                SenderPublicKey = GetTransactionPublicKey(transaction); //65 bytes
                Receiver = transaction.Skip(104).Take(20).ToArray(); //20 bytes
                Amount = BitConverter.ToUInt64(transaction.Skip(124).Take(8).ToArray(), 0); //8 bytes
                Message = transaction.Skip(132).Take(32).ToArray(); //32 bytes
                Signature = transaction.Skip(164).Take(transaction.Length - 164).ToArray(); 
            } else if (TxType == SendTransactionType) {
                RAP = GetTransactionRAP(transaction); //2 bytes
                Nonce = GetTransactionNonce(transaction); //4 bytes
                PreviousHash = GetTransactionPublicKey(transaction); //32 bytes
                SenderPublicKey = GetTransactionPublicKey(transaction); //65 bytes
                Receiver = transaction.Skip(104).Take(20).ToArray(); //20 bytes
                Amount = BitConverter.ToUInt64(transaction.Skip(124).Take(8).ToArray(), 0); //8 bytes
                Signature = transaction.Skip(132).Take(transaction.Length - 132).ToArray(); 
            }
            throw new Exception("Send transaction is not valid.");
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
            return Receiver;
        }

        public new static byte[] GetTarget(byte[] transaction) {
            if (transaction.Length >= 133) {
                return transaction.Skip(104).Take(20).ToArray();
            }
            return null;
        }

        public bool VerifyWithReceiveTransaction(ReceiveTransaction receiveTransaction) {
            //verify amount
            if (receiveTransaction.Amount + GenesisBlock.TransactionFee != Amount) {
                return false;
            }

            //verify receive target and hash
            if (!receiveTransaction.Target.SequenceEqual(this.GetHash())) {
                return false;
            }

            //verify send target
            if (!receiveTransaction.SenderPublicKey.SequenceEqual(Receiver)) {
                return false;
            }

            //verify is send transaction is already spent
            if (SpentBy != null /*&& sendTransaction.SpentBy.SequenceEqual(receiveTransaction.GetHash()) == false*/) {
                return false;
            }

            return true;
        }
    }

    internal static partial class ExtensionsTx {
        public static SendTransaction GetByHash(this IEnumerable<SendTransaction> transactions, byte[] hash) {
            foreach (SendTransaction sendTransaction in transactions) {
                if (sendTransaction.GetHash().SequenceEqual(hash)) {
                    return sendTransaction;
                }
            }
            return null;
        }

        public static SendTransaction GetByPreviousHash(this IEnumerable<SendTransaction> transactions, byte[] hash) {
            foreach (SendTransaction sendTransaction in transactions) {
                if (sendTransaction.PreviousHash.SequenceEqual(hash)) {
                    return sendTransaction;
                }
            }
            return null;
        }

        public static byte[] ToByteArray(this SendTransaction sendTransaction) {
            if (sendTransaction.TxType == SendTransaction.SendTransactionType) {
                return BitConverter.GetBytes(sendTransaction.Nonce)
                    .Concat(new byte[] { sendTransaction.TxType })
                    .Concat(BitConverter.GetBytes(sendTransaction.RAP))
                    .Concat(sendTransaction.PreviousHash)
                    .Concat(sendTransaction.SenderPublicKey)
                    .Concat(sendTransaction.Receiver)
                    .Concat(BitConverter.GetBytes(sendTransaction.Amount))
                    .Concat(sendTransaction.Signature)
                    .ToArray();
            } else {
                return BitConverter.GetBytes(sendTransaction.Nonce)
                    .Concat(new byte[] { sendTransaction.TxType })
                    .Concat(BitConverter.GetBytes(sendTransaction.RAP))
                    .Concat(sendTransaction.PreviousHash)
                    .Concat(sendTransaction.SenderPublicKey)
                    .Concat(sendTransaction.Receiver)
                    .Concat(BitConverter.GetBytes(sendTransaction.Amount))
                    .Concat(sendTransaction.Message)
                    .Concat(sendTransaction.Signature)
                    .ToArray();
            }
        }

        public static byte[] ToByteArrayUnsigned(this SendTransaction sendTransaction) {
            if (sendTransaction.TxType == SendTransaction.SendTransactionType) {
                return BitConverter.GetBytes(sendTransaction.Nonce)
                    .Concat(new byte[] { sendTransaction.TxType })
                    .Concat(BitConverter.GetBytes(sendTransaction.RAP))
                    .Concat(sendTransaction.PreviousHash)
                    .Concat(sendTransaction.SenderPublicKey)
                    .Concat(sendTransaction.Receiver)
                    .Concat(BitConverter.GetBytes(sendTransaction.Amount))
                    .ToArray();
            } else {
                return BitConverter.GetBytes(sendTransaction.Nonce)
                    .Concat(new byte[] { sendTransaction.TxType })
                    .Concat(BitConverter.GetBytes(sendTransaction.RAP))
                    .Concat(sendTransaction.PreviousHash)
                    .Concat(sendTransaction.SenderPublicKey)
                    .Concat(sendTransaction.Receiver)
                    .Concat(BitConverter.GetBytes(sendTransaction.Amount))
                    .Concat(sendTransaction.Message)
                    .ToArray();
            }
        }

        public static byte[] GetHash(this SendTransaction sendTransaction) {
            byte[] unsigned = sendTransaction.ToByteArray();
            return Transaction.TxHashFunction(unsigned);
        }
    }
}
