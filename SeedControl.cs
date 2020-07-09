using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AnoBIT_Wallet {
    class SeedControl {
        public static IVersionPreprocessor VersionPreprocessor(int version) {
            switch (version) {
                case 1:
                    return new VAlphaPreprocessor();
            }
            throw new Exception("Unknown seed preprocessor");
        }
    }

    public interface IVersionPreprocessor {
        string CreateNew(int words);
        Encoding GetEncoding();
        AddressConfig[] GetEncryptedAddresses(string seed, int count, byte[] password, Random random);
        AddressConfig[] GetEncryptedAddresses(byte[] keyStretchedSeed, int count, byte[] password, Random random);
        AddressConfig[] GetAddresses(string seed, int count);
        AddressConfig[] GetAddresses(byte[] keyStretchedSeed, int count);
        AddressConfig GetAddress(string seed, int index);
        AddressConfig GetAddress(byte[] keyStretchedSeed, int index);
        byte[] KeyStretchingSeed(string seed);
        int GetVersion();
    }

    public class VAlphaPreprocessor : IVersionPreprocessor {
        private int Version = 1;

        public int GetVersion() {
            return Version;
        }

        private string[] GetWords(string[] input, int words) {
            int len = input.Length;
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            string[] output = new string[words];
            for (int i = 0; i < words; i++) {
                output[i] = input[Next(0, len)];
            }
            return output;
        }

        private int Next(int minValue, int maxValue) {
            if (minValue > maxValue) {
                throw new ArgumentOutOfRangeException();
            }
            return (int)Math.Floor((minValue + ((double)maxValue - minValue) * NextDouble()));
        }

        private double NextDouble() {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            var data = new byte[sizeof(uint)];
            rng.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return randUint / (uint.MaxValue + 1.0);
        }

        public Encoding GetEncoding() {
            return Encoding.ASCII;
        }

        public string CreateNew(int words) {
            string[] seedArray = GetWords(Properties.Settings.Default.AlphaWordlist.Split(','), words);
            string output = string.Empty;
            foreach (string s in seedArray) {
                output += s + " ";
            }
            return output.Trim();
        }

        public byte[] KeyStretchingSeed(string seed) {
            byte[] byteSeed = GetEncoding().GetBytes(seed);
            byte[] origin = byteSeed;
            SHA256Managed SHA256 = new SHA256Managed();
            for (int i = 0; i < 1000; i++) {
                origin = SHA256.ComputeHash(origin);
            }
            return origin;
        }

        public byte[] KeyStretchingAddress(byte[] stretchedSeed, int index) {
            SHA256Managed SHA256 = new SHA256Managed();
            for (int i = 0; i < 100; i++) {
                stretchedSeed = SHA256.ComputeHash(stretchedSeed.Concat(BitConverter.GetBytes(index)).ToArray());
            }
            return stretchedSeed;
        }

        public AddressConfig GetAddress(string seed, int index) {
            byte[] keyStretchedSeed = KeyStretchingSeed(seed);
            byte[] keyStretchedAddress = KeyStretchingAddress(keyStretchedSeed, index);
            return new AddressConfig(keyStretchedAddress);
        }

        public AddressConfig GetAddress(byte[] keyStretchedSeed, int index) {
            byte[] keyStretchedAddress = KeyStretchingAddress(keyStretchedSeed, index);
            return new AddressConfig(keyStretchedAddress);
        }

        public AddressConfig[] GetAddresses(string seed, int count) {
            byte[] keyStretchedSeed = KeyStretchingSeed(seed);
            AddressConfig[] addresses = new AddressConfig[count];
            for (int i = 0; i < addresses.Length; i++) {
                addresses[i] = new AddressConfig(KeyStretchingAddress(keyStretchedSeed, i));
            }
            return addresses;
        }

        public AddressConfig[] GetAddresses(byte[] keyStretchedSeed, int count) {
            AddressConfig[] addresses = new AddressConfig[count];
            for (int i = 0; i < addresses.Length; i++) {
                addresses[i] = new AddressConfig(KeyStretchingAddress(keyStretchedSeed, i));
            }
            return addresses;
        }

        public AddressConfig[] GetEncryptedAddresses(string seed, int count, byte[] password, Random random) {
            byte[] keyStretchedSeed = KeyStretchingSeed(seed);
            AddressConfig[] addresses = new AddressConfig[count];
            for (int i = 0; i < addresses.Length; i++) {
                addresses[i] = new AddressConfig(KeyStretchingAddress(keyStretchedSeed, i), password, random);
            }
            return addresses;
        }

        public AddressConfig[] GetEncryptedAddresses(byte[] keyStretchedSeed, int count, byte[] password, Random random) {
            AddressConfig[] addresses = new AddressConfig[count];
            for (int i = 0; i < addresses.Length; i++) {
                addresses[i] = new AddressConfig(KeyStretchingAddress(keyStretchedSeed, i), password, random);
            }
            return addresses;
        }
    }
}

