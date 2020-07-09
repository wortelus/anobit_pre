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
    /// Interaction logic for WindowAddContact.xaml
    /// </summary>
    public partial class WindowAddContact : Window
    {
        private string WalletPath;
        private string WalletFile;
        private string OldName;
        private string OldAddress;
        private string OldInfo;
        private bool IsEdited;
        private string Color;

        public WindowAddContact()
        {
            InitializeComponent();
        }

        private void buttonConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxName.Text.IndexOf(";") == -1 || textBoxName.Text.IndexOf("/") == -1 || textBoxName.Text.IndexOf("<") == -1 || textBoxName.Text.IndexOf(">") == -1 || textBoxAddress.Text.IndexOf(";") == -1 || textBoxAddress.Text.IndexOf("/") == -1 || textBoxAddress.Text.IndexOf("<") == -1 || textBoxAddress.Text.IndexOf(">") == -1)
            {
                if (IsEdited)
                {
                    WalletFile = File.ReadAllText(WalletPath);
                    WalletFile = WalletFile.Replace(OldInfo, "");
                    WalletFile = WalletFile + Environment.NewLine + "<contact>" + Color + ";" + textBoxName.Text + ";" + textBoxAddress.Text + "</contact>";
                    File.WriteAllText(WalletPath, WalletFile);
                    Close();
                }
                else
                {
                    File.AppendAllText(WalletPath,Environment.NewLine + "<contact>" + Color + ";" + textBoxName.Text + ";" + textBoxAddress.Text + "</contact>");
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Name or address can not contain character <, >, /. Enter inputs without those characters.", "AnoBIT", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        public void SetInfo(string path, string name, string add, string raw, bool editing)
        {
            WalletPath = path;
            textBoxName.Text= name;
            textBoxAddress.Text = add;
            IsEdited = editing;

            if (editing == true)
            {
                OldAddress = add;
                OldName = name;
                OldInfo = raw;
            }
        }

        private void radioButtonWhite_Checked(object sender, RoutedEventArgs e)
        {
            Color = "WH";
        }

        private void radioButtonRed_Checked(object sender, RoutedEventArgs e)
        {
            Color = "RE";
        }

        private void radioButtonOrange_Checked(object sender, RoutedEventArgs e)
        {
            Color = "OR";
        }

        private void radioButtonYellow_Checked(object sender, RoutedEventArgs e)
        {
            Color = "YE";
        }

        private void radioButtonGreen_Checked(object sender, RoutedEventArgs e)
        {
            Color = "GR";
        }

        private void radioButtonTurqoise_Checked(object sender, RoutedEventArgs e)
        {
            Color = "TU";
        }

        private void radioButtonBlue_Checked(object sender, RoutedEventArgs e)
        {
            Color = "BL";
        }

        private void radioButtonPink_Checked(object sender, RoutedEventArgs e)
        {
            Color = "PI";
        }

        private void radioButtonPurple_Checked(object sender, RoutedEventArgs e)
        {
            Color = "PU";
        }

        private void radioButtonGrey_Checked(object sender, RoutedEventArgs e)
        {
            Color = "GE";
        }
    }
}
