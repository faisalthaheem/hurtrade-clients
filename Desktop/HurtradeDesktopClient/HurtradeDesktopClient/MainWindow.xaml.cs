using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using HurtradeDesktopClient.Services;

namespace HurtradeDesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        ProgressDialogController progressController;
        private string officeExchangeName = string.Empty;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;
        private string username = string.Empty;
        private string password = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

            ClientService.GetInstance().OnUpdateReceived += MainWindow_OnUpdateReceived; ;

            AuthService.GetInstance().OnGenericResponseReceived += MainWindow_OnGenericResponseReceived;

            LoginDialogData creds = await this.ShowLoginAsync("Authentication", "Login to continue using your account");
            if (creds != null && !string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
            {
                progressController = await this.ShowProgressAsync("Please wait...", "Authenticating with server.");
                progressController.SetIndeterminate();

                if (!AuthService.GetInstance().init(creds.Username, creds.Password)
                    ||
                    !AuthService.GetInstance().ResolveUserEndpoints()
                    )
                {
                    await progressController.CloseAsync();

                    await this.ShowMessageAsync("Authentication", "Incorrect username/password.");
                    App.Current.Shutdown();
                }
                else
                {
                    username = creds.Username;
                    password = creds.Password;
                }
            }
            else
            {
                await this.ShowMessageAsync("Authentication", "Credentials are needed to continue.");
                App.Current.Shutdown();
            }
            
        }

        private void MainWindow_OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
        {
            
        }

        private void MainWindow_OnGenericResponseReceived(object sender, SharedData.events.GenericResponseEventArgs e)
        {
            officeExchangeName = e.GenericResponse["officeExchangeName"];
            clientExchangeName = e.GenericResponse["clientExchangeName"];
            responseQueueName = e.GenericResponse["responseQueueName"];
            ClientService.GetInstance().init(username, password, officeExchangeName, clientExchangeName, responseQueueName);

            System.Windows.Threading.
                  Dispatcher.CurrentDispatcher.Invoke(async() => {

                      await progressController.CloseAsync();

                      MetroDialogSettings set = new MetroDialogSettings();
                      await this.ShowMessageAsync("Authentication", "Login successful.");

                      //we are done here
                      AuthService.Cleanup();

                  });
        }
        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
