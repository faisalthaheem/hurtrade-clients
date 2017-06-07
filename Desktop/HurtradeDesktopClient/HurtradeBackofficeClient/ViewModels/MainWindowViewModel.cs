using HurtradeBackofficeClient.Views;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Prism.Commands;
using Prism.Mvvm;
using SharedData.poco;
using SharedData.poco.trade;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow.Utils;
using SharedData.poco.positions;
using SharedData.Services;
using BackofficeSharedData.Services;
using BackofficeSharedData.poco.updates;
using System.Linq;

namespace HurtradeBackofficeClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Commands
        public DelegateCommand TradeBuyCommand { get; private set; }
        public DelegateCommand TradeSellCommand { get; private set; }
        public DelegateCommand ApproveSelectedTradeCommand { get; private set; }
        public DelegateCommand RejectSelectedTradeCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }

        public ObservableCollection<SharedData.poco.positions.Position> Trades = new ObservableCollection<SharedData.poco.positions.Position>();
        public ListCollectionView TradesCollectionView { get; private set; }
        #endregion


        #region Private Members
        ProgressDialogController progressController;
        private string officeExchangeName = string.Empty;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;
        private string officeDealerOutQName = string.Empty;
        private string officeDealerInQName = string.Empty;
        private string username = string.Empty;
        private string password = string.Empty;
        
        private MetroWindow _mainWindow;
        private IDialogCoordinator _dialogCoord;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region locks
        private Object lockQuotes = new Object();
        private Object lockTrades = new Object();
        #endregion


        public MainWindowViewModel(MetroWindow mainWindow,IDialogCoordinator dialogCoordinator)
        {
            TradesCollectionView = new ListCollectionView(Trades);
            QuoteCollectionView = new ListCollectionView(Quotes);
            _mainWindow = mainWindow;
           _dialogCoord = dialogCoordinator;

            SetupCommands();
        }

        private void SetupCommands()
        {
            TradeBuyCommand = new DelegateCommand(ExecuteTradeBuyCommand);
            TradeSellCommand = new DelegateCommand(ExecuteTradeSellCommand);
            ApproveSelectedTradeCommand = new DelegateCommand(ExecuteApproveSelectedTradeCommand);
            RejectSelectedTradeCommand = new DelegateCommand(ExecuteRejectSelectedTradeCommand);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
        }

        private void ExecuteApproveSelectedTradeCommand()
        {
            lock (lockTrades)
            {
                SharedData.poco.positions.Position position = TradesCollectionView.CurrentItem as SharedData.poco.positions.Position;
                if(position == null)
                {
                    //todo show error
                    return;
                }
                DealerService.GetInstance().approveRejectOrder(position.ClientName, position.OrderId, DealerService.COMMAND_VERB_APPROVE);
            }
        }

        private void ExecuteRejectSelectedTradeCommand()
        {
            lock (lockTrades)
            {
                SharedData.poco.positions.Position position = TradesCollectionView.CurrentItem as SharedData.poco.positions.Position;
                if (position == null)
                {
                    //todo show error
                    return;
                }
                DealerService.GetInstance().approveRejectOrder(position.ClientName, position.OrderId, DealerService.COMMAND_VERB_REJECT);
            }
        }

        private void ExecuteWindowClosing()
        {
            ClientService.GetInstance().OnUpdateReceived -= OnUpdateReceived;
            DealerService.GetInstance().OnOfficePositionsUpdateReceived -= OnOfficePositionsUpdateReceived;
        }

        private async void ExecuteWindowLoaded()
        {
            ClientService.GetInstance().OnUpdateReceived += OnUpdateReceived;
            DealerService.GetInstance().OnOfficePositionsUpdateReceived += OnOfficePositionsUpdateReceived;
            
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

        private void OnOfficePositionsUpdateReceived(object sender, OfficePositionsUpdateEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                lock (lockTrades)
                {
                    int currentIndex = TradesCollectionView.CurrentPosition;

                    foreach (var row in e.OfficePositionsUpdate)
                    {
                        var values = row.Value;
                        var notPresent = Trades.Where(p => !values.Any(p2 => p2.Equals(p)));

                        int idx = -1;
                        foreach (var t in row.Value)
                        {
                            idx = Trades.IndexOf(t);
                            t.ClientName = row.Key;
                            //for the back office client's profit equates to loss
                            t.CurrentPl *= -1;
                            if (idx >= 0)
                            {
                                Trades[idx] = t;
                            }
                            else
                            {
                                Trades.Add(t);
                            }
                        }
                    }
                    
                    //in case we removed items and the list shortened
                    if(currentIndex > TradesCollectionView.Count)
                    {
                        currentIndex = TradesCollectionView.Count;
                    }
                    TradesCollectionView.MoveCurrentToPosition(currentIndex);
                    TradesCollectionView.Refresh();
                }
            });
        }

        private void OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
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
            officeDealerInQName = e.GenericResponse["officeDealerInQName"];
            officeDealerOutQName = e.GenericResponse["officeDealerOutQName"];

            ClientService.GetInstance().init(username, password, clientExchangeName, responseQueueName);
            DealerService.GetInstance().init(username, password, officeExchangeName, officeDealerOutQName, officeDealerInQName);

            System.Windows.Threading.
                  Dispatcher.CurrentDispatcher.Invoke(async () => {

                      await progressController.CloseAsync();

                      MetroDialogSettings set = new MetroDialogSettings();
                      await _dialogCoord.ShowMessageAsync(this,"Authentication", "Login successful.");

                      //we are done here
                      AuthService.Cleanup();
                  });
        }
        
        private async void ExecuteTradeCommand(bool isBuy)
        {
            //await ChildWindowManager.ShowChildWindowAsync(_mainWindow, tow, ChildWindowManager.OverlayFillBehavior.FullWindow);
        }

        private void ExecuteTradeSellCommand()
        {
            ExecuteTradeCommand(false);
        }
        private void ExecuteTradeBuyCommand()
        {
            ExecuteTradeCommand(true);
        }

        
    }
}
