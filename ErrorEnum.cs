using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    public enum TxCheckErrorCode : byte {
        Unknown = 0, //unknown
        InvalidRAP = 1, //replay attack protection failed, transaction is not compatible with the genesis block
        InvalidType = 2, //unknown specified type
        InvalidNonce = 3, //too low nonce, hash doesn't meet difficulty requirements
        InvalidPreviousHash = 4, //previous hash not found, but not forked, so maybe consider resync with network
        BalanceOverflow = 5, //balance overflow, but passed precheck, so this is malicious tx
        PreviousHashFork = 6, //same as InvalidPreviousHash, but same tx with previous hash has been found == fork
        NullTargetTx = 7, //target tx (ex. receive -> send) is null,
        InvalidTargetTx = 8, //target tx is not compatible/valid with the tx
        TooShort = 9,
        TooLong = 10,
        TargetTxAlreadySpent = 11, //target send transaction has already been spent
        RootTransactionUnknownGenesisBlockHash = 12, //Hash of the genesis block is different than the hard-coded version
    }
}
