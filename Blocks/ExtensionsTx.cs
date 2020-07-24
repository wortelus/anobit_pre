using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet.Blocks {
    internal static partial class ExtensionsTx {
        /// <summary>
        /// Compares two separate blockchains, returns delta, can throw exceptions signalising a fork or unrelated blockchains.
        /// </summary>
        /// <param name="aChain"></param>
        /// <param name="bChain"></param>
        /// <returns>Negative delta value if aChain is lower length and vice versa.</returns>
        public static int CompareSeparateBlockchains(IEnumerable<byte[]> aChain, IEnumerable<byte[]> bChain) {
            int aLen = aChain.Count();
            int bLen = bChain.Count();

            if (aLen < bLen) {
                //go negative return index
                for (int i = 0; i < aLen; i++) {
                    if (!aChain.ElementAt(i).SequenceEqual(aChain.ElementAt(i))) {
                        throw new Exception("Incompatible chain sequences, possible fork detected.");
                    }
                }
                return aLen - bLen;
            } else if (bLen < aLen) {
                //go positive return index
                for (int i = 0; i < bLen; i++) {
                    if (!aChain.ElementAt(i).SequenceEqual(aChain.ElementAt(i))) {
                        throw new Exception("Incompatible chain sequences, possible fork detected.");
                    }
                }
                return aLen - bLen;
            } else {
                //chains are equal
                return 0;
            }
        }
    }
}
