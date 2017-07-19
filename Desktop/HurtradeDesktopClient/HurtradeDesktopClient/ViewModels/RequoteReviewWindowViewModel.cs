using HurtradeDesktopClient.Views;
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
using SharedData.poco.positions;

namespace HurtradeDesktopClient.ViewModels
{
    public delegate void RequoteReviewedEventHandler(RequoteReviewWindowViewModel model, string action);

    public class RequoteReviewWindowViewModel : BindableBase
    {

        #region Commands
        public DelegateCommand AcceptRequotedPriceCommand { get; private set; }
        public DelegateCommand RejectRequotedPriceCommand { get; private set; }

        public DelegateCommand WindowLoaded { get; private set; }
        public DelegateCommand WindowClosing { get; private set; }
        #endregion


        #region Events
        public event RequoteReviewedEventHandler OnRequoteReviewed;
        #endregion

        #region Properties
        

        private decimal _RequotedPrice;
        public decimal RequotedPrice
        {
            get
            {
                return _RequotedPrice;
            }
            set
            {
                SetProperty(ref _RequotedPrice, value);
            }
        }

        private decimal _RequestedPrice;
        public decimal RequestedPrice
        {
            get
            {
                return _RequestedPrice;
            }
            set
            {
                SetProperty(ref _RequestedPrice, value);
            }
        }

        private long _OrderId;
        public long OrderId
        {
            get
            {
                return _OrderId;
            }
            set
            {
                SetProperty(ref _OrderId, value);
            }
        }

        private string _TimeRemaining = string.Empty;
        public string TimeRemaining
        {
            get
            {
                return _TimeRemaining;
            }
            set
            {
                SetProperty(ref _TimeRemaining, value);
            }
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

        private int _remainingTime; //this is decremented by timer every second until it reaches 0 and the window is closed
        private TradePosition _tradePosition;

        public RequoteReviewWindow View { get; set; }
        public int RemainingTime { get => _remainingTime; set => _remainingTime = value; }
        public TradePosition TradePosition { get => _tradePosition; set => _tradePosition = value; }
        #endregion


        #region Private Members

        private System.Timers.Timer _countdownTimer;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion


        public RequoteReviewWindowViewModel()
        {
            SetupCommands();
        }

        private void SetupCommands()
        {
            AcceptRequotedPriceCommand = new DelegateCommand(ExecuteAcceptRequotedPrice);
            RejectRequotedPriceCommand = new DelegateCommand(ExecuteRejectRequotedPrice);
            WindowLoaded = new DelegateCommand(ExecuteWindowLoaded);
            WindowClosing = new DelegateCommand(ExecuteWindowClosing);
        }

        private void ExecuteWindowClosing()
        {
            
        }

        private void ExecuteWindowLoaded()
        {
            WindowTitle = "Order Requoted";

            //start timer
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += _countdownTimer_Elapsed;
            _countdownTimer.Start();
        }

        private void _countdownTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(_remainingTime-- <= 0)
            {
                OnRequoteReviewed(this, "cancel");
            }
            else
            {
                TimeRemaining = "Remainig Time: " + _remainingTime;
            }

        }

        private void ExecuteAcceptRequotedPrice()
        {
            OnRequoteReviewed(this, "accept");
        }

        private void ExecuteRejectRequotedPrice()
        {
            OnRequoteReviewed(this, "cancel");
        }
        
    }
}
