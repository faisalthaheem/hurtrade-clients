using MahApps.Metro.Controls.Dialogs;
using HurtradeBackofficeClient.ViewModels;

namespace HurtradeBackofficeClient.Views
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
