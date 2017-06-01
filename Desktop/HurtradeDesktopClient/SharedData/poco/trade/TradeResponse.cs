using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedData.poco.trade
{
    public class TradeResponse
    {
        private TradeRequest request;
        private String response;

        public string Response { get => response; set => response = value; }
        internal TradeRequest Request { get => request; set => request = value; }
    }
}
