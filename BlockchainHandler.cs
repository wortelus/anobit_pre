using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using AnoBIT_Wallet.Blocks;

namespace AnoBIT_Wallet {
    class BlockchainHandler {
        SQLiteConnection DbConnection;

        private const string AccountCreateQuery = "CREATE TABLE IF NOT EXISTS {0} (hash BLOB PRIMARY KEY, prehash BLOB NOT NULL UNIQUE, type INTEGER NOT NULL, owner BLOB NOT NULL, ownerhash BLOB NOT NULL, amount INTEGER, target BLOB, payload BLOB);";
        private string DatabaseName = "tx";

        Dictionary<byte[], Account> tempBlockchain = new Dictionary<byte[], Account>();

        public BlockchainHandler(string name) {
            DbConnection = new SQLiteConnection("Data Source=" + name + ";Version=3;");
            SQLiteConnection.CreateFile(name);
            DbConnection.Open();
        }

        ~BlockchainHandler() {
            try {
                DbConnection.Close();
                DbConnection.Dispose();
            } catch { }
        }

        public bool InsertTransaction(byte[] transaction) {
            if (transaction == null) {
                throw new ArgumentNullException("Null transaction provided for insert.");
            }

            byte[] sender = Transaction.GetTransactionPublicKey(transaction);
            if (sender == null) {
                throw new ArgumentException("The transaction for insertion is too short, no public key returned.");
            }

            byte[] senderHash = AnoBITCrypto.PublicKeyToRIPEMD160(sender);

            string createSql = string.Format(AccountCreateQuery, senderHash);
            SQLiteCommand command = new SQLiteCommand(createSql, DbConnection);
            command.ExecuteNonQuery();
            return false;
        }

        private TxCheckErrorCode ToAccountDictionary(byte[] transaction) {
            //sync with database
            //TODO: possible memory leak attack
            LoadFromDbByOwner(Transaction.GetTransactionPublicKey(transaction));

            //TODO: log exception and return database error
            bool success = DoubleSpendProtection(transaction);
            if (!success) {
                return TxCheckErrorCode.DoubleSpend;
            }

            DbTxEntry dbTxEntry;
            TxCheckErrorCode txCheckErrorCode;
            try {
                dbTxEntry = new DbTxEntry(transaction);
                txCheckErrorCode = tempBlockchain[dbTxEntry.RIPEMD160].InsertTransaction(transaction, GetTargetTransaction(transaction));

            } catch (Exception e) {
                return (TxCheckErrorCode)e.Data["TxCheckErrorCode"];
            }

            if (txCheckErrorCode != TxCheckErrorCode.Success) {
                return txCheckErrorCode;
            }
            success = PrepareInsertStatement(dbTxEntry);
            return success ? TxCheckErrorCode.Success : TxCheckErrorCode.Unknown;
        }

        private void LoadFromDbByOwner(byte[] publicKey) {
            byte[] ripemd = AnoBITCrypto.PublicKeyToRIPEMD160(publicKey);
            LoadFromDbByRIPEMD(ripemd);
        }

