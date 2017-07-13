using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class OfficeFloatingStatus
    {
        private string commodity;
        private int buyDeals;
        private decimal buyAmt;
        private decimal buyAvg;
        private decimal bid;
        private int sellDeals;
        private decimal sellAmt;
        private decimal sellAvg;
        private decimal ask;
        private decimal netAmt;
        private string pl;
        private decimal netpl;

        public string Commodity { get => commodity; set => commodity = value; }
        public int BuyDeals { get => buyDeals; set => buyDeals = value; }
        public decimal BuyAmt { get => buyAmt; set => buyAmt = value; }
        public decimal BuyAvg { get => buyAvg; set => buyAvg = value; }
        public decimal Bid { get => bid; set => bid = value; }
        public int SellDeals { get => sellDeals; set => sellDeals = value; }
        public decimal SellAmt { get => sellAmt; set => sellAmt = value; }
        public decimal SellAvg { get => sellAvg; set => sellAvg = value; }
        public decimal Ask { get => ask; set => ask = value; }
        public decimal NetAmt { get => netAmt; set => netAmt = value; }
        public string Pl { get => pl; set => pl = value; }
        public decimal Netpl { get => netpl; set => netpl = value; }
    }
}
