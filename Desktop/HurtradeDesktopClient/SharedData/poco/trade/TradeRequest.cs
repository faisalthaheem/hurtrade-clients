using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedData.poco.trade
{
    public class TradeRequest
    {
        public const string REQUEST_TYPE_BUY = "buy";
        public const string REQUEST_TYPE_SELL = "sell";
    
        public string requestType { get; set; }
        public string commodity { get; set; }
        public  decimal requestedPrice { get; set; }
        public  decimal requestedLot { get; set; }
        public  string requestTime { get; set; }
        public  Guid tradeId { get; set; }

    }
}
