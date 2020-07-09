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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string pathS = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnoBIT");
        string ConfigName = String.Empty;

        public MainWindow()
        {
            PropertyControl.CreateRootFolderIfNotExists();
            bool existingWallet = PropertyControl.ExistingWallets();

            if (existingWallet == false)
            {
                WindowCreateWallet WinCreateWallet = new WindowCreateWallet();
                WinCreateWallet.ShowDialog();

                InitializeComponent();
            }
            else
            {
                InitializeComponent();

                DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(pathS, "wallets"));
                FileInfo fi = di.GetFiles()[di.GetFiles().Length - 1];
                textBoxPATH.Text = System.IO.Path.Combine(pathS, "wallets", fi.ToString());
            }

            SetRecognizedWallets();

        }

        private void SetRecognizedWallets()
        {
            listBoxHOSTS.Items.Clear();

            if (Directory.GetFiles(System.IO.Path.Combine(pathS, "wallets"), "*.xml").Length != 0)
            {
                DirectoryInfo dci = new DirectoryInfo(System.IO.Path.Combine(pathS, "wallets"));
                FileInfo[] filez = dci.GetFiles();

                string textff = String.Empty;
                string nameoffile = String.Empty;

                foreach (FileInfo file in filez)
                {
                    nameoffile = file.ToString();
                    textff = System.IO.File.ReadAllText(System.IO.Path.Combine(pathS, "wallets", nameoffile));
                    listBoxHOSTS.Items.Add(nameoffile);
                }
                listBoxHOSTS.SelectedIndex = listBoxHOSTS.Items.Count - 1;
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofdWALLET = new OpenFileDialog();
            ofdWALLET.CheckFileExists = true;
            ofdWALLET.CheckPathExists = true;
            ofdWALLET.AddExtension = false;
            ofdWALLET.Multiselect = false;
            ofdWALLET.Filter = "AnoBIT Wallets (*.xml) | *.xml; |All Files (*.*) | *.*;";
            ofdWALLET.FilterIndex = 1;
            ofdWALLET.InitialDirectory = System.IO.Path.Combine(pathS, "wallets");
            if (ofdWALLET.ShowDialog() == true)
            {
                textBoxPATH.Text = ofdWALLET.FileName;
            }
         
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            WindowCreateWallet WinCreateWallet = new WindowCreateWallet();
            WinCreateWallet.ShowDialog();
            SetRecognizedWallets();
        }

        private void buttonOW_Click(object sender, RoutedEventArgs e)
        {
            WindowWallet WinWallet = new WindowWallet(textBoxPATH.Text, System.IO.Path.GetFileName(textBoxPATH.Text));
            this.Close();
            WinWallet.Show();
        }

        private void listBoxHOSTS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try {
                textBlockSERVER.Text = listBoxHOSTS.SelectedItem.ToString();
                textBoxPATH.Text = System.IO.Path.Combine(pathS, "wallets", listBoxHOSTS.SelectedItem.ToString());
            } catch { }
        }  
    }
}
