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

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for WindowRemoveAddress.xaml
    /// </summary>
    public partial class WindowRemoveAddress : Window
    {
        string LineToRemove = String.Empty;
        string WalletPath;
        string Address;
        string WalletContent;
        string ranum;
        Random rnd = new Random();

        public WindowRemoveAddress()
        {
            InitializeComponent();
            ranum = String.Empty + rnd.Next(1000, 9999);
            labelNUMBER.Content = ranum;
            Keyboard.Focus(textBox1);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (textBox1.Text == ranum)
            {
                WalletContent = File.ReadAllText(WalletPath);
                WalletContent = WalletContent.Replace(LineToRemove, String.Empty);
                File.WriteAllText(WalletPath, WalletContent);
                Close();
            }
            else
            {
                labelNUMBER.Content = String.Empty;
                ranum = "" + rnd.Next(1000, 9999);
                labelNUMBER.Content = ranum;
                MessageBox.Show("Entered verification code is invalid. Re-enter your verification code and try again", "AnoBIT", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        public void WRARemove(string line, string add, string path)
        {
            WalletPath = path;
            Address = add;
            LineToRemove = line;
            labelADDRESS.Content = add;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
