using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AnoBIT_Wallet {
    public abstract class BaseAddress : AnoBITCrypto {
        [XmlIgnore]
        protected byte[] PrivateKey { get; set; }
        [XmlIgnore]
        protected byte[] PublicKey { get; set; }
        [XmlIgnore]
        public byte[] RIPEMD160 { get; set; }
        [XmlIgnore]
        public string Base58Address { get; set; }
    }

    /*public class Address : BaseAddress {
        public int WalletConfigIndex = -1;
        private byte[] lastTransactionHash;

        public bool Spendable {
            get {
                if (GetPrivateKey() == null) {
                    return false;
                }
                return true;
            }
        }
        public bool Verifiable {
            get {
                if (GetPublicKey() == null) {
                    return false;
                }
                return true;
            }
        }

        public byte[] LastTransactionHash {
            get {
                return lastTransactionHash;
            }
            set {
                if (value.Length == 32) {
                    lastTransactionHash = value;
                } else {
                    throw new Exception(string.Format("Error while setting the last transaction hash, expected 32 bytes, got {0}. Are you using SHA-256?", value));
                }
            }
        }

        public Address() { }

        public Address(string WIF) {
            PrivateKey = GetHexPrivateKey(WIF);
            PublicKey = ToPublicKey(PrivateKey);
            RIPEMD160 = PublicKeyToRIPEMD160(PublicKey);
            Base58Address = RIPEMD160.ToBase58();
        }
        public Address(byte[] hexPrivateKey) {
            PrivateKey = hexPrivateKey;
            PublicKey = ToPublicKey(PrivateKey);
            RIPEMD160 = PublicKeyToRIPEMD160(PublicKey);
            Base58Address = RIPEMD160.ToBase58();
        }

        internal void AddPrivateKey(byte[] hexPrivateKey) {
            PrivateKey = hexPrivateKey;
        }

        internal void AddPrivateKey(string WIF) {
            PrivateKey = GetHexPrivateKey(WIF);
        }

        internal void AddPublicKey(byte[] publicKey) {
            PublicKey = publicKey;
        }

        internal byte[] GetPrivateKey() {
            if (PrivateKey == null) {
                return null;
            } else if (PrivateKey.Length == 0) {
                return null;
            } else {
                return PrivateKey;
            }
        }

        internal byte[] GetPublicKey() {
            if (PublicKey == null) {
                return null;
            } else if (PublicKey.Length == 0) {
                return null;
            } else {
                return PublicKey;
            }
        }*/

        /*public AddressConfig ToAddressConfig(Color color, string description) {
            byte[] rpmd = RIPEMD160;
            return new AddressConfig {
                RIPEMD160 = rpmd,
                BackColor = color,
                Desc = description,
            };
        }
    }*/
}