        private void LoadFromDbByRIPEMD(byte[] ripemd) {
            List<byte[]> payload = new List<byte[]>();

            using (var command = new SQLiteCommand("SELECT payload FROM + " + DatabaseName + " WHERE ownerhash = @ownerhash", DbConnection)) {
                command.Parameters.Add("@ownerhash", DbType.Binary, ripemd.Length).Value = ripemd;
                var reader = command.ExecuteReader();
                int i = 0;
                while (reader.Read()) {
                    payload.Add((byte[])reader["payload"]);
                    i++;
                }
                if (i == 0) {
                    //tempBlockchain.Add(ripemd, new Account(ripemd));
                }
            }

            List<byte[]> sortedTx = Transaction.SortTransactions(payload, true);
            if (tempBlockchain.ContainsKey(ripemd)) {
                //there are tx's in db, and there are tx's in tempBlockchain. Let's do the magic
                List<byte[]> hashListDictionary = tempBlockchain[ripemd].GetHashList(); //hashlist from dictionary
                List<byte[]> hashListDb = Transaction.GetHashList(sortedTx, false, true); //hashlist from Db
                int comparedChains = ExtensionsTx.CompareSeparateBlockchains(hashListDictionary, hashListDb);

                if (comparedChains < 0) {
                    //if tempBlockchain is shorter, needs to be filled up from database
                    for (int i = sortedTx.Count + comparedChains; i < sortedTx.Count; i++) {
                        if (tempBlockchain[ripemd].InsertTransaction(sortedTx[i], GetTargetTransaction(sortedTx[i])) != TxCheckErrorCode.Success) {
                            //transaction from db is not accepted
                            //TODO: handling

                            /* DEV NOTES:
                             * if we got here, the db is same as the tempBlockchain, but tempBlockchain is shorter and won't accept 
                             * blocks from db, which is weird
                             * 
                             * possible causes:
                             * the database couldn't be outdated, as is contains more information
                             * the only reasonable cause of this could be invalid Db entry
                             * 
                             * possible missing target transaction, request
                             */
                        }
                    }
                    return;
                } else if (comparedChains > 0) {
                    //if database is shorter than the tempBlockchain, this shouldn't happen as 
                    //the account class should take care of it and add to both or none in case of invalid block entry
                    //in my opinion the proper handling would be to throw an exception and look for fix
                    throw new Exception("Database is shorter than the tempBlockchain, this shouldn't happen.");
                }
                return;
            } else {
                //tempBlockchain is empty, load from database
                tempBlockchain.Add(ripemd, new Account(ripemd));

                for (int i = 0; i < sortedTx.Count; i++) {
                    if (tempBlockchain[ripemd].InsertTransaction(sortedTx[i], GetTargetTransaction(sortedTx[i])) != TxCheckErrorCode.Success) {
                        //transaction from db is not accepted
                        //TODO: handling

                        /* DEV NOTES:
                         * if we got here, the db is same as the tempBlockchain, but tempBlockchain is shorter and won't accept 
                         * blocks from db, which is weird
                         * 
                         * possible causes:
                         * the database couldn't be outdated, as is contains more information
                         * the only reasonable cause of this could be invalid Db entry
                         * 
                         * possible missing target transaction, request from the network
                         */
                    }
                }
            }
        }

        private bool PrepareInsertStatement(DbTxEntry dbTxEntry) {
            try {
                using (var command = new SQLiteCommand("INSERT INTO " + DatabaseName + " (hash, prehash, owner, ownerhash, type, amount, target, payload) VALUES (@hash, @prevhash, @owner, @ownerhash, @type, @amount, @target, @payload)", DbConnection)) {
                    command.Parameters.Add("@hash", DbType.Binary, dbTxEntry.Hash.Length).Value = dbTxEntry.Hash;
                    command.Parameters.Add("@prehash", DbType.Binary, dbTxEntry.PreviousHash.Length).Value = dbTxEntry.PreviousHash;
                    command.Parameters.Add("@owner", DbType.Binary, dbTxEntry.SenderPublicKey.Length).Value = dbTxEntry.SenderPublicKey;
                    command.Parameters.Add("@ownerhash", DbType.Binary, dbTxEntry.RIPEMD160.Length).Value = dbTxEntry.RIPEMD160;
                    command.Parameters.Add("@type", DbType.Byte).Value = dbTxEntry.Type; //TODO
                    command.Parameters.Add("@amount", DbType.UInt64).Value = dbTxEntry.Amount;
                    command.Parameters.Add("@target", DbType.Binary, dbTxEntry.Target.Length).Value = dbTxEntry.Target;
                    command.Parameters.Add("@payload", DbType.Binary, dbTxEntry.Payload.Length).Value = dbTxEntry.Payload;
                    int l = command.ExecuteNonQuery();
                    if (l < 1) {
                        return false;
                    }
                    return true;
                }
            } catch {
                return false;
            }
        }

        public byte[] GetTargetTransaction(byte[] transaction) {
            if (Transaction.GetTransactionType(transaction) == ReceiveTransaction.ReceiveTransactionType) {
                ReceiveTransaction receiveTransaction = new ReceiveTransaction(transaction);
                byte[] payload = null;
                using (var command = new SQLiteCommand("SELECT payload FROM " + DatabaseName + "WHERE hash = @target")) {
                    command.Parameters.Add("@target", DbType.Binary, receiveTransaction.Target.Length).Value = receiveTransaction.Target;
                    var reader = command.ExecuteReader();
                    int i = 0;
                    while (reader.Read()) {
                        if (i >= 1) {
                            Exception exception = new Exception("More than one target transactions found in database!");
                            exception.Data.Add("TxCheckErrorCode", TxCheckErrorCode.Fatal);
                            throw exception;
                        }
                        payload = (byte[])reader["payload"];
                        i++;
                    }
                }
                return payload;
            }
            return null;
        }

