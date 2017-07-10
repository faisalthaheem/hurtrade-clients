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
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Collections.Generic;
using SharedData.events;

namespace HurtradeBackofficeClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Commands
        public DelegateCommand TradeBuyCommand { get; private set; }
        public DelegateCommand TradeSellCommand { get; private set; }
        public DelegateCommand CandlestickChartCommand { get; private set; }
        public DelegateCommand ApproveSelectedTradeCommand { get; private set; }
        public DelegateCommand RejectSelectedTradeCommand { get; private set; }
        public DelegateCommand BulkApproveSelectedTradeCommand { get; private set; }
        public DelegateCommand BulkRejectSelectedTradeCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        public DelegateCommand WindowClosed { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }

        public ObservableCollection<SharedData.poco.positions.TradePosition> PendingTrades = new ObservableCollection<SharedData.poco.positions.TradePosition>();
        public ListCollectionView PendingTradesCollectionView { get; private set; }

        public ObservableCollection<SharedData.poco.positions.TradePosition> OpenTrades = new ObservableCollection<SharedData.poco.positions.TradePosition>();
        public ListCollectionView OpenTradesCollectionView { get; private set; }
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

        #region properties
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
        public string CandleStickHeading
        {
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
        #endregion


        public MainWindowViewModel(MetroWindow mainWindow,IDialogCoordinator dialogCoordinator)
        {
            PendingTradesCollectionView = new ListCollectionView(PendingTrades);
            OpenTradesCollectionView = new ListCollectionView(OpenTrades);
            QuoteCollectionView = new ListCollectionView(Quotes);
            _mainWindow = mainWindow;
           _dialogCoord = dialogCoordinator;

            SetupCommands();
        }

        private void SetupCommands()
        {
            TradeBuyCommand = new DelegateCommand(ExecuteTradeBuyCommand);
            TradeSellCommand = new DelegateCommand(ExecuteTradeSellCommand);
            CandlestickChartCommand = new DelegateCommand(ExecuteCandlestickChartCommand);
            ApproveSelectedTradeCommand = new DelegateCommand(ExecuteApproveSelectedTradeCommand);
            RejectSelectedTradeCommand = new DelegateCommand(ExecuteRejectSelectedTradeCommand);
            BulkApproveSelectedTradeCommand = new DelegateCommand(ExecuteBulkApproveSelectedTradeCommand);
            BulkRejectSelectedTradeCommand = new DelegateCommand(ExecuteBulkRejectSelectedTradeCommand);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
            WindowClosed = new DelegateCommand(ExecuteWindowClosed);
        }

        private void ExecuteCandlestickChartCommand()
        {
            if (null == QuoteCollectionView.CurrentItem) return;

            string commodity = (QuoteCollectionView.CurrentItem as SharedData.poco.Quote).Name;
            ClientService.GetInstance().requestCandleStickChartData(commodity);
            CandleStickHeading = "Candle Stick Chart for " + commodity;
        }

        private void ExecuteBulkApproveSelectedTradeCommand()
        {
            lock (lockTrades)
            {
                foreach(var row in PendingTradesCollectionView)
                {
                    TradePosition position = row as TradePosition;
                    if (position.IsSelected)
                    {
                        DealerService.GetInstance().approveRejectOrder(position.ClientName, position.OrderId, DealerService.COMMAND_VERB_APPROVE);
                    }
                }
            }
        }

        private void ExecuteBulkRejectSelectedTradeCommand()
        {
            lock (lockTrades)
            {
                foreach (var row in PendingTradesCollectionView)
                {
                    TradePosition position = row as TradePosition;
                    if (position.IsSelected)
                    {
                        DealerService.GetInstance().approveRejectOrder(position.ClientName, position.OrderId, DealerService.COMMAND_VERB_REJECT);
                    }
                }
            }
        }

        private void ExecuteApproveSelectedTradeCommand()
        {
            lock (lockTrades)
            {
                TradePosition position = PendingTradesCollectionView.CurrentItem as TradePosition;
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
                TradePosition position = PendingTradesCollectionView.CurrentItem as TradePosition;
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
            ClientService.GetInstance().OnCandleStickDataEventHandler -= MainWindowViewModel_OnCandleStickDataEventHandler;

        }

        private void ExecuteWindowClosed()
        {
            Environment.Exit(0);
        }

        private async void ExecuteWindowLoaded()
        {
            ClientService.GetInstance().OnUpdateReceived += OnUpdateReceived;
            DealerService.GetInstance().OnOfficePositionsUpdateReceived += OnOfficePositionsUpdateReceived;
            ClientService.GetInstance().OnCandleStickDataEventHandler += MainWindowViewModel_OnCandleStickDataEventHandler;


            AuthService.GetInstance().OnGenericResponseReceived += MainWindow_OnGenericResponseReceived;

            LoginDialogData creds = await _dialogCoord.ShowLoginAsync(this, "Backoffice Login", "Login to continue using your account");
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

        private void MainWindowViewModel_OnCandleStickDataEventHandler(object sender, CandleStickDataEventArgs e)
        {
            //CandleStickCollection

            List<string> xLabels = new List<string>(e.Data.Count);
            ChartValues<OhlcPoint> candles = new ChartValues<OhlcPoint>();
            e.Data.Reverse();

            foreach (var cs in e.Data)
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

        private void OnOfficePositionsUpdateReceived(object sender, BackofficeUpdateEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                QuoteList quotes = e.OfficeUpdate.Quotes;
                Dictionary<string, List<TradePosition>> positions = e.OfficeUpdate.UserPositions;


                lock (lockTrades)
                {
                    if (positions.Count > 0)
                    {
                        int currentIndexPending = PendingTradesCollectionView.CurrentPosition;
                        int currentIndexOpen = OpenTradesCollectionView.CurrentPosition;
                        List<TradePosition> pendingWithSelectionPreserved = new List<TradePosition>();
                        
                        OpenTrades.Clear();

                        foreach (var row in positions)
                        {
                            foreach (var t in row.Value)
                            {
                                t.ClientName = row.Key;
                                t.CurrentPl *= -1.0M;

                                if (t.OrderState.Equals(TradePosition.ORDER_STATE_OPEN))
                                {
                                    OpenTrades.Add(t);
                                }
                                else
                                {
                                    if (!PendingTrades.Contains(t))
                                    {
                                        pendingWithSelectionPreserved.Add(t);
                                    }
                                    else
                                    {
                                        t.IsSelected = PendingTrades.First(x => x.Equals(t)).IsSelected;
                                        pendingWithSelectionPreserved.Add(t);
                                    }
                                }
                            }
                        }

                        PendingTrades.Clear();
                        PendingTrades.AddRange(pendingWithSelectionPreserved);
                        

                        if (PendingTradesCollectionView.Count >= currentIndexPending)
                        {
                            PendingTradesCollectionView.MoveCurrentToPosition(currentIndexPending);
                        }
                        

                        if (OpenTradesCollectionView.Count >= currentIndexOpen)
                        {
                            OpenTradesCollectionView.MoveCurrentToPosition(currentIndexOpen);
                        }
                        
                    }
                    else
                    {
                        OpenTrades.Clear();
                        PendingTrades.Clear();
                    }
                }
                PendingTradesCollectionView.Refresh();
                OpenTradesCollectionView.Refresh();
            });
        }

        private void OnUpdateReceived(object sender, SharedData.poco.updates.ClientUpdateEventArgs e)
        {

            App.Current.Dispatcher.Invoke((Action)delegate
            {
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
