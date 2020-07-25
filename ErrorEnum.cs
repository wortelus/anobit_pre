using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    public enum TxCheckErrorCode : byte {
        /// <summary>
        /// Unknown error code
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Replay attack protection failed....network incompatibility.
        /// </summary>
        InvalidRAP,

        /// <summary>
        /// Unknown transaction type....outdated software or malicious transaction.
        /// </summary>
        InvalidType,

        /// <summary>
        /// Too low nonce, hash doesn't meet difficulty requirements...malicious transaction.
        /// </summary>
        InvalidNonce,

        /// <summary>
        /// Previous hash not found, but not forked....past blocks maybe missing if not malicious, consider resync.
        /// </summary>
        InvalidPreviousHash,

        /// <summary>
        /// Balance overflow, but passed precheck....malicious transaction.
        /// </summary>
        BalanceOverflow,

        /// <summary>
        /// Insufficient balance, malicious transaction.
        /// </summary>
        InsufficientBalance,

        /// <summary>
        /// Same as InvalidPreviousHash, but same tx with previous hash has been found....fork -> vote needed.
        /// </summary>
        PreviousHashFork,

        /// <summary>
        /// Transaction is null....wallet error, so just ignore.
        /// </summary>
        NullTx,

        /// <summary>
        /// Target transaction (ex. receive -> send) is null....consider resync.
        /// </summary>
        NullTargetTx,

        /// <summary>
        /// Target tx is not compatible/valid with the tx....malicious transaction.
        /// </summary>
        InvalidTargetTx,

        /// <summary>
        /// Too short or too long....malicious transaction.
        /// </summary>
        OutOfBounds,

        /// <summary>
        /// Target send transaction has already been spent....malicious transaction.
        /// </summary>
        TargetTxAlreadySpent,

        /// <summary>
        /// Hash of the genesis block is different than the hard-coded version....network incompatibility.
        /// </summary>
        RootTransactionUnknownGenesisBlockHash,

        /// <summary>
        /// Transaction couldn't be added because the network said no. ¯\_(ツ)_/¯
        /// </summary>
        InsufficientVoteWeight,

        /// <summary>
        /// Success, transaction accepted.
        /// </summary>
        Success = 255,
    }
}
