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
using System.IO;
using System.Security.Cryptography;

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for WindowAddOffseedAddress.xaml
    /// </summary>
    public partial class WindowAddOffseedAddress : Window
    {
        private string WalletPath;
        private string xps = String.Empty;
        private string xps1 = String.Empty;
        private byte[] Password;
        SHA256Managed _SHA256 = new SHA256Managed();

        public WindowAddOffseedAddress()
        {
            InitializeComponent();
        }

        private void buttonImport_Click(object sender, RoutedEventArgs e)
        {

        }

        public void SetInfo(string _WalletPath, byte[] _Password)
        {
            WalletPath = _WalletPath;
            Password = _Password;
        }

        private void WifImport()
        {
            
        }
    }
}
