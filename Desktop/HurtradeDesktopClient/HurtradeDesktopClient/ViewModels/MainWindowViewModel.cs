using HurtradeDesktopClient.Services;
using MahApps.Metro.Controls.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using SharedData.poco;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace HurtradeDesktopClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Commands
        public DelegateCommand TradeBuyCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }
        #endregion


        #region Private Members
        ProgressDialogController progressController;
        private string officeExchangeName = string.Empty;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;
        private string username = string.Empty;
        private string password = string.Empty;
        private Object lockQuotes = new Object();
        private IDialogCoordinator _dialogCoord;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion


        public MainWindowViewModel(IDialogCoordinator dialogCoordinator)
        {
            QuoteCollectionView = new ListCollectionView(Quotes);
            _dialogCoord = dialogCoordinator;

            SetupCommands();
        }

        private void SetupCommands()
        {
            TradeBuyCommand = new DelegateCommand(ExecuteTradeBuyCommand);
            WindowLoaded = new DelegateCommand(Window_Loaded);
        }

        private async void Window_Loaded()
        {
            ClientService.GetInstance().OnUpdateReceived += MainWindow_OnUpdateReceived; ;

            AuthService.GetInstance().OnGenericResponseReceived += MainWindow_OnGenericResponseReceived;

            LoginDialogData creds = await _dialogCoord.ShowLoginAsync(this, "Authentication", "Login to continue using your account");
            if (creds != null && !string.IsNullOrEmpty(creds.Username) && !string.IsNullOrEmpty(creds.Password))
            {
                progressController = await _dialogCoord.ShowProgressAsync(this,"Please wait...", "Authenticating with server.");
                progressController.SetIndeterminate();

                if (!AuthService.GetInstance().init(creds.Username, creds.Password)
                    ||
                    !AuthService.GetInstance().ResolveUserEndpoints()
                    )
                {
                    await progressController.CloseAsync();

                    await _dialogCoord.ShowMessageAsync(this,"Authentication", "Incorrect username/password.");
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
                await _dialogCoord.ShowMessageAsync(this,"Authentication", "Credentials are needed to continue.");
                App.Current.Shutdown();
            }

        }

        private void MainWindow_OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
        {

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                lock (lockQuotes)
                {
                    int currentIndex = QuoteCollectionView.CurrentPosition;

                    int idx = -1;
                    foreach (var q in e.ClientQuotes.Values)
                    {
                        //Quotes.Add(q);
                        idx = Quotes.IndexOf(q);
                        if (idx >= 0)
                        {
                            Quotes[idx] = q;
                        }
                        else
                        {
                            Quotes.Add(q);
                        }

                        log.Info(q.Name + " B: " + q.Bid + " A: " + q.Ask);
                        Console.WriteLine(q.Name + " B: " + q.Bid + " A: " + q.Ask);
                    }

                    QuoteCollectionView.MoveCurrentToPosition(currentIndex);
                    QuoteCollectionView.Refresh();
                }
            });
        }

        private void MainWindow_OnGenericResponseReceived(object sender, SharedData.events.GenericResponseEventArgs e)
        {
            officeExchangeName = e.GenericResponse["officeExchangeName"];
            clientExchangeName = e.GenericResponse["clientExchangeName"];
            responseQueueName = e.GenericResponse["responseQueueName"];
            ClientService.GetInstance().init(username, password, officeExchangeName, clientExchangeName, responseQueueName);

            System.Windows.Threading.
                  Dispatcher.CurrentDispatcher.Invoke(async () => {

                      await progressController.CloseAsync();

                      MetroDialogSettings set = new MetroDialogSettings();
                      await _dialogCoord.ShowMessageAsync(this,"Authentication", "Login successful.");

                      //we are done here
                      AuthService.Cleanup();
                  });
        }

        private Window findParentWindow(DependencyObject o)
        {
            var parent = VisualTreeHelper.GetParent(o);
            while (!(parent is Window))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as Window;
        }

        private void ExecuteTradeBuyCommand()
        {

        }

    }
}
