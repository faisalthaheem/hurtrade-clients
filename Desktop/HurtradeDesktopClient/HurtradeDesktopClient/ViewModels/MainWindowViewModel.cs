using HurtradeDesktopClient.Views;
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

namespace HurtradeDesktopClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Commands
        public DelegateCommand TradeBuyCommand { get; private set; }
        public DelegateCommand TradeSellCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }

        public ObservableCollection<SharedData.poco.positions.TradePosition> Trades = new ObservableCollection<SharedData.poco.positions.TradePosition>();
        public ListCollectionView TradesCollectionView { get; private set; }
        #endregion


        #region Private Members
        ProgressDialogController progressController;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;
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
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
        }

        private void ExecuteWindowClosing()
        {
            ClientService.GetInstance().OnUpdateReceived -= OnUpdateReceived;
        }

        private async void ExecuteWindowLoaded()
        {
            ClientService.GetInstance().OnUpdateReceived += OnUpdateReceived; ;

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

        private void OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
        {

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                lock (lockQuotes)
                {
                    int currentIndex = QuoteCollectionView.CurrentPosition;

                    Quotes.Clear();
                    Quotes.AddRange(e.ClientQuotes.Values);

                    QuoteCollectionView.MoveCurrentToPosition(currentIndex);
                    QuoteCollectionView.Refresh();
                }
                

                lock(lockTrades)
                {
                    int currentIndex = TradesCollectionView.CurrentPosition;

                    Trades.Clear();
                    Trades.AddRange(e.Positions.Values);

                    if (TradesCollectionView.Count >= currentIndex)
                    {
                        TradesCollectionView.MoveCurrentToPosition(currentIndex);
                    }
                    TradesCollectionView.Refresh();
                }
            });
        }

        private void MainWindow_OnGenericResponseReceived(object sender, SharedData.events.GenericResponseEventArgs e)
        {
            
            clientExchangeName = e.GenericResponse["clientExchangeName"];
            responseQueueName = e.GenericResponse["responseQueueName"];
            ClientService.GetInstance().init(username, password, clientExchangeName, responseQueueName);

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
            TradeOrderWindow tow = new TradeOrderWindow();
            TradeOrderWindowViewModel towContext = tow.DataContext as TradeOrderWindowViewModel;
            towContext.View = tow;
            towContext.OnTradeExecuted += TowContext_OnTradeExecuted;


            towContext.IsBuy = isBuy;
            towContext.TradingSymbol = (QuoteCollectionView.CurrentItem as SharedData.poco.Quote).Name;

            await ChildWindowManager.ShowChildWindowAsync(_mainWindow, tow, ChildWindowManager.OverlayFillBehavior.FullWindow);
        }

        private void ExecuteTradeSellCommand()
        {
            ExecuteTradeCommand(false);
        }
        private void ExecuteTradeBuyCommand()
        {
            ExecuteTradeCommand(true);
        }

        private void TowContext_OnTradeExecuted(TradeOrderWindowViewModel context)
        {
            context.View.Close();

            decimal requestedPrice = 0;
            Quote searchKey = new Quote() { Name = context.TradingSymbol };
            lock (lockQuotes)
            {
                int quoteIndex = Quotes.IndexOf(searchKey);

                if (context.IsBuy)
                {
                    requestedPrice = Quotes[quoteIndex].Ask;
                }
                else
                {
                    requestedPrice = Quotes[quoteIndex].Bid;
                }
            }

            TradeRequest request = new TradeRequest()
            {
                commodity = context.TradingSymbol,
                requestedLot = decimal.Parse(context.LotSize),
                requestTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm"),
                requestType = context.IsBuy ? TradeRequest.REQUEST_TYPE_BUY : TradeRequest.REQUEST_TYPE_SELL,
                tradeId = Guid.NewGuid(),
                requestedPrice = requestedPrice
            };

            ClientService.GetInstance().requestTrade(request);
            
        }
    }
}
