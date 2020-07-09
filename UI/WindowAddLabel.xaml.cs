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
    /// Interaction logic for WindowAddLabel.xaml
    /// </summary>
    public partial class WindowAddLabel : Window
    {
        public string Note {
            get {
                return textBoxLabel.Text;
            }
        }

        public WindowAddLabel(string address, string note)
        {
            InitializeComponent();

            textBlockDescription.Text = address;
            textBoxLabel.Text = note;

            textBoxLabel.Focus();
            textBoxLabel.SelectAll();
        }

        private void buttonConfirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            //Close();
        }
    
        private void textBoxLabel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                buttonConfirm_Click(null, new RoutedEventArgs());
            }
        }
    }
}
