using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace AnoBIT_Wallet {

    public static class WalletConfig {

        public static byte[] PasswordToByteArray(string password) {
            return new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public static string GetEncryptionType() {
            //just for verification it was encrypted with AES, if any changes, this one should increment.
            return "AES-AB_1.0";
        }

        public static Config GetNewConfig(string password, int words, int addressCount) {
            IVersionPreprocessor preprocessor = SeedControl.VersionPreprocessor(1);
            string seed = preprocessor.CreateNew(words);
            byte[] seedByteArray = preprocessor.GetEncoding().GetBytes(seed);
            byte[] seedIv = AES.GetRandomIV();

            SHA256Managed SHA256 = new SHA256Managed();
            byte[] hashedPassword = SHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
            byte[] doubleHashedPassword = SHA256.ComputeHash(hashedPassword);

            byte[] keyStretchedSeed = preprocessor.KeyStretchingSeed(seed);
            byte[] stretchedSeedIv = AES.GetRandomIV();

            AddressConfig[] addresses = preprocessor.GetAddresses(keyStretchedSeed, addressCount);

            return new Config {
                EncryptedSeed = AES.EncryptWithPrefix(seedByteArray, hashedPassword, seedIv),
                EncryptionType = GetEncryptionType(),
                DoubleHashPassword = doubleHashedPassword,
                KeyStretchedSeed = AES.EncryptWithPrefix(keyStretchedSeed, hashedPassword, stretchedSeedIv),
                ActiveAddressIndex = 0,
                Addresses = addresses.ToList(),
            };
        }

        public static Config GetNewConfig(string seed, string password, int addressCount, IVersionPreprocessor preprocessor) {
            Random random = new Random();

            byte[] seedByteArray = preprocessor.GetEncoding().GetBytes(seed);
            byte[] seedIv = AES.GetRandomIV();

            SHA256Managed SHA256 = new SHA256Managed();
            byte[] hashedPassword = SHA256.ComputeHash(Encoding.UTF8.GetBytes(password));
            byte[] doubleHashedPassword = SHA256.ComputeHash(hashedPassword);

            byte[] keyStretchedSeed = preprocessor.KeyStretchingSeed(seed);
            byte[] stretchedSeedIv = AES.GetRandomIV();

            AddressConfig[] addresses = preprocessor.GetEncryptedAddresses(keyStretchedSeed, addressCount, hashedPassword, random);

            return new Config {
                EncryptedSeed = AES.EncryptWithPrefix(seedByteArray, hashedPassword, seedIv),
                EncryptionType = GetEncryptionType(),
                DoubleHashPassword = doubleHashedPassword,
                KeyStretchedSeed = AES.EncryptWithPrefix(keyStretchedSeed, hashedPassword, stretchedSeedIv),
                ActiveAddressIndex = 0, //automatically sets the first address as the default one
                Addresses = addresses.ToList(),
            };
        }

        public static string Serialize<T>(this T value) {
            if (value == null) {
                return string.Empty;
            }
            try {
                var xmlWriterSettings = new XmlWriterSettings() { Indent = true };
                var xmlSerializer = new XmlSerializer(typeof(T));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, xmlWriterSettings)) {
                    xmlSerializer.Serialize(writer, value);
                    return stringWriter.ToString();
                }
            } catch (Exception ex) {
                throw new XmlException("An error occurred during serialization", ex);
            }
        }

        public static Config DeserializeA(string xmlFile) {
            var serializer = new XmlSerializer(typeof(Config));
            Config result;

            using (TextReader reader = new StringReader(xmlFile)) {
                result = (Config)serializer.Deserialize(reader);
            }
            return result;
        }

        public static Config Deserialize(string xmlFile) {
            if (string.IsNullOrWhiteSpace(xmlFile)) {
                throw new Exception("Whoops, the wallet file you tried to open is empty.");
            }
            try {
                var xmlSerializer = new XmlSerializer(typeof(Config));
                var stringReader = new StringReader(xmlFile);
                using (var reader = XmlReader.Create(stringReader)) {
                    Config output = (Config)xmlSerializer.Deserialize(reader, xmlFile);
                    return output;
                }
            } catch (Exception ex) {
                throw new XmlException("An error occurred during deserialization: " + ex.Message);
            }
        }

        public static AddressConfig GetAddress(this AddressConfig addressConfig, int walletConfigIndex) {
            AddressConfig output = new AddressConfig() {
                Base58Address = AnoBITCrypto.RIPEMD160ToAddress(addressConfig.RIPEMD160),
                RIPEMD160 = addressConfig.RIPEMD160,
                WalletConfigIndex = walletConfigIndex,
            };
            return output;
        }

        public static AddressConfig GetAddress(this OffseedAddressConfig addressConfig, int walletConfigIndex) {
            AddressConfig output = new AddressConfig() {
                Base58Address = AnoBITCrypto.RIPEMD160ToAddress(addressConfig.RIPEMD160),
                RIPEMD160 = addressConfig.RIPEMD160,
                WalletConfigIndex = walletConfigIndex,
            };

            if (addressConfig.PrivateKey != null && addressConfig.PrivateKey.Length != 0) {
                output.AddPrivateKey(addressConfig.PrivateKey);
            }

            return output;
        }


        public static AddressConfig GetActiveAddress(this Config config) {
            AddressConfig output = null;

            if (config.ActiveAddressIndex < 1073741824) {
                //Active address is in pregen list
                if (config.ActiveAddressIndex >= config.Addresses.Count) {
                    throw new Exception(string.Format("Active address couldn't be retrieved (pregen) because the index {0} is out of bounds.", config.ActiveAddressIndex));
                }
                AddressConfig activeAddress = config.Addresses[config.ActiveAddressIndex];
                output = new AddressConfig() {
                    Base58Address = AnoBITCrypto.RIPEMD160ToAddress(activeAddress.RIPEMD160),
                    RIPEMD160 = activeAddress.RIPEMD160,
                    WalletConfigIndex = config.ActiveAddressIndex,
                };

            } else {
                //Active address is in offseed list
                int offseedIndex = config.ActiveAddressIndex % 1073741824;
                if (offseedIndex >= config.OffseedAddresses.Count) {
                    throw new Exception(string.Format("Active address couldn't be retrieved (offseed) because the index {0} is out of bounds.", offseedIndex));
                }

                OffseedAddressConfig activeAddress = config.OffseedAddresses[offseedIndex];

                if (activeAddress.PrivateKey != null && activeAddress.PrivateKey.Length != 0) {
                    //TODO: test integrity between public key, address and private key
                    output = new AddressConfig(activeAddress.PrivateKey);
                } else {
                    output = new AddressConfig() {
                        Base58Address = AnoBITCrypto.RIPEMD160ToAddress(activeAddress.RIPEMD160),
                        RIPEMD160 = activeAddress.RIPEMD160,
                        WalletConfigIndex = offseedIndex,
                    };
                }
            }
            return output;
        }
    }

    [XmlRoot("AnoBITWallet")]
    public class Config {
        [XmlElement("Seed")]
        public byte[] EncryptedSeed { get; set; }
        [XmlElement("EncryptionType")]
        public string EncryptionType { get; set; }
        [XmlElement("DoubleHashPassword")]
        public byte[] DoubleHashPassword { get; set; }
        [XmlElement("KeyStretchedSeed")]
        public byte[] KeyStretchedSeed { get; set; }
        [XmlElement("ActiveAddressIndex"), DefaultValue(0)]
        public int ActiveAddressIndex;
        [XmlElement("Address")]
        public List<AddressConfig> Addresses { get; set; }
        [XmlElement("OffseedAddress")]
        public List<OffseedAddressConfig> OffseedAddresses { get; set; }
        [XmlElement("Contact")]
        public List<ContactConfig> Contacts { get; set; }

        public void DecryptWallet(byte[] password) {
            //TODO: return list of undecoded addresses instead of throwing an error
            for (int i = 0; i < Addresses.Count; i++ ) {
                Addresses[i].Decrypt(password);
            }
            for (int i = 0; i < OffseedAddresses.Count; i++) {
                //TODO
                //OffseedAddresses[i].Decrypt(password);
            }
        }

        public void EncryptWallet(byte[] password) {
            //TODO: return list of undecoded addresses instead of throwing an error
            Random r = new Random();
            for (int i = 0; i < Addresses.Count; i++) {
                Addresses[i].Encrypt(password, ref r);
            }
            for (int i = 0; i < OffseedAddresses.Count; i++) {
                //OffseedAddresses[i].Decrypt(password);
            }
        }
    }

    public class AddressConfig {

        //runtime properties
        [XmlIgnore]
        public decimal Balance { get; set; }
        [XmlIgnore]
        public int BlockCount { get; set; }
        [XmlIgnore]
        public int WalletConfigIndex = -1;
        [XmlIgnore]
        private byte[] lastTransactionHash;

        //address cryptographic values needed to function
        [XmlIgnore]
        private byte[] privateKey;

        [XmlIgnore]
        protected byte[] PrivateKey {
            get {
                return privateKey;
            }
            set {
                if (value == null) {
                    privateKey = value;
                    return;
                }

                if (value.Length != 32) {
                    throw new Exception(string.Format("The private key has wrong length. 32 bytes expected, got {0}.", value));
                }
                if (GetPublicKey() != null) {
                    if (PublicKey == AnoBITCrypto.ToPublicKey(value)) {
                        throw new Exception(string.Format("The private key provided for address {0} doesn't correspond with already applied public key, this could create some serious trouble!\r\n" +
                            "Current Address: {1}\r\n" +
                            "Tried to add private key for this address: {2}", Base58Address, Base58Address, AnoBITCrypto.RIPEMD160ToAddress(AnoBITCrypto.PublicKeyToRIPEMD160(AnoBITCrypto.ToPublicKey(value)))));
                    }
                }
                privateKey = value;
                PublicKey = AnoBITCrypto.ToPublicKey(value);
            }
        }
        [XmlIgnore]
        private byte[] publicKey;

        [XmlElement("PublicKey")]
        protected byte[] PublicKey {
            get {
                return publicKey;
            }
            set {
                if (value == null) {
                    publicKey = value;
                    return;
                }

                if (value.Length != 65) {
                    throw new Exception(string.Format("The public key has wrong length. 65 bytes expected, got {0}.", value));
                }

                byte[] generatedAddress = AnoBITCrypto.PublicKeyToRIPEMD160(value);
                if (RIPEMD160 != null) {
                    if (RIPEMD160.SequenceEqual(generatedAddress) == false) {
                        throw new Exception(string.Format("The public key provided for address {0} doesn't correspond with the address, this could create some serious trouble!\r\n" +
                                                    "Current Address: {1}\r\n" +
                                                    "Tried to add public key from this address: {2}", Base58Address, Base58Address, AnoBITCrypto.RIPEMD160ToAddress(generatedAddress)));
                    }
                }

                publicKey = value;
                RIPEMD160 = generatedAddress;
            }
        }
        [XmlIgnore]
        private byte[] rIPEMD160;
        [XmlElement("RIPEMD160")]
        public byte[] RIPEMD160 {
            get {
                return rIPEMD160;
            }
            set {
                if (value == null) {
                    rIPEMD160 = value;
                    return;
                }

                if (value.Length != 20) {
                    throw new Exception(string.Format("The public key has wrong length. 65 bytes expected, got {0}.", value));
                }

                rIPEMD160 = value;
                Base58Address = AnoBITCrypto.RIPEMD160ToAddress(value);
            }
        }


        [XmlIgnore]
        public string Base58Address { get; set; }

        //address values exposed to serialization
        [XmlElement("EncryptedPrivateKey"), /*XmlElement(DataType = "hexBinary")*/]
        public byte[] EncryptedPrivateKey { get; set; }

        [XmlElement("SeedNonce")]
        public byte[] SeedNonce { get; set; }

        [XmlElement("Desc"), DefaultValue("")]
        public string Desc { get; set; }

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

        public void Encrypt(byte[] password, ref Random random) {
            if (GetPrivateKey() == null && EncryptedPrivateKey != null) {
                return;
            }
            if (GetPrivateKey() == null && EncryptedPrivateKey == null) {
                throw new Exception("Tried to encrypt an address while the address doesn't have supplied private key.");
            }
            EncryptedPrivateKey = AES.EncryptWithPrefix(PrivateKey, password, AES.GetRandomIV(random));
            PrivateKey = null;
            PublicKey = null;
        }

        public void Lock() {
            if (GetPrivateKey() != null && EncryptedPrivateKey == null) {
                throw new Exception("Tried to lock private key that somehow doesn't have encrypted private key but only raw. hmm");
            }
            PrivateKey = null;
        }

        public bool Decrypt(byte[] password) {
            if (GetPrivateKey() != null) {
                return true;
            }
            PrivateKey = AES.DecryptWithPrefix(EncryptedPrivateKey, password);
            return true;
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

        public AddressConfig() { }

        public AddressConfig(string WIF) {
            PrivateKey = AnoBITCrypto.GetHexPrivateKey(WIF);
        }

        public AddressConfig(byte[] hexPrivateKey) {
            PrivateKey = hexPrivateKey;
        }

        public AddressConfig(byte[] hexPrivateKey, byte[] password, Random random) {
            PrivateKey = hexPrivateKey;
            EncryptedPrivateKey = AES.EncryptWithPrefix(PrivateKey, password, AES.GetRandomIV(random));
        }
        //TODO: integrity check between private/public keys
        internal void AddPrivateKey(byte[] hexPrivateKey) {
            PrivateKey = hexPrivateKey;
        }

        internal void AddPrivateKey(string WIF) {
            PrivateKey = AnoBITCrypto.GetHexPrivateKey(WIF);
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
        }
    }

    public class OffseedAddressConfig {
        [XmlElement("PrivateKey")]
        public byte[] PrivateKey { get; set; }

        [XmlElement("RIPEMD160")]
        public byte[] RIPEMD160 { get; set; }

        [XmlIgnore]
        public Color BackColor { get; set; }

        [XmlElement("BackColor")]
        public int BackColorAsArgb {
            get { return (BackColor.A << 24) | (BackColor.R << 16) | (BackColor.G << 8) | BackColor.B; ; }
            set {
                BackColor = Color.FromArgb((byte)(value >> 24),
                           (byte)(value >> 16),
                           (byte)(value >> 8),
                           (byte)(value));
            }
        }

        [XmlElement("Desc"), DefaultValue("")]
        public string Desc { get; set; }
    }

    public class ContactConfig {
        [XmlElement("Name")]
        public string Name { get; set; }
        [XmlElement("RIPEMD160")]
        public byte[] RIPEMD160 { get; set; }

        [XmlIgnore]
        public Color BackColor { get; set; }

        [XmlElement("BackColor")]
        public int BackColorAsArgb {
            get { return (BackColor.A << 24) | (BackColor.R << 16) | (BackColor.G << 8) | BackColor.B; ; }
            set {
                BackColor = Color.FromArgb((byte)(value >> 24),
                           (byte)(value >> 16),
                           (byte)(value >> 8),
                           (byte)(value));
            }
        }

        [XmlElement("Desc"), DefaultValue("")]
        public string Desc { get; set; }
    }
}
