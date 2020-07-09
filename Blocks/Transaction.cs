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