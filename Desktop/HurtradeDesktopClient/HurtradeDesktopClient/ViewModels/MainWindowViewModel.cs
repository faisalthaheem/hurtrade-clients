using HurtradeDesktopClient.Views;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Prism.Commands;
using Prism.Mvvm;
using SharedData.poco;
using SharedData.poco.trade;
using System;
using System.Collections.ObjectModel;
using System.Windows.Data;
using MahApps.Metro.Controls;
using SharedData.poco.positions;
using SharedData.Services;
using SharedData.events;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Collections.Generic;
using System.Linq;

namespace HurtradeDesktopClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Commands
        public DelegateCommand TradeCommand { get; private set; }
        public DelegateCommand CandlestickChartCommand { get; private set; }
        public DelegateCommand TradeCloseCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        public DelegateCommand WindowClosed { get; private set; }
        public DelegateCommand ShowLogsCommand { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }

        public ObservableCollection<TradePosition> Trades = new ObservableCollection<TradePosition>();
        public ListCollectionView TradesCollectionView { get; private set; }

        public ObservableCollection<TradePosition> NetTrades = new ObservableCollection<TradePosition>();
        public ListCollectionView NetTradesCollectionView { get; private set; }

        public ObservableCollection<string> _notificationLogsList = new ObservableCollection<string>();
        public ListCollectionView NotificationLogsListCollectionView { get; private set; }

        private bool _LogsFlyoutOpen = false;
        public bool LogsFlyoutOpen
        {
            get { return _LogsFlyoutOpen; }
            set
            {
                SetProperty(ref _LogsFlyoutOpen, value);
            }
        }

        private bool _NotificationsFlyoutOpen = false;
        public bool NotificationsFlyoutOpen
        {
            get { return _NotificationsFlyoutOpen; }
            set
            {
                SetProperty(ref _NotificationsFlyoutOpen, value);
            }
        }

        private string[] _candleStickXLabels = null;
        public string[] CandleStickXLabels
        {
            get
            {
                return _candleStickXLabels;
            }
            set
            {
                SetProperty(ref _candleStickXLabels, value);
            }
        }

        private string _CandleStickHeading;
        public string CandleStickHeading {
            get
            {
                return _CandleStickHeading;
            }
            set
            {
                SetProperty(ref _CandleStickHeading, value);
            }
        }

        public SeriesCollection _candleStickCollection = null;
        public SeriesCollection CandleStickCollection
        {
            get
            {
                return _candleStickCollection;
            }
            set
            {
                SetProperty(ref _candleStickCollection, value);
            }
        }

        private decimal _availableCash = 0;
        public decimal AvailableCash {
            get
            {
                return _availableCash;
            }
            set
            {
                SetProperty(ref _availableCash, value);
            }
        }

        private decimal _floatingPL = 0;
        public decimal FloatingPL
        {
            get
            {
                return _floatingPL;
            }
            set
            {
                SetProperty(ref _floatingPL, value);
            }
        }

        private decimal _usedMargin = 0;
        public decimal UsedMargin
        {
            get
            {
                return _usedMargin;
            }
            set
            {
                SetProperty(ref _usedMargin, value);
            }
        }

        private decimal _usableMargin = 0;
        public decimal UsableMargin
        {
            get
            {
                return _usableMargin;
            }
            set
            {
                SetProperty(ref _usableMargin, value);
            }
        }


        private decimal _equity;
        public decimal Equity
        {
            get
            {
                return _equity;
            }
            set
            {
                SetProperty(ref _equity, value);
            }
        }

        

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
            NetTradesCollectionView = new ListCollectionView(NetTrades);
            QuoteCollectionView = new ListCollectionView(Quotes);
            NotificationLogsListCollectionView = new ListCollectionView(_notificationLogsList);

            _mainWindow = mainWindow;
           _dialogCoord = dialogCoordinator;

            SetupCommands();
        }

        private void SetupCommands()
        {
            TradeCommand = new DelegateCommand(ExecuteTradeCommand);
            CandlestickChartCommand = new DelegateCommand(ExecuteCandlestickChartCommand);
            TradeCloseCommand = new DelegateCommand(ExecuteTradeCloseCommand);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
            WindowClosed = new DelegateCommand(ExecuteWindowClosed);
            ShowLogsCommand = new DelegateCommand(ExecuteShowLogsCommand);
        }

        private void ExecuteShowLogsCommand()
        {
            LogsFlyoutOpen = true;
        }

        private void ExecuteCandlestickChartCommand()
        {
            if (null == QuoteCollectionView.CurrentItem) return;

            string commodity = (QuoteCollectionView.CurrentItem as SharedData.poco.Quote).Name;
            ClientService.GetInstance().requestCandleStickChartData(commodity);
            CandleStickHeading = "Candlestick Chart for " + commodity;
        }

        private void ExecuteWindowClosing()
        {
            ClientService.GetInstance().OnUpdateReceived -= OnUpdateReceived;
            ClientService.GetInstance().OnAccountStatusEventReceived -= MainWindowViewModel_OnAccountStatusEventReceived;
            ClientService.GetInstance().OnCandleStickDataEventHandler -= MainWindowViewModel_OnCandleStickDataEventHandler;

            ClientService.Cleanup();
        }

        private void ExecuteWindowClosed()
        {
            Environment.Exit(0);
        }


        private async void ExecuteWindowLoaded()
        {
            ClientService.GetInstance().OnUpdateReceived += OnUpdateReceived;
            ClientService.GetInstance().OnAccountStatusEventReceived += MainWindowViewModel_OnAccountStatusEventReceived;
            ClientService.GetInstance().OnCandleStickDataEventHandler += MainWindowViewModel_OnCandleStickDataEventHandler;
            ClientService.GetInstance().OnOrderUpdateReceived += MainWindowViewModel_OnOrderUpdateReceived;
            ClientService.GetInstance().OnNotificationReceived += MainWindowViewModel_OnNotificationReceived;

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

        private void MainWindowViewModel_OnNotificationReceived(string notification)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _notificationLogsList.Insert(0, notification);
                NotificationLogsListCollectionView.Refresh();
            });
        }

        private void MainWindowViewModel_OnOrderUpdateReceived(object sender, GenericResponseEventArgs e)
        {
            if (e.GenericResponse.ContainsKey("requote"))
            {
                App.Current.Dispatcher.Invoke((Action)async delegate
                {
                    RequoteReviewWindow rrw = new RequoteReviewWindow();
                    RequoteReviewWindowViewModel rrwm = rrw.DataContext as RequoteReviewWindowViewModel;

                    Guid orderId = Guid.Parse(e.GenericResponse["orderid"]);
                    TradePosition p = null;
                    lock (lockTrades)
                    {
                        p = Trades.FirstOrDefault(x => orderId.CompareTo(x.OrderId) == 0);
                    }

                    if (p != null)
                    {
                        rrwm.TradePosition = p;
                        if (e.GenericResponse["pendingclose"].Length > 0)
                        {
                            rrwm.RequestedPrice = p.ClosePrice;
                        }
                        else
                        {
                            rrwm.RequestedPrice = p.OpenPrice;
                        }
                        rrwm.RequotedPrice = decimal.Parse(e.GenericResponse["requoteprice"]);
                        rrwm.OrderId = p.Friendlyorderid;
                        rrwm.RemainingTime = int.Parse(e.GenericResponse["timeout"]);
                        rrwm.OnRequoteReviewed += Rrwm_OnRequoteReviewed;

                        rrwm.View = rrw;

                        await ChildWindowManager.ShowChildWindowAsync(_mainWindow, rrw, ChildWindowManager.OverlayFillBehavior.FullWindow);
                    }
                    else
                    {
                        //todo: log here
                    }
                });

            }
        }

        private void Rrwm_OnRequoteReviewed(RequoteReviewWindowViewModel model, string action)
        {
            model.View.Close();

            if(action == "accept")
            {
                ClientService.GetInstance().acceptRequote(model.TradePosition.OrderId);
            }
        }

        private void MainWindowViewModel_OnCandleStickDataEventHandler(object sender, CandleStickDataEventArgs e)
        {
            //CandleStickCollection

            List<string> xLabels = new List<string>(e.Data.Count);
            ChartValues<OhlcPoint> candles = new ChartValues<OhlcPoint>();
            e.Data.Reverse();

            foreach(var cs in e.Data)
            {
                candles.Add(new OhlcPoint(cs.Open, cs.Highest, cs.Lowest, cs.Close));
                xLabels.Add(cs.SampleFor.ToShortDateString() + " " + cs.SampleFor.ToShortTimeString());
            }
            
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                CandleStickCollection = new SeriesCollection { new CandleSeries { Values = candles } };
                CandleStickXLabels = xLabels.ToArray();
            });
        }

        private void MainWindowViewModel_OnAccountStatusEventReceived(object sender, SharedData.events.GenericResponseEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate {
                AvailableCash = decimal.Parse(e.GenericResponse["availableCash"]);
                FloatingPL = decimal.Parse(e.GenericResponse["floating"]);
                UsedMargin = decimal.Parse(e.GenericResponse["usedMargin"]);
                UsableMargin = decimal.Parse(e.GenericResponse["usableMargin"]);
                Equity = decimal.Parse(e.GenericResponse["equity"]);
            });
        }

        private void OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
        {

            App.Current.Dispatcher.Invoke((Action)delegate
            {
                //quotes
                if (null != e.ClientQuotes && e.ClientQuotes.Count > 0)
                {
                    lock (lockQuotes)
                    {
                        int currentIndex = QuoteCollectionView.CurrentPosition;

                        Quotes.Clear();
                        Quotes.AddRange(e.ClientQuotes.Values);

                        QuoteCollectionView.MoveCurrentToPosition(currentIndex);
                        QuoteCollectionView.Refresh();
                    }
                }

                //current trades
                if (null != e.Positions && e.Positions.Count > 0)
                {
                    lock (lockTrades)
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
                }
                else
                {
                    Trades.Clear();
                    TradesCollectionView.Refresh();
                }

                //net trades
                if (null != e.NetPosition && e.NetPosition.Count > 0)
                {
                    int currentIndex = NetTradesCollectionView.CurrentPosition;

                    NetTrades.Clear();
                    NetTrades.AddRange(e.NetPosition.Values);

                    if (NetTradesCollectionView.Count >= currentIndex)
                    {
                        NetTradesCollectionView.MoveCurrentToPosition(currentIndex);
                    }
                    NetTradesCollectionView.Refresh();
                }
                else
                {
                    NetTrades.Clear();
                    NetTradesCollectionView.Refresh();
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

        private void ExecuteTradeCloseCommand()
        {
            lock(lockTrades)
            {
                TradePosition currPosition = TradesCollectionView.CurrentItem as TradePosition;
                ClientService.GetInstance().requestTradeClosure(currPosition.OrderId);
            }
        }
        
        private async void ExecuteTradeCommand()
        {

            TradeOrderWindow tow = new TradeOrderWindow();
            TradeOrderWindowViewModel towContext = tow.DataContext as TradeOrderWindowViewModel;
            towContext.View = tow;
            towContext.OnTradeExecuted += TowContext_OnTradeExecuted;

            var quote = QuoteCollectionView.CurrentItem as SharedData.poco.Quote;
            if (null == quote) { return; }
            towContext.TradingSymbol = quote.Name;

            await ChildWindowManager.ShowChildWindowAsync(_mainWindow, tow, ChildWindowManager.OverlayFillBehavior.WindowContent);
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
