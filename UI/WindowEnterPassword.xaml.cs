using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interakční logika pro WindowEnterPassword.xaml
    /// </summary>
    public partial class WindowEnterPassword : Window
    {
        private byte[] DoubleHashPassword;
        public string rawPassword {
            get {
                return passwordBox.Password;
            }
        }
        SHA256Managed sHA256 = new SHA256Managed();
        public WindowEnterPassword(byte[] doubleHashPassword)
        {
            InitializeComponent();
            DoubleHashPassword = doubleHashPassword;
            passwordBox.Focus();
            passwordBox.SelectAll();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            byte[] singleHash = sHA256.ComputeHash(Encoding.UTF8.GetBytes(passwordBox.Password));
            if (sHA256.ComputeHash(singleHash).SequenceEqual(DoubleHashPassword))
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Incorrect password entered, please try again.", "AnoBIT", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                passwordBox.Clear();
            }
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
           if (e.Key == Key.Enter)
            {
                byte[] singleHash = sHA256.ComputeHash(Encoding.UTF8.GetBytes(passwordBox.Password));
                if (sHA256.ComputeHash(singleHash).SequenceEqual(DoubleHashPassword))
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Incorrect password entered, please try again.", "AnoBIT", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    passwordBox.Clear();
                }
            }
        }
    }
}
