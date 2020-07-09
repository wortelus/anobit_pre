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

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for WindowSHOWPRIVKEY.xaml
    /// </summary>
    public partial class WindowShowPrivateKey : Window
    {
        AddressConfig addressConfig;

        public WindowShowPrivateKey(AddressConfig _addressConfig)
        {
            addressConfig = _addressConfig;
            InitializeComponent();

            labelAddress.Content = addressConfig.Base58Address;
            textBoxNonBase58.Text = addressConfig.RIPEMD160.ToHexString();
            if (addressConfig.GetPublicKey() != null) {
                textBoxPublicKey.Text = addressConfig.GetPublicKey().ToHexString();
            }
            if (addressConfig.GetPrivateKey() != null) {
                textBoxPrivateKey.Text = addressConfig.GetPrivateKey().ToHexString();
            }
        }

        private void FormatChange(object sender, RoutedEventArgs e)
        {
            if (radioButtonWifFormat.IsChecked == true) {
                if (addressConfig.GetPrivateKey() != null) {
                    textBoxPrivateKey.Text = AnoBITCrypto.GetWIFPrivateKey(addressConfig.GetPrivateKey().ToHexString());
                }
            } else {
                if (addressConfig.GetPrivateKey() != null) {
                    textBoxPrivateKey.Text = (addressConfig.GetPrivateKey().ToHexString());
                }
            }
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FormatChange(null, null);
            }
        }
    }
}
