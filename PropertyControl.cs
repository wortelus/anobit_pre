using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AnoBIT_Wallet {

    public class PropertyControl {
        public const string WalletsFolder = "wallets";

        public static string GetWalletsFolder() {
            return Path.Combine(EvaluateRootPath(), Properties.Settings.Default.RootPathFolder, WalletsFolder);
        }

        public static bool CreateRootFolderIfNotExists() {
            bool Exists = CheckRootFolder();
            if (!Exists) {
                Directory.CreateDirectory(Path.Combine(EvaluateRootPath(), Properties.Settings.Default.RootPathFolder));
                Directory.CreateDirectory(Path.Combine(EvaluateRootPath(), Properties.Settings.Default.RootPathFolder, WalletsFolder));
                
                return false;
            }
            return true;
        }

        public static bool ExistingWallets() {
            string walletPath = Path.Combine(EvaluateRootPath(), Properties.Settings.Default.RootPathFolder, WalletsFolder);
            if (Directory.Exists(walletPath)) {
                string[] files = Directory.GetFiles(walletPath);
                foreach (var file in files) {
                    if (Path.GetExtension(file).ToLower() == ".xml") {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CheckRootFolder() {
            string DirectPath = EvaluateAnoBITFolder();
            if (Directory.Exists(DirectPath)) {
                return true;
            }
            return false;
        }

        public static string EvaluateAnoBITFolder() {
            var AnoBITFolder = Properties.Settings.Default.RootPathFolder;

            if (string.IsNullOrEmpty(AnoBITFolder)) {
                throw new Exception("AnoBIT folder couldn't be evaluated, check the AnoBIT config");
            }
            return Path.Combine(EvaluateRootPath(), AnoBITFolder.Trim());
        }

        public static string EvaluateRootPath() {
            string RootPath = Properties.Settings.Default.RootPath;

            if (string.IsNullOrEmpty(RootPath)) {
                throw new Exception("Root path couldn't be evaluated, check the AnoBIT config");
            }

            if (RootPath.Trim() == "SpecialFolder.ApplicationData") {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            } else {
                return RootPath.Trim();
            }
        }

        public bool IsDirectoryEmpty(string path) {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
