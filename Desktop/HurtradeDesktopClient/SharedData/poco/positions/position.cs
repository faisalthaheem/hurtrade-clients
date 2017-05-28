using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedData.poco.positions
{
    public class position
    {
        public static string ORDER_TYPE_BUY = "b";
        public static string ORDER_TYPE_SELL = "s";
    
        public static string ORDER_STATE_PENDING_OPEN = "pending_dealer_open";
        public static string ORDER_STATE_OPEN = "open";
        public static string ORDER_STATE_PENDING_CLOSE = "pending_dealer_close";
        public static string ORDER_STATE_CLOSED = "closed";

        //is this buy or sell?
        private string OrderType;
        //what commodity are we trading?
        private string Commodity;
        //how much are we trading, can change in cases of hedge so not final
        private decimal Amount;
        //p/l of this position
        private decimal CurrentPl;
        //a unique identifier for this order
        private Guid OrderId;
        //the price at which the commodity was requested
        private decimal OpenPrice;
        //what state the order currently is in
        private string OrderState;
    }
}
