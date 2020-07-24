﻿using AnoBIT_Wallet.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    public class Account {
        public byte[] RIPEMD160 { get; set; }
        public byte[] Representative { get; set; }

        RootTransaction RootTransaction;
        List<byte[]> Blocks = new List<byte[]>();

        private readonly object OpLock = new object();

        public Account(byte[] ripemd160) {
            RIPEMD160 = ripemd160;
        }

        public Account(RootTransaction rootTransaction) {
            RootTransaction = rootTransaction;
        }

        public Account(List<byte[]> transactions) {
            throw new NotImplementedException();
            /*RootTransaction = new RootTransaction(transactions[0]);
            for (int i = 1; i < transactions.Count; i++) {
                //InsertTransaction(transactions[i]);
            }*/
        }

        public byte[] GetPublicKey() {
            return RootTransaction.SenderPublicKey;
        }

        /*public bool MarkSpent(byte[] hash) {
            for (int i = 0; i < TxOutputs.Count; i++) {
                if (TxOutputs[i].GetHash().SequenceEqual(hash)) {
                    if (TxOutputs[i].SpentBy != null) {
                        TxOutputs[i].SpentBy = hash;
                        return true;
                    } else {
                        return false;
                    }
                }
            }
            return false;
        }*/

        public bool InsertTransaction(byte[] transaction, byte[] targetTransaction) {
            byte type = Transaction.GetTransactionType(transaction);

            try {
                bool precheck = TransactionPrecheck(transaction);
                bool maincheck = false;

                if (precheck == false) {
                    return false;
                }

                if (type == SendTransaction.SendTransactionType || type == SendTransaction.SendTransactionTypeMessage) {
                    try {
                        SendTransaction sendTransaction = new SendTransaction(transaction);
                        maincheck = SendTransactionPrecheck(sendTransaction);

                    } catch (OverflowException) {
                        throw new OverflowException(type + " balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                    }
                } else if (type == ReceiveTransaction.ReceiveTransactionType) {
                    try {
                        ReceiveTransaction receiveTransaction = new ReceiveTransaction(transaction);
                        SendTransaction sendTargetTransaction = new SendTransaction(targetTransaction);
                        maincheck = ReceiveTransactionPrecheck(receiveTransaction, sendTargetTransaction);
                    } catch (OverflowException) {
                        throw new OverflowException(type + " balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                    }
                } else if (type == ChangeTransaction.ChangeTransactionType) {
                    ChangeTransaction changeTransaction = new ChangeTransaction(transaction);
                    Representative = changeTransaction.Representative;
                }
                return maincheck && precheck;
            } catch (Exception ex) {
                throw new Exception("an error occured during adding send transaction to blockchain: " + ex.Message);
            }
        }

        public List<byte[]> GetHashList() {
            List<byte[]> output = new List<byte[]>();
            if (RootTransaction != null) {
                output.Add(RootTransaction.GetHash());
                for (int i = 0; i < Blocks.Count; i++) {
                    output.Add(Transaction.TxHashFunction(output[i]));
                }
            } 
            return output;
        }

        public bool SendTransactionPrecheck(SendTransaction sendTransaction) {
            lock (OpLock) {
                TransactionPrecheck(sendTransaction.ToByteArray());
                ulong balance = GetBalance();
                if (sendTransaction.Amount > balance) {
                    return false;
                }
            }
            return true;
        }

        public bool ReceiveTransactionPrecheck(ReceiveTransaction receiveTransaction, SendTransaction targetTransaction) {
            lock (OpLock) {
                byte[] hash = receiveTransaction.GetHash();
                bool sendCheck = targetTransaction.VerifyWithReceiveTransaction(receiveTransaction);
                if (sendCheck) {
                    return true;
                } else {
                    return false;
                }
            }
        }

        public bool TransactionPrecheck(byte[] transaction) {
            lock (OpLock) {
                byte type = Transaction.GetTransactionType(transaction);
                if (RootTransaction == null && type != RootTransaction.RootTransactionType) {
                    return false;
                }
                if (Transaction.GetTransactionRAP(transaction) != GenesisBlock.RAP) {
                    return false;
                }
                if (!Transaction.GetTransactionPreviousHash(transaction).SequenceEqual(GetLastPreviousHash())) {
                    return false;
                }
                byte[] publicKey = Transaction.GetTransactionPublicKey(transaction);
                if (publicKey != null && !AnoBITCrypto.PublicKeyToRIPEMD160(publicKey).SequenceEqual(RIPEMD160)) {
                    return false;
                }
                if (!Transaction.HasValidNonce(
                    Transaction.GetDifficulty(Transaction.GetTransactionType(transaction)),
                    transaction)) {
                    return false;
                }

                return true;
            }
        }

        public byte[] GetLastPreviousHash() {
            lock (OpLock) {
                if (Blocks.Count == 0) {
                    return GenesisBlock.GetHash();
                }

                return Transaction.GetTransactionPreviousHash(Blocks[Blocks.Count - 1]);
            }
        }

        public ulong GetBalance() {
            ulong balance = 0;

            for (int i = 0; i < Blocks.Count; i++) {
                byte[] tx = Blocks[i];
                byte type = Transaction.GetTransactionType(tx);

                if (type == SendTransaction.SendTransactionType || type == SendTransaction.SendTransactionTypeMessage) {
                    SendTransaction sendTransaction = new SendTransaction(tx);
                    try {
                        balance = checked(balance - sendTransaction.Amount);
                    } catch (OverflowException) {
                        throw new OverflowException(type + "balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                    }
                } else if (type == ReceiveTransaction.ReceiveTransactionType) {
                    ReceiveTransaction sendTransaction = new ReceiveTransaction(tx);
                    try {
                        balance = checked(balance + sendTransaction.Amount);
                    } catch (OverflowException) {
                        throw new OverflowException(type + "balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                    }
                }
            }

            return balance;
        }
    }
}
