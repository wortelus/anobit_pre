using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace AnoBIT_Wallet
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            PropertyControl.CreateRootFolderIfNotExists();

            MainWindow window = new AnoBIT_Wallet.MainWindow();
            window.Show();
        }
    }
}