        public bool DoubleSpendProtection(byte[] transaction) {
            if (Transaction.GetTransactionType(transaction) == ReceiveTransaction.ReceiveTransactionType) {
                ReceiveTransaction receiveTransaction = new ReceiveTransaction(transaction);
                using (var command = new SQLiteCommand("SELECT payload FROM " + DatabaseName + "WHERE target = @target")) {
                    command.Parameters.Add("@target", DbType.Binary, receiveTransaction.Target.Length).Value = receiveTransaction.Target;
                    var reader = command.ExecuteReader();
                    while (reader.Read()) {
                        return false;
                    }
                }
                return true;
            }
            return true;
        }

        public Account TransactionListToAccount(List<byte[]> transactions, bool fromSecureSource) {
            List<byte[]> sortedTxs = Transaction.SortTransactions(transactions, fromSecureSource);
            Account account = new Account(sortedTxs);
            for (int i = 1; i < transactions.Count; i++) {
                byte[] tx = transactions[i];
                TxCheckErrorCode insert = account.InsertTransaction(tx, GetTargetTransaction(tx));
                if (insert != TxCheckErrorCode.Success) {
                    throw new Exception(string.Format("TransactionListToAccount: accounts couldn't be built, {0} has an transaction error {1} at height {2}. fromSecureSource={3}", 
                        Transaction.GetTransactionPublicKey(transactions[0]),
                        insert,
                        i,
                        fromSecureSource.ToString()
                        ));
                }
            }
            return account;
        }

        public class DbTxEntry {
            public DbTxEntry(byte[] transaction) {
                Exception exception = new Exception("Error during adding to DbTxEntry.");
                exception.Data.Add("TxCheckErrorCode", TxCheckErrorCode.Unknown);

                Type = Transaction.GetTransactionType(transaction);
                if (Type == 255) {
                    exception.Data["TxCheckErrorCode"] = TxCheckErrorCode.InvalidType;
                    throw exception;
                }

                int minSize = Transaction.GetMinSize(Type);
                int maxSize = Transaction.GetMaxSize(Type);

                if (transaction.Length < minSize) {
                    exception.Data["TxCheckErrorCode"] = TxCheckErrorCode.OutOfBounds;
                    throw exception;
                }
                if (transaction.Length > maxSize) {
                    exception.Data["TxCheckErrorCode"] = TxCheckErrorCode.OutOfBounds;
                    throw exception;
                }

                Hash = Transaction.TxHashFunction(transaction);
                PreviousHash = Transaction.GetTransactionPreviousHash(transaction);
                SenderPublicKey = Transaction.GetTransactionPublicKey(transaction);
                Target = Transaction.GetTarget(transaction);
                Amount = Transaction.GetAmount(transaction);
                Signature = Transaction.GetSignature(transaction);
                Payload = transaction;
            }

            public byte Type { get; set; }

            private byte[] hash;
            public byte[] Hash {
                get {
                    return hash;
                }
                set {
                    if (value.Length != 32) {
                        throw new AnoBITCryptoException("Hash for transaction is not 32 bytes.");
                    }
                    hash = value;
                }
            }

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

            public byte[] RIPEMD160 {
                get {
                    return AnoBITCrypto.PublicKeyToRIPEMD160(SenderPublicKey);
                }
            }

            private byte[] target { get; set; }
            public byte[] Target {
                get {
                    return target;
                }
                set {
                    if (target.Length > 32) {
                        throw new AnoBITCryptoException("Target for transaction is too big (> 32 bytes).");
                    }
                    target = value;
                }
            }

            public ulong Amount { get; set; }

            private byte[] signature;
            public byte[] Signature {
                get {
                    return signature;
                }
                set {
                    if (value[30] != 48) {
                        throw new AnoBITCryptoException("Signature for transaction doesn't start with 48.");
                    }
                    if (value.Length > Transaction.MaxSignatureSize) {
                        throw new AnoBITCryptoException("Signature for transaction is too long.");
                    }
                    signature = value;
                }
            }

            private byte[] payload { get; set; }
            public byte[] Payload {
                get {
                    return payload;
                }
                set {
                    if (value.Length > Transaction.GetMaxSize(Type)) { //TODO: make a function to check max size, or pass it as parameter
                        throw new AnoBITCryptoException(string.Format("Payload for transaction is too big (>{0} bytes for type of {1}).", value.Length, Type));
                    } else if (value.Length < Transaction.GetMinSize(Type)) {
                        throw new AnoBITCryptoException(string.Format("Payload for transaction is too small (<{0} bytes for type of {1}).", value.Length, Type));
                    }
                    payload = value;
                }
            }
        }
    }
}
