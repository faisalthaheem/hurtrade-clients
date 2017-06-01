using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SharedData.poco;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using HurtradeDesktopClient.Services;
using HurtradeDesktopClient.ViewModels;

namespace HurtradeDesktopClient.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        MainWindowViewModel mvvm = null;

        public MainWindow()
        {
            InitializeComponent();
            mvvm = new MainWindowViewModel(this, DialogCoordinator.Instance);
            DataContext = mvvm;
        }
    }
}
