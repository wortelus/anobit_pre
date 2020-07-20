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

        private const string AccountCreateQuery = "CREATE TABLE IF NOT EXISTS {0} (hash BLOB PRIMARY KEY, prehash BLOB NOT NULL UNIQUE, type INTEGER NOT NULL, owner BLOB NOT NULL, amount INTEGER, target BLOB, payload BLOB);";

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

        private bool PrepareInsertStatement(string databaseName, DbTxEntry dbTxEntry) {
            try {
                using (var command = new SQLiteCommand("INSERT INTO " + databaseName + " (hash, prehash, owner, type, amount, target, payload) VALUES (@hash, @prevhash, @owner, @type, @amount, @target, @payload)", DbConnection)) {
                    command.Parameters.Add("@hash", DbType.Binary, dbTxEntry.Hash.Length).Value = dbTxEntry.Hash;
                    command.Parameters.Add("@prehash", DbType.Binary, dbTxEntry.PreviousHash.Length).Value = dbTxEntry.PreviousHash;
                    command.Parameters.Add("@owner", DbType.Binary, dbTxEntry.SenderPublicKey.Length).Value = dbTxEntry.SenderPublicKey;
                    command.Parameters.Add("@type", DbType.Binary, 1).Value = dbTxEntry.Type; //TODO
                    command.Parameters.Add("@amount", DbType.Binary, 8).Value = dbTxEntry.Amount;
                    command.Parameters.Add("@target", DbType.Binary, dbTxEntry.Target.Length).Value = dbTxEntry.Target;
                    command.Parameters.Add("@payload", DbType.Binary, dbTxEntry.Payload.Length).Value = dbTxEntry.Payload;
                    command.ExecuteNonQuery();
                    return true;
                }
            } catch {
                return false;
            }
        }

        public class DbTxEntry {
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
                    if (payload.Length > 200) { //TODO: make a function to check max size, or pass it as parameter
                        throw new AnoBITCryptoException("Payload for transaction is too big (>32 bytes).");
                    }
                    payload = value;
                }
            }
        }
    }
}
