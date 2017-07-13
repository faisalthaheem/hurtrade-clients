using HurtradeBackofficeClient.Views;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using Prism.Commands;
using Prism.Mvvm;
using SharedData.poco;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.SimpleChildWindow.Utils;
using SharedData.Services;
using BackofficeSharedData.poco.updates;
using BackofficeSharedData.Services;

namespace HurtradeBackofficeClient.ViewModels
{
    public delegate void TradeExecutedEventHandler(CoverPositionWindowViewModel context);

    public class OrderType
    {
        private string _title;
        private string _val;

        public string Title { get => _title; set => _title = value; }
        public string Val { get => _val; set => _val = value; }
    }

    public class CoverPositionWindowViewModel : BindableBase
    {

        #region Commands
        public DelegateCommand ExecuteSaveUpdate { get; private set; }
        public DelegateCommand ExecuteCancel { get; private set; }
        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        #endregion


        #region Events
        public event TradeExecutedEventHandler OnTradeExecuted;
        #endregion

        #region Properties
        public ObservableCollection<string> _commodities = new ObservableCollection<string>();
        public ListCollectionView CommoditiesCollection { get; private set; }

        public ObservableCollection<CoverAccount> _coveringAccounts = new ObservableCollection<CoverAccount>();
        public ListCollectionView CoveringAccounts { get; private set; }

        public ObservableCollection<OrderType> _orderTypes = new ObservableCollection<OrderType>();
        public ListCollectionView OrderTypes { get; private set; }

        private string _selectedCommodity;
        public string SelectedCommodity
        {
            get { return _selectedCommodity; }
            set { SetProperty(ref _selectedCommodity, value); }
        }

        private string _orderid;
        public string Orderid
        {
            get { return _orderid; }
            set { SetProperty(ref _orderid, value); }
        }

        private string _selectedOrderType;
        public string SelectedOrderType
        {
            get { return _selectedOrderType; }
            set { SetProperty(ref _selectedOrderType, value); }
        }
        
        private int _selectedCoveringAccount;
        public int SelectedCoveringAccount
        {
            get { return _selectedCoveringAccount; }
            set { SetProperty(ref _selectedCoveringAccount, value); }
        }

        private decimal _LotSize;
        public decimal LotSize
        {
            get { return _LotSize; }
            set { SetProperty(ref _LotSize, value); }
        }

        private decimal _openPrice;
        public decimal OpenPrice
        {
            get { return _openPrice; }
            set { SetProperty(ref _openPrice, value); }
        }

        private string _WindowTitle = string.Empty;
        public string WindowTitle
        {
            get
            {
                return _WindowTitle;
            }
            set
            {
                SetProperty(ref _WindowTitle, value);
            }
        }

        private bool isCancel = false;
        private CoverPosition coverPosition;


        public CoverPositionWindow View { get; set; }
        public bool IsCancel { get => isCancel; set => isCancel = value; }
        public CoverPosition CoverPosition { get => coverPosition; set => coverPosition = value; }

        #endregion


        #region Private Members
        private Object lockQuotes = new Object();

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion


        public CoverPositionWindowViewModel()
        {
            CoveringAccounts = new ListCollectionView(_coveringAccounts);
            OrderTypes = new ListCollectionView(_orderTypes);
            CommoditiesCollection = new ListCollectionView(_commodities);

            SetupCommands();
        }

        private void SetupCommands()
        {
            ExecuteSaveUpdate = new DelegateCommand(ExecuteSaveUpdateCommand);
            ExecuteCancel = new DelegateCommand(ExecuteCancelCommand);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);

            _orderTypes.Add(new OrderType { Title = "Buy", Val = "buy" });
            _orderTypes.Add(new OrderType { Title = "Sell", Val = "sell" });

            OrderTypes.Refresh();

            SelectedOrderType = "buy";
        }

        private void ExecuteWindowClosing()
        {
            DealerService.GetInstance().OnCoverAccountsListReceived -= CoverPositionWindowViewModel_OnCoverAccountsListReceived;
        }

        private void ExecuteWindowLoaded()
        {
            
            WindowTitle = "Covering Position";

            DealerService.GetInstance().OnCoverAccountsListReceived += CoverPositionWindowViewModel_OnCoverAccountsListReceived;
            DealerService.GetInstance().listCoverAccounts();
        }

        private void CoverPositionWindowViewModel_OnCoverAccountsListReceived(object sender, BackofficeSharedData.events.CoverAccountsEventArgs e)
        {
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                this._coveringAccounts.AddRange(e.CoverAccounts);
                CoveringAccounts.Refresh();

                if (SelectedCoveringAccount == null)
                {
                    if (_coveringAccounts.Count > 0)
                    {
                        SelectedCoveringAccount = _coveringAccounts[0].Id;
                    }
                }
            });
        }

        private void ExecuteSaveUpdateCommand()
        {
            OnTradeExecuted?.Invoke(this);
        }

        private void ExecuteCancelCommand()
        {
            IsCancel = true;
            OnTradeExecuted?.Invoke(this);
        }

    }
}
