using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {

    public abstract class Transaction {
        public const int MaxSignatureSize = 73;

        public abstract byte Difficulty { get; }

        public byte TxType;
        public ushort RAP { get; set; }
        private byte[] previousHash;
        public byte[] PreviousHash {
            get {
                return previousHash;
            }
            set {
                if (value.Length != 32) {
                    throw new AnoBITCryptoException("Previous hash for transaction is not 32 bytes.");
                }
                previousHash = value;
            }
        }

        private byte[] senderPublicKey;
        public byte[] SenderPublicKey {
            get {
                return senderPublicKey;
            }
            set {
                if (value.Length != 65) {
                    throw new AnoBITCryptoException("Sender public key for transaction is not 65 bytes.");
                } else if (value[0] != 4) {
                    throw new AnoBITCryptoException("Sender public key for transaction doesn't start with '4' byte");
                }
                senderPublicKey = value;
            }
        }

        public int Nonce { get; set; }
        private byte[] signature;
        public byte[] Signature {
            get {
                return signature;
            }
            set {
                if (value[30] != 48) {
                    throw new AnoBITCryptoException("Signature for transaction doesn't start with 48.");
                }
                if (value.Length > MaxSignatureSize) {
                    throw new AnoBITCryptoException("Signature for transaction is too long.");
                }
                signature = value;
            }
        }

        public abstract bool HasValidNonce();
        public abstract byte[] GetTarget();

        public static bool HasValidNonce(byte difficulty, byte[] transaction) {
            string binaryOutput = string.Concat(new SHA256Managed().ComputeHash(transaction).Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

            if (binaryOutput.Substring(0, difficulty).Replace("0", "") == string.Empty) {
                return true;
            } else {
                return false;
            }
        }

        public static byte[] TxHashFunction(byte[] transaction) {
            return new SHA256Managed().ComputeHash(transaction);
        }

        public static byte GetDifficulty(byte type) {
            switch (type) {
                case SendTransaction.SendTransactionType:
                    return SendTransaction.SendTransactionDifficulty;
                case SendTransaction.SendTransactionTypeMessage:
                    return SendTransaction.SendTransactionMessageDifficulty;
                case ReceiveTransaction.ReceiveTransactionType:
                    return ReceiveTransaction.ReceiveTransactionDiffiulty;
                case RootTransaction.RootTransactionType:
                    return RootTransaction.RootTransactionDifficulty;
                case ChangeTransaction.ChangeTransactionType:
                    return ChangeTransaction.ChangeTransactionDifficulty;
                default:
                    return 255;
            }
        }

        public static byte[] GetTarget(byte[] transaction) {
            switch (GetTransactionType(transaction)) {
                case SendTransaction.SendTransactionType:
                    return SendTransaction.GetTarget(transaction);
                case SendTransaction.SendTransactionTypeMessage:
                    return SendTransaction.GetTarget(transaction);
                case ReceiveTransaction.ReceiveTransactionType:
                    return ReceiveTransaction.GetTarget(transaction);
                case RootTransaction.RootTransactionType:
                    return RootTransaction.GetTarget(transaction);
                case ChangeTransaction.ChangeTransactionType:
                    return ChangeTransaction.GetTarget(transaction);
                default:
                    return null;
            }
        }

        public static int GetMinSize(byte[] transaction) {
            switch (GetTransactionType(transaction)) {
                case SendTransaction.SendTransactionType:
                    return SendTransaction.SendTransactionMinSize;
                case SendTransaction.SendTransactionTypeMessage:
                    return SendTransaction.SendTransactionMessageMinSize;
                case ReceiveTransaction.ReceiveTransactionType:
                    return ReceiveTransaction.ReceiveTransactionMinSize;
                case RootTransaction.RootTransactionType:
                    return RootTransaction.RootTransactionMinSize;
                case ChangeTransaction.ChangeTransactionType:
                    return ChangeTransaction.ChangeTransactionMinSize;
                default:
                    return 0;
            }
        }

        public static int GetMaxSize(byte type) {
            switch (type) {
                case SendTransaction.SendTransactionType:
                    return SendTransaction.SendTransactionMaxSize;
                case SendTransaction.SendTransactionTypeMessage:
                    return SendTransaction.SendTransactionMessageMaxSize;
                case ReceiveTransaction.ReceiveTransactionType:
                    return ReceiveTransaction.ReceiveTransactionMaxSize;
                case RootTransaction.RootTransactionType:
                    return RootTransaction.RootTransactionMaxSize;
                case ChangeTransaction.ChangeTransactionType:
                    return ChangeTransaction.ChangeTransactionMaxSize;
                default:
                    return 0;
            }
        }

        public static int GetMaxSize(byte[] transaction) {
            switch (GetTransactionType(transaction)) {
                case SendTransaction.SendTransactionType:
                    return SendTransaction.SendTransactionMaxSize;
                case SendTransaction.SendTransactionTypeMessage:
                    return SendTransaction.SendTransactionMessageMaxSize;
                case ReceiveTransaction.ReceiveTransactionType:
                    return ReceiveTransaction.ReceiveTransactionMaxSize;
                case RootTransaction.RootTransactionType:
                    return RootTransaction.RootTransactionMaxSize;
                case ChangeTransaction.ChangeTransactionType:
                    return ChangeTransaction.ChangeTransactionMaxSize;
                default:
                    return 0;
            }
        }

        public static byte GetTransactionType(byte[] transaction) {
            if (transaction.Length > 5) {
                return transaction[4];
            }
            return 255;
        }

        public static int GetTransactionNonce(byte[] transaction) {
            if (transaction.Length > 4) {
                return Convert.ToInt32(transaction.Take(4).ToArray());
            }
            return 0;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int GetTransactionNonceUnchecked(byte[] transaction) {
            return Convert.ToInt32(transaction.Take(4).ToArray());
        }

        public static ushort GetTransactionRAP(byte[] transaction) {
            if (transaction.Length > 7) {
                return Convert.ToUInt16(BitConverter.ToInt16(transaction.Skip(5).Take(2).ToArray(), 0));
            }
            return 0;
        }

        public static byte[] GetTransactionPreviousHash(byte[] transaction) {
            if (transaction.Length > 35) {
                return transaction.Skip(7).Take(32).ToArray();
            }
            return null;
        }

        public static byte[] GetTransactionPublicKey(byte[] transaction) {
            if (transaction.Length > 104) {
                if (transaction[39] == 4) {
                    return transaction.Skip(39).Take(65).ToArray();
                }
            }
            return null;
        }

        public static List<byte[]> GetHashList(List<byte[]> transactions, bool sort, bool fromSecureSource) {
            if (sort) {
                transactions = SortTransactions(transactions, fromSecureSource);
            }
            List<byte[]> output = new List<byte[]>();
                for (int i = 0; i < transactions.Count; i++) {
                    output.Add(TxHashFunction(output[i]));
                }
            
            return output;
        }

        public static List<byte[]> GetPreviousHashList(List<byte[]> transactions, bool sort, bool fromSecureSource) {
            if (sort) {
                transactions = SortTransactions(transactions, fromSecureSource);
            }
            List<byte[]> output = new List<byte[]>();
            for (int i = 0; i < transactions.Count; i++) {
                output.Add(GetTransactionPreviousHash(output[i]));
            }

            return output;
        }

        public static Account TransactionsToAccount(List<byte[]> transactions, bool fromSecureSource) {
            List<byte[]> sortedTxs = SortTransactions(transactions, fromSecureSource);
            Account account = new Account(sortedTxs);
            return account;
        }

        public static List<byte[]> SortTransactions(List<byte[]> transactions, bool fromSecureSource) {
            byte[] hashGenesisBlock = GenesisBlock.GetHash();
            byte[] lastHash = hashGenesisBlock;
            List<byte[]> output = new List<byte[]>();

            RootTransaction rootTransaction = null;
            for (int i = 0; i < transactions.Count; i++) {
                byte[] tx = transactions[i];
                if (GetTransactionType(tx) == RootTransaction.RootTransactionType) {
                    byte[] pHash = GetTransactionPreviousHash(tx);
                    if (pHash.SequenceEqual(lastHash)) {
                        output.Add(tx);
                        transactions.RemoveAt(i);
                        rootTransaction = new RootTransaction(tx);
                        lastHash = rootTransaction.GetHash();
                    }
                }
            }

            if (rootTransaction == null) {
                string txOwnerAddress = "unknown";
                try {
                    txOwnerAddress = AnoBITCrypto.RIPEMD160ToAddress(AnoBITCrypto.PublicKeyToRIPEMD160(GetTransactionPublicKey(transactions[0])));
                } catch {
                    if (fromSecureSource) {
                        throw new Exception(string.Format("TransactionsToAccount: Invalid owner of transaction, this absolutely shouldn't happen, especially as it is an input from database, specified as {0}.", GetTransactionPublicKey(transactions[0])));
                    }
                    throw new Exception(string.Format("TransactionsToAccount: Invalid owner of transaction, specified as {0}.", GetTransactionPublicKey(transactions[0])));
                }
                throw new Exception(string.Format("Open transaction for {0} couldn't be found.", txOwnerAddress));
            }

            int iterations = transactions.Count;

            for (int i = 0; i < transactions.Count; i++, iterations--) {
                byte[] tx = transactions[i];
                byte[] pHash = GetTransactionPreviousHash(tx);
                if (pHash.SequenceEqual(lastHash)) {
                    output.Add(tx);
                    transactions.RemoveAt(i);
                }
                if (iterations < 0) {
                    throw new Exception("All transactions couldn't be sorted, malfuctioned blockchain.");
                }
            }

            /*
            if (transactions.Count != 0) {
                string txOwnerAddress = "unknown";
                try {
                    txOwnerAddress = AnoBITCrypto.RIPEMD160ToAddress(AnoBITCrypto.PublicKeyToRIPEMD160(GetTransactionPublicKey(transactions[0])));
                } catch {
                    if (fromSecureSource) {
                        throw new Exception(string.Format("SortTransactions: The blockchain for this account is malfuctioned, and this is a big problem as it comes out of secure source (database), specified as {0}.", GetTransactionPublicKey(transactions[0])));
                    }
                    throw new Exception(string.Format("SortTransactions: The blockchain for this account is malfuctioned, specified as {0}.", GetTransactionPublicKey(transactions[0])));
                }
                throw new Exception(string.Format("SortTransactions: transactions for {0} are corrupted.", txOwnerAddress));
            }*/

            return output;
        }

        public static int CalculateNonce(byte difficulty, byte[] transaction, byte[] privateKey) {
            bool foundTheResult = false;
            int calculatedNonce = 0;

            Thread t0 = new Thread(() => {
                SHA256Managed SHA256 = new SHA256Managed();
                int nonce = 0;

                while (foundTheResult == false) {
                    byte[] iteration;
                    iteration = AnoBITCrypto.GetSignature(transaction, privateKey);
                    byte[] hashIteration = SHA256.ComputeHash(iteration);

                    string binaryOutput = string.Concat(hashIteration.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    if (binaryOutput.Substring(0, difficulty).Replace("0", "") == string.Empty) {
                        calculatedNonce = nonce;
                        foundTheResult = true;
                        break;
                    } else {
                        nonce++;
                    }
                }
            });
            t0.Start();

            Thread t1 = new Thread(() => {
                SHA256Managed SHA256 = new SHA256Managed();
                int nonce = int.MaxValue;

                while (foundTheResult == false) {
                    byte[] iteration;
                    iteration = AnoBITCrypto.GetSignature(transaction, privateKey);
                    byte[] hashIteration = SHA256.ComputeHash(iteration);

                    string binaryOutput = string.Concat(hashIteration.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    if (binaryOutput.Substring(0, difficulty).Replace("0", "") == string.Empty) {
                        calculatedNonce = nonce;
                        foundTheResult = true;
                        break;
                    } else {
                        nonce--;
                    }
                }
            });
            t1.Start();

            Thread t2 = new Thread(() => {
                SHA256Managed SHA256 = new SHA256Managed();
                int nonce = int.MaxValue/2;

                while (foundTheResult == false) {
                    byte[] iteration;
                    iteration = AnoBITCrypto.GetSignature(transaction, privateKey);
                    byte[] hashIteration = SHA256.ComputeHash(iteration);

                    string binaryOutput = string.Concat(hashIteration.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    if (binaryOutput.Substring(0, difficulty).Replace("0", "") == string.Empty) {
                        calculatedNonce = nonce;
                        foundTheResult = true;
                        break;
                    } else {
                        nonce++;
                    }
                }
            });
            t2.Start();

            Thread t3 = new Thread(() => {
                SHA256Managed SHA256 = new SHA256Managed();
                int nonce = int.MaxValue / 2;

                while (foundTheResult == false) {
                    byte[] iteration;
                    iteration = AnoBITCrypto.GetSignature(transaction, privateKey);
                    byte[] hashIteration = SHA256.ComputeHash(iteration);

                    string binaryOutput = string.Concat(hashIteration.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                    if (binaryOutput.Substring(0, difficulty).Replace("0", "") == string.Empty) {
                        calculatedNonce = nonce;
                        foundTheResult = true;
                        break;
                    } else {
                        nonce--;
                    }
                }
            });
            t3.Start();

            while (true) {
                if (foundTheResult) {
                    return calculatedNonce;
                }
            }
        }
    }
}