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
        
        public DelegateCommand CandlestickChartCommand { get; private set; }
        public DelegateCommand ApproveSelectedTradeCommand { get; private set; }
        public DelegateCommand RejectSelectedTradeCommand { get; private set; }
        public DelegateCommand BulkApproveSelectedTradeCommand { get; private set; }
        public DelegateCommand BulkRejectSelectedTradeCommand { get; private set; }
        
        public DelegateCommand<object> RequoteSetPriceCommand { get; private set; }
        public DelegateCommand RequoteSelectedOrders { get; private set; }

        public DelegateCommand NewCoverPositionCommand { get; private set; }
        public DelegateCommand EditSelectedCoverPositionCommand { get; private set; }
        public DelegateCommand CloseSelectedCoverPositionCommand { get; private set; }

        public DelegateCommand ShowLogsCommand { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        public DelegateCommand WindowClosed { get; private set; }
        #endregion

        #region Properties
        public ObservableCollection<Quote> Quotes = new ObservableCollection<Quote>();
        public ListCollectionView QuoteCollectionView { get; private set; }

        public ObservableCollection<TradePosition> PendingTrades = new ObservableCollection<SharedData.poco.positions.TradePosition>();
        public ListCollectionView PendingTradesCollectionView { get; private set; }

        public ObservableCollection<TradePosition> OpenTrades = new ObservableCollection<SharedData.poco.positions.TradePosition>();
        public ListCollectionView OpenTradesCollectionView { get; private set; }

        public ObservableCollection<OfficeFloatingStatus> _floatingStatus = new ObservableCollection<OfficeFloatingStatus>();
        public ListCollectionView FloatingStatusCollectionView { get; private set; }

        public ObservableCollection<CoverPosition> _coverPositions = new ObservableCollection<CoverPosition>();
        public ListCollectionView OpenCoverTradesCollectionView { get; private set; }

        public ObservableCollection<string> _notificationLogsList = new ObservableCollection<string>();
        public ListCollectionView NotificationLogsListCollectionView { get; private set; }
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
            set {
                SetProperty(ref _NotificationsFlyoutOpen, value);
            }
        }

        private string _NotificationText;
        public string NotificationText
        {
            get
            {
                return _NotificationText;
            }
            set
            {
                SetProperty(ref _NotificationText, value);
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
            FloatingStatusCollectionView = new ListCollectionView(_floatingStatus);
            OpenCoverTradesCollectionView = new ListCollectionView(_coverPositions);
            NotificationLogsListCollectionView = new ListCollectionView(_notificationLogsList);
            

            _mainWindow = mainWindow;
           _dialogCoord = dialogCoordinator;

            SetupCommands();
        }

        private void SetupCommands()
        {
            NewCoverPositionCommand = new DelegateCommand(ExecuteNewCoverPositionCommand);
            EditSelectedCoverPositionCommand = new DelegateCommand(ExecuteEditSelectedCoverPositionCommand);
            CloseSelectedCoverPositionCommand = new DelegateCommand(ExecuteCloseSelectedCoverPositionCommand);

            CandlestickChartCommand = new DelegateCommand(ExecuteCandlestickChartCommand);
            ApproveSelectedTradeCommand = new DelegateCommand(ExecuteApproveSelectedTradeCommand);
            RejectSelectedTradeCommand = new DelegateCommand(ExecuteRejectSelectedTradeCommand);
            BulkApproveSelectedTradeCommand = new DelegateCommand(ExecuteBulkApproveSelectedTradeCommand);
            BulkRejectSelectedTradeCommand = new DelegateCommand(ExecuteBulkRejectSelectedTradeCommand);

            RequoteSetPriceCommand = new DelegateCommand<object>(ExecuteRequoteSetPriceCommand);
            RequoteSelectedOrders = new DelegateCommand(ExecuteRequoteSelectedOrders);

            ShowLogsCommand = new DelegateCommand(ExecuteShowLogsCommand);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
            WindowClosed = new DelegateCommand(ExecuteWindowClosed);
        }


        private void ExecuteShowLogsCommand()
        {
            LogsFlyoutOpen = !LogsFlyoutOpen;
        }

        private void ExecuteCandlestickChartCommand()
        {
            if (null == QuoteCollectionView.CurrentItem) return;

            string commodity = (QuoteCollectionView.CurrentItem as SharedData.poco.Quote).Name;
            ClientService.GetInstance().requestCandleStickChartData(commodity);
            CandleStickHeading = "Candlestick Chart for " + commodity;
        }

        private void ExecuteRequoteSelectedOrders()
        {
            lock (lockTrades)
            {
                foreach (var row in PendingTradesCollectionView)
                {
                    TradePosition position = row as TradePosition;
                    if (position.IsSelected)
                    {
                        DealerService.GetInstance().requoteOrder(
                            position.ClientName, 
                            position.OrderId, 
                            position.RequotePrice
                        );
                    }
                }
            }
        }

        private void ExecuteRequoteSetPriceCommand(object price)
        {
            decimal requoteDiffernce = 0;
            
            try
            {
                requoteDiffernce = Convert.ToDecimal(price);
                requoteDiffernce /= 10000;
            }
            catch(Exception ex)
            {
                log.Error(ex.Message, ex);
            }

            lock (lockTrades)
            {
                foreach (var row in PendingTrades)
                {
                    TradePosition position = row as TradePosition;
                    if (position.IsSelected)
                    {
                        if (position.RequotePriceSet)
                        {
                            position.RequotePrice += requoteDiffernce;
                        }
                        else
                        {
                            if (position.OrderState == TradePosition.ORDER_STATE_PENDING_OPEN)
                            {
                                position.RequotePrice = position.OpenPrice + requoteDiffernce;
                            }
                            else
                            {
                                position.RequotePrice = position.ClosePrice + requoteDiffernce;
                            }
                            position.RequotePriceSet = true;

                        }
                    }
                }
            }
            
            PendingTradesCollectionView.Refresh();
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
            ClientService.GetInstance().OnCandleStickDataEventHandler += MainWindowViewModel_OnCandleStickDataEventHandler;

            DealerService.GetInstance().OnOfficePositionsUpdateReceived += OnOfficePositionsUpdateReceived;
            DealerService.GetInstance().OnNotificationReceived += MainWindowViewModel_OnNotificationReceived;

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

        private void MainWindowViewModel_OnNotificationReceived(string notification)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                _notificationLogsList.Insert(0, notification);
                NotificationLogsListCollectionView.Refresh();

                NotificationText = notification;
                NotificationsFlyoutOpen = true;

            });
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
                List<OfficeFloatingStatus> floatingStatus = e.OfficeUpdate.FloatingStatus;

                lock (lockTrades)
                {
                    _floatingStatus.Clear();
                    if (floatingStatus.Count > 0)
                    {
                        _floatingStatus.AddRange(floatingStatus);
                        FloatingStatusCollectionView.Refresh();
                    }

                    if(e.OfficeUpdate.CoverPositions.Count > 0)
                    {
                        int selIndex = OpenCoverTradesCollectionView.CurrentPosition;
                        _coverPositions.Clear();

                        _coverPositions.AddRange(e.OfficeUpdate.CoverPositions);

                        if(_coverPositions.Count >= selIndex)
                        {
                            OpenCoverTradesCollectionView.MoveCurrentToPosition(selIndex);
                        }
                    }
                    else
                    {
                        _coverPositions.Clear();
                    }
                    

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
                                else if(t.OrderState.Equals(TradePosition.ORDER_STATE_REQUOTED))
                                {
                                    continue;
                                }
                                else
                                {
                                    if (!PendingTrades.Contains(t))
                                    {
                                        pendingWithSelectionPreserved.Add(t);
                                    }
                                    else
                                    {
                                        TradePosition trade = PendingTrades.First(x => x.Equals(t));
                                        t.IsSelected = trade.IsSelected;
                                        t.RequotePrice = trade.RequotePrice;
                                        t.RequotePriceSet = trade.RequotePriceSet;
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
        

        private async void ExecuteNewCoverPositionCommand()
        {
            CoverPositionWindow cpw = new CoverPositionWindow();
            CoverPositionWindowViewModel cpwContext = cpw.DataContext as CoverPositionWindowViewModel;
            cpwContext.View = cpw;
            cpwContext.OnTradeExecuted += CpwContext_OnTradeExecuted;

            lock (lockQuotes)
            {
                cpwContext._commodities.AddRange(Quotes.Select(x => x.Name).ToList());
            }
            if (cpwContext._commodities.Count > 0)
            {
                cpwContext.SelectedCommodity = cpwContext._commodities[0];
                cpwContext.CommoditiesCollection.Refresh();
            }

            await ChildWindowManager.ShowChildWindowAsync(_mainWindow, cpw, ChildWindowManager.OverlayFillBehavior.WindowContent);
        }

        private async void ExecuteEditSelectedCoverPositionCommand()
        {
            CoverPositionWindow cpw = new CoverPositionWindow();
            CoverPositionWindowViewModel cpwContext = cpw.DataContext as CoverPositionWindowViewModel;
            cpwContext.View = cpw;
            cpwContext.OnTradeExecuted += CpwContext_OnTradeExecuted;

            lock (lockQuotes)
            {
                cpwContext._commodities.AddRange(Quotes.Select(x => x.Name).ToList());
            }
            if (cpwContext._commodities.Count > 0)
            {
                cpwContext.SelectedCommodity = cpwContext._commodities[0];
                cpwContext.CommoditiesCollection.Refresh();
            }

            var position = OpenCoverTradesCollectionView.CurrentItem as CoverPosition;
            if (null == position)
            {
                return;
            }
            cpwContext.CoverPosition = position;

            cpwContext.SelectedCommodity = position.Commodity;
            cpwContext.SelectedCoveringAccount = position.Coveraccount_id;
            cpwContext.SelectedOrderType = position.OrderType;
            cpwContext.LotSize = position.Amount;
            cpwContext.OpenPrice = position.OpenPrice;
            cpwContext.Orderid = position.Remoteid;

            await ChildWindowManager.ShowChildWindowAsync(_mainWindow, cpw, ChildWindowManager.OverlayFillBehavior.WindowContent);
        }

        private void ExecuteCloseSelectedCoverPositionCommand()
        {
            if (null == OpenCoverTradesCollectionView.CurrentItem) return;

            CoverPosition position = OpenCoverTradesCollectionView.CurrentItem as CoverPosition;
            position.ClosedBy = username;

            DealerService.GetInstance().saveUpdateCloseCoverPosition(position, "closeCoverPosition");
        }

        private void CpwContext_OnTradeExecuted(CoverPositionWindowViewModel context)
        {
            context.View.Close();


            if (context.IsCancel)
            {
                return;
            }

            var position = context.CoverPosition;
            if (null == position)
            {
                position = new CoverPosition();
            }

            position.Amount = context.LotSize;
            position.Coveraccount_id = context.SelectedCoveringAccount;
            position.OrderType = context.SelectedOrderType;
            position.OpenPrice = context.OpenPrice;
            //position.Opentime = DateTime.Now; //do not set
            position.OpenedBy = username;
            position.Commodity = context.SelectedCommodity;
            //position.Internalid = Guid.NewGuid(); //will be regenerated at server end..
            position.Remoteid = context.Orderid;

            DealerService.GetInstance().saveUpdateCloseCoverPosition(position, "createUpdateCoverPosition");

        }


        
    }
}
