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
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Math.EC;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Configuration;
using System.Xml;

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for WindowWallet.xaml
    /// </summary>
    public partial class WindowWallet : Window
    {

        public const ulong Divider = 100000000;
        ulong MinimumPocket = 1000000;

        private string MasterNode;
        private UInt16 NetPort;
        private byte[] MasterNodePublicKey;
        private ushort MasterNodeReplayProtection;

        Config WalletObject;
        private string WalletPath;
        private string WalletName;

        bool canCommunicate;
        public bool CanCommunicate {
            get {
                return canCommunicate;
            }
            set {
                canCommunicate = value;
                if (value) {
                    statusBarItemNetworkStatus.Content = "Connected";
                } else {
                    statusBarItemNetworkStatus.Content = "Disconnected";
                }
            }
        }

        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public WindowWallet(string walletPath, string walletName)
        {
            InitializeComponent();

            string xmlString = string.Empty;
            try {
                xmlString = File.ReadAllText(walletPath);
                WalletObject = WalletConfig.DeserializeA(xmlString);
            } catch (XmlException ex) {
                MessageBox.Show("There was an error during deserialization: " + ex.Message);
                Close();
            } catch (Exception ex) {
                MessageBox.Show("There was an error during opening or reading the wallet file: " + ex.Message);
                Close();
            }

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            if (!int.TryParse(ConfigurationManager.AppSettings["UINotifyTimeout"], out int uiNotifyTimeout)) {
                uiNotifyTimeout = 2;
            }
            dispatcherTimer.Interval = new TimeSpan(0, 0, uiNotifyTimeout);

            //textBoxSendFee.Text = Transaction.TxFee.ToString();

            WalletPath = walletPath;
            WalletName = walletName;
            Title = "AnoBIT Wallet - " + WalletName;

            if (ulong.TryParse(ConfigurationManager.AppSettings["PocketingThreshold"], out ulong pocketingThreshold)) {
                MinimumPocket = pocketingThreshold;
            }
            textBlockMinimumAmountPocket.Text = "Minimum Amount for Pocketing: " + decimal.Divide(MinimumPocket, Divider);

            LoadConnectionData();
            SetActiveAddress();
            SetNormalAddresses();
        }

        private void LoadConnectionData() {
            NetPort = Properties.Settings.Default.Port;
            MasterNode = Properties.Settings.Default.IP;
            MasterNodePublicKey = Properties.Settings.Default.ServerPublicKey.GetHexBytes();
            MasterNodeReplayProtection = Properties.Settings.Default.ReplayProtection;
            if (MasterNodePublicKey.Length == 0 || MasterNodeReplayProtection == 0 || string.IsNullOrWhiteSpace(MasterNode)) {
                canCommunicate = false;
            }
            textBoxNetworkPort.Text = NetPort.ToString();
            //textBoxNetworkMaxConnections.Text = MasterNode;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            barItemUpdate.Content = "";
            dispatcherTimer.Stop();
        }

        private void SetActiveAddress()
        {
            AddressConfig activeAddress = WalletObject.GetActiveAddress();
            string base58Address = AnoBITCrypto.RIPEMD160ToAddress(activeAddress.RIPEMD160);
            textBlockActiveAddress.Text = base58Address;
            textBoxActiveAddress.Text = base58Address;
        }

        public void SetNormalAddresses()
        {
            listViewNormalAddresses.Items.Clear();
            foreach (AddressConfig addressConfig in WalletObject.Addresses) {
                listViewNormalAddresses.Items.Add(addressConfig);
            }
        }  

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public virtual void AddLogContent(string s)
        {
            this.Dispatcher.Invoke(() =>
            {
                textBoxNetworkLog.AppendText(Environment.NewLine + s);
            });
        }

        private void textBoxNetworkLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            textBoxNetworkLog.CaretIndex = textBoxNetworkLog.Text.Length;
            textBoxNetworkLog.ScrollToEnd();
        }

        private void tabItemNetwork_TouchDown(object sender, TouchEventArgs e)
        {
            textBoxNetworkCommandLine.Focus();
        }

        private void menuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            WindowAbout WinAbout = new WindowAbout();
            WinAbout.ShowDialog();
        }

        private void textBlockActiveAddress_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dispatcherTimer.Stop();
            barItemUpdate.Content = "Copied: " + WalletObject.GetActiveAddress().Base58Address;
            dispatcherTimer.Start();
            System.Windows.Clipboard.SetText(WalletObject.GetActiveAddress().Base58Address);
        }

        private void MenuItemUnlock_Click(object sender, RoutedEventArgs e)
        {
            WindowEnterPassword windowEnterPassword = new WindowEnterPassword(WalletObject.DoubleHashPassword);
            if (windowEnterPassword.ShowDialog() == true)
            {
                try {
                    WalletObject.DecryptWallet(new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(windowEnterPassword.rawPassword)));
                } catch (Exception ex){
                    MessageBox.Show("There was an error during decoding a wallet. Check the xml file or retrieve wallet from seed/backup\r\n" + ex.Message);
                }
                menuItemLock.Header = "Unlocked";
                menuItemLock.Background = Brushes.LightSkyBlue;
                //TODO
            }
        }

        private void MenuItemLock_Click(object sender, RoutedEventArgs e)
        {
            menuItemLock.Header = "Locked";
            menuItemLock.Background = Brushes.IndianRed;
        }

        private void listBoxBlockchainTransactions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateBlockchainList();
        }

        private void buttonChangeActiveAddress_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("The current instance will be closed. Do you want to proceed?", "AnoBIT", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
            {
                if (AnoBITCrypto.ValidateAddress(textBoxActiveAddress.Text, 23))
                {
                    //TODO: Find the active adress and set it
                }
            }
        }

        private void UpdateBlockchainList()
        {
            
        }

        private void listBoxBlockchainTransactions_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateBlockchainList();
        }

        private void MenuItemPocket_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void textBoxBlockchainInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //buttonBlockchainInput_Click(null, null);
            }
        }

        private void menuItemOpenWallet_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private void menuItemCloseWallet_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuItemCloseAnoBIT_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void menuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.github.com/wortelus/anobit");
        }

        private void menuItemEditAddress_Click(object sender, RoutedEventArgs e) {
            //MessageBox.Show(listViewNormalAddresses.SelectedItem.ToString());
            if (listViewNormalAddresses.SelectedIndex == -1) {
                return;
            }
            int selectedIndex = listViewNormalAddresses.SelectedIndex;
            WindowAddLabel windowAddLabel = new WindowAddLabel(
                (string)listViewNormalAddresses.SelectedItem.GetType().GetProperty("Base58Address").GetValue(listViewNormalAddresses.SelectedItem),
                (string)listViewNormalAddresses.SelectedItem.GetType().GetProperty("Desc").GetValue(listViewNormalAddresses.SelectedItem));
            if (windowAddLabel.ShowDialog() == true) {
                WalletObject.Addresses[selectedIndex].Desc = windowAddLabel.Note;
                SetNormalAddresses();
                SaveXmlWallet();
            }
        }

        private void SaveXmlWallet() {
            string xmlOutput = WalletConfig.Serialize(WalletObject);
            try {
                File.WriteAllText(WalletPath, xmlOutput);
            } catch (Exception ex) {
                MessageBox.Show(string.Format("There was an exception during saving a Xml wallet file {0}. {1}", WalletPath, ex.Message));
            }
        }

        private void menuItemShowAddressProperties_Click(object sender, RoutedEventArgs e) {
            if (listViewNormalAddresses.SelectedIndex == -1) {
                return;
            }
            WindowShowPrivateKey windowShowPrivateKey = new WindowShowPrivateKey(WalletObject.Addresses[listViewNormalAddresses.SelectedIndex]);
            windowShowPrivateKey.ShowDialog();
        }
    }
}
