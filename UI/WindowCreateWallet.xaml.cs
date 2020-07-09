using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.IO;
using System.Xml.Serialization;
using Path = System.IO.Path;

namespace AnoBIT_Wallet {
    /// <summary>
    /// Interaction logic for WindowCreateWallet.xaml
    /// </summary>
    public partial class WindowCreateWallet : Window {
        int cointype = 23;
        SHA256Managed SHA256 = new SHA256Managed();
        IVersionPreprocessor preprocessor = SeedControl.VersionPreprocessor(1);
        private byte initialWordCount;
        string generatedSeed = string.Empty;

        public byte InitialWordCount {
            get { return initialWordCount; }
            set {
                initialWordCount = value;
                CW1txtNum.Text = value.ToString();
            }
        }

        public WindowCreateWallet() {
            InitializeComponent();

            InitialWordCount = 12;
            CW1txtNum.Text = initialWordCount.ToString();
        }

        private void buttonN0_Click(object sender, RoutedEventArgs e) {
            if (radioButtonNewWallet.IsChecked == true) {
                tabControl1.SelectedItem = tabItemCW1;

                generatedSeed = preprocessor.CreateNew(12);
                textBoxSeed.Text = generatedSeed;
            } else if (radioButtonRestoreWallet.IsChecked == true) {
                tabControl1.SelectedItem = tabItemRW1;
            }
        }


        private void ButtonCW1NewSeed_Click(object sender, RoutedEventArgs e) {
            generatedSeed = preprocessor.CreateNew(InitialWordCount);
            textBoxSeed.Text = generatedSeed;
        }

        private void buttonCW1Next_Click(object sender, RoutedEventArgs e) {
            tabControl1.SelectedItem = tabItemCW2;
        }

        private void buttonCW1Back_Click(object sender, RoutedEventArgs e) {
            tabControl1.SelectedItem = tabItemStart;
        }


        private void cmdUp_Click(object sender, RoutedEventArgs e) {
            if (initialWordCount < 255) {
                InitialWordCount++;
            }
        }

        private void cmdDown_Click(object sender, RoutedEventArgs e) {
            if (initialWordCount > 1) {
                InitialWordCount--;
            }
        }

        private void txtNum_TextChanged(object sender, TextChangedEventArgs e) {
            if (CW1txtNum == null) {
                return;
            }

            if (!byte.TryParse(CW1txtNum.Text, out initialWordCount))
                CW1txtNum.Text = initialWordCount.ToString();
        }

        private void buttonCW2Next_Click(object sender, RoutedEventArgs e) {
            tabControl1.SelectedItem = tabItemCW3;
            textBoxCW3Name.Text = IndexedFilename("wallet", "xml");
        }

        string IndexedFilename(string stub, string extension) {
            int ix = 0;
            string filename = null;
            do {
                ix++;
                filename = String.Format("{0}{1}.{2}", stub, ix, extension);
            } while (File.Exists(Path.Combine(PropertyControl.GetWalletsFolder(), filename)));
            return filename;
        }

        private void buttonCW2Back_Click(object sender, RoutedEventArgs e) {
            tabControl1.SelectedItem = tabItemCW1;
        }

        private void textBoxSeedConfirm_TextChanged(object sender, TextChangedEventArgs e) {
            Encoding encoding = preprocessor.GetEncoding();
            byte[] bb = SHA256.ComputeHash(encoding.GetBytes(textBoxSeedConfirm.Text));
            byte[] cc = SHA256.ComputeHash(encoding.GetBytes(generatedSeed));

            if (cc.SequenceEqual(bb)) {
                buttonCW2Next.IsEnabled = true;
            } else {
                buttonCW2Next.IsEnabled = false;
            }
        }

        private void buttonCW3Back_Click(object sender, RoutedEventArgs e) {
            tabControl1.SelectedItem = tabItemCW2;
        }

        private void buttonCW3Done_Click(object sender, RoutedEventArgs e) {
            string savePath = System.IO.Path.Combine(PropertyControl.EvaluateAnoBITFolder(), PropertyControl.WalletsFolder, textBoxCW3Name.Text);

            if (passwordBoxCW3Password.Password == passwordBoxCW3PasswordConfirm.Password) {
                if (!File.Exists(savePath)) {
                    if (string.IsNullOrWhiteSpace(passwordBoxCW3Password.Password) || string.IsNullOrEmpty(passwordBoxCW3Password.Password)) {
                        MessageBox.Show("Enter a valid password.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    } else {
                        Config walletConfig = WalletConfig.GetNewConfig(generatedSeed, passwordBoxCW3Password.Password, 20, preprocessor);
                        string xmlOutput = WalletConfig.Serialize(walletConfig);
                        try {
                            File.WriteAllText(savePath, xmlOutput);
                            Close();
                            //WindowWallet windowWallet = new WindowWallet(savePath, textBoxCW3Name.Text);
                            //windowWallet.Show();
                        } catch (Exception ex) {
                            MessageBox.Show(string.Format("There was an exception during saving a Xml wallet file {0}. {1}", savePath, ex.Message));
                        }
                    }
                } else {
                    MessageBox.Show("File under this name already exists. Try using a different name.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            } else {
                MessageBox.Show("Passwords do not match. Re-Enter them and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        
        }

        private void buttonRW1Next_Click(object sender, RoutedEventArgs e) {
            generatedSeed = textBoxRW1Seed.Text.Trim();
            tabControl1.SelectedItem = tabItemCW3;
            textBoxCW3Name.Text = IndexedFilename("wallet", "xml");
        }
    }
}
