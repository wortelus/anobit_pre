using AnoBIT_Wallet.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    /// <summary>
    /// This is the object for BlockchainHandler class consisting of Accounts.
    /// Account gets loaded from database, and then the transactions are verified here in this object
    /// while parameters from the database and the network are passed from the BlockchainHandler class.
    /// If the InsertTransaction goes true/positive, BlockchainHandler should update the Database.
    /// BlockchainHandler should keep a track that the database and Account objects are consistent, although
    /// not all must be loaded from the database to save RAM.
    /// This class should also notify BlockchainHandler of a possible fork, which then passes the network request to vote
    /// which one is the right one.
    /// </summary>
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

        public TxCheckErrorCode InsertTransaction(byte[] transaction, byte[] targetTransaction) {
            lock (OpLock) {
                if (transaction == null || transaction.Length == 0) {
                    return TxCheckErrorCode.NullTx;
                }

                byte type = Transaction.GetTransactionType(transaction);

                try {
                    TxCheckErrorCode precheck = TransactionPrecheck(transaction);
                    TxCheckErrorCode maincheck = TxCheckErrorCode.Unknown;

                    if (precheck != TxCheckErrorCode.Success) {
                        //precheck failed, if the previous hashes are combating
                        //let network decide the fate of 2 combating transactions
                        return precheck;
                    }

                    if (type == SendTransaction.SendTransactionType || type == SendTransaction.SendTransactionTypeMessage) {
                        try {
                            SendTransaction sendTransaction = new SendTransaction(transaction);
                            maincheck = SendTransactionPrecheck(sendTransaction);

                        } catch (OverflowException) {
                            //throw new OverflowException(type + " balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                            return TxCheckErrorCode.BalanceOverflow;
                        }
                    } else if (type == ReceiveTransaction.ReceiveTransactionType) {
                        try {
                            if (targetTransaction == null || targetTransaction.Length == 0) {
                                maincheck = TxCheckErrorCode.NullTargetTx;
                            } else {
                                ReceiveTransaction receiveTransaction = new ReceiveTransaction(transaction);
                                SendTransaction sendTargetTransaction = new SendTransaction(targetTransaction);
                                maincheck = ReceiveTransactionPrecheck(receiveTransaction, sendTargetTransaction);
                            }
                        } catch (OverflowException) {
                            //throw new OverflowException(type + " balance overflow exceeded at " + AnoBITCrypto.RIPEMD160ToAddress(RIPEMD160));
                            return TxCheckErrorCode.BalanceOverflow;
                        }
                    } else if (type == ChangeTransaction.ChangeTransactionType) {
                        ChangeTransaction changeTransaction = new ChangeTransaction(transaction);
                        Representative = changeTransaction.Representative;
                    }
                    return Transaction.GetTxCheckErrorCodeFromArray(new TxCheckErrorCode[] { precheck, maincheck });
                } catch (Exception ex) {
                    throw new Exception("an error occured during adding send transaction to blockchain: " + ex.Message);
                }
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

        public TxCheckErrorCode SendTransactionPrecheck(SendTransaction sendTransaction) {
            lock (OpLock) {
                TransactionPrecheck(sendTransaction.ToByteArray());
                try {
                    ulong balance = GetBalance();
                    if (sendTransaction.Amount > balance) {
                        return TxCheckErrorCode.InsufficientBalance;
                    }
                } catch (OverflowException) {
                    return TxCheckErrorCode.BalanceOverflow;
                }
            }
            return TxCheckErrorCode.Success;
        }

        public TxCheckErrorCode ReceiveTransactionPrecheck(ReceiveTransaction receiveTransaction, SendTransaction targetTransaction) {
            lock (OpLock) {
                byte[] hash = receiveTransaction.GetHash();
                TxCheckErrorCode sendCheck = targetTransaction.VerifyWithReceiveTransaction(receiveTransaction);
                if (sendCheck == TxCheckErrorCode.Success) {
                    return TxCheckErrorCode.Success;
                } else {
                    return sendCheck;
                }
            }
        }

        public TxCheckErrorCode TransactionPrecheck(byte[] transaction) {
            lock (OpLock) {
                byte type = Transaction.GetTransactionType(transaction);
                if (RootTransaction == null && type != RootTransaction.RootTransactionType) {
                    //root transaction not loaded, consider resync
                    return TxCheckErrorCode.InvalidPreviousHash;
                }
                if (Transaction.GetTransactionRAP(transaction) != GenesisBlock.RAP) {
                    return TxCheckErrorCode.InvalidRAP;
                }
                if (!Transaction.GetTransactionPreviousHash(transaction).SequenceEqual(GetLastPreviousHash())) {
                    return TxCheckErrorCode.InvalidPreviousHash;
                }
                byte[] publicKey = Transaction.GetTransactionPublicKey(transaction);
                if (publicKey != null && !AnoBITCrypto.PublicKeyToRIPEMD160(publicKey).SequenceEqual(RIPEMD160)) {
                    return TxCheckErrorCode.Unknown;
                }
                if (!Transaction.HasValidNonce(
                    Transaction.GetDifficulty(Transaction.GetTransactionType(transaction)),
                    transaction)) {
                    return TxCheckErrorCode.InvalidNonce;
                }

                return TxCheckErrorCode.Success;
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
