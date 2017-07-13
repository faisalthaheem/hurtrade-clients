using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class CoverPosition
    {
        [JsonProperty]
        private int id;
        [JsonProperty]
        private int coveraccount_id;
        [JsonProperty]
        private string commodity;
        [JsonProperty]
        private string orderType;
        [JsonProperty]
        private string openedBy;
        [JsonProperty]
        private string closedBy;
        [JsonProperty]
        private decimal currentPL;
        [JsonProperty]
        private decimal amount;
        [JsonProperty]
        private decimal openPrice;
        [JsonProperty]
        private decimal closePrice;
        [JsonIgnore]
        private DateTime opentime;
        [JsonIgnore]
        private DateTime closetime;
        [JsonIgnore]
        private DateTime created;
        [JsonIgnore]
        private DateTime endedat;
        [JsonProperty]
        private Guid internalid = Guid.NewGuid();
        [JsonProperty]
        private string remoteid;

        //these are locally used
        [JsonIgnore]
        private decimal sumPlBuy;
        [JsonIgnore]
        private decimal sumBuyAmt;
        [JsonIgnore]
        private decimal sumBuyPrice;
        [JsonIgnore]
        private int buysIn;

        [JsonIgnore]
        private decimal sumPlSell;
        [JsonIgnore]
        private decimal sumSellAmt;
        [JsonIgnore]
        private decimal sumSellPrice;
        [JsonIgnore]
        private int sellsIn;

        [JsonIgnore]
        private CoverAccount _covAcc;
        [JsonIgnore]
        private string _coverAccountTitle;

        [JsonIgnore]
        public int Id { get => id; set => id = value; }
        [JsonIgnore]
        public int Coveraccount_id { get => coveraccount_id; set => coveraccount_id = value; }
        [JsonIgnore]
        public string Commodity { get => commodity; set => commodity = value; }
        [JsonIgnore]
        public string OrderType { get => orderType; set => orderType = value; }
        [JsonIgnore]
        public string OpenedBy { get => openedBy; set => openedBy = value; }
        [JsonIgnore]
        public string ClosedBy { get => closedBy; set => closedBy = value; }
        [JsonIgnore]
        public decimal CurrentPL { get => currentPL; set => currentPL = value; }
        [JsonIgnore]
        public decimal Amount { get => amount; set => amount = value; }
        [JsonIgnore]
        public decimal OpenPrice { get => openPrice; set => openPrice = value; }
        [JsonIgnore]
        public decimal ClosePrice { get => closePrice; set => closePrice = value; }
        [JsonIgnore]
        public DateTime Opentime { get => opentime; set => opentime = value; }
        [JsonIgnore]
        public DateTime Closetime { get => closetime; set => closetime = value; }
        [JsonIgnore]
        public DateTime Created { get => created; set => created = value; }
        [JsonIgnore]
        public DateTime Endedat { get => endedat; set => endedat = value; }
        [JsonIgnore]
        public Guid Internalid { get => internalid; set => internalid = value; }
        [JsonIgnore]
        public string Remoteid { get => remoteid; set => remoteid = value; }

        [JsonIgnore]
        public decimal SumPlBuy { get => sumPlBuy; set => sumPlBuy = value; }
        [JsonIgnore]
        public decimal SumBuyAmt { get => sumBuyAmt; set => sumBuyAmt = value; }
        [JsonIgnore]
        public decimal SumBuyPrice { get => sumBuyPrice; set => sumBuyPrice = value; }
        [JsonIgnore]
        public int BuysIn { get => buysIn; set => buysIn = value; }
        [JsonIgnore]
        public decimal SumPlSell { get => sumPlSell; set => sumPlSell = value; }
        [JsonIgnore]
        public decimal SumSellAmt { get => sumSellAmt; set => sumSellAmt = value; }
        [JsonIgnore]
        public decimal SumSellPrice { get => sumSellPrice; set => sumSellPrice = value; }
        [JsonIgnore]
        public int SellsIn { get => sellsIn; set => sellsIn = value; }
        [JsonIgnore]
        public CoverAccount CovAcc { get => _covAcc; set => _covAcc = value; }
        [JsonIgnore]
        public string CoverAccountTitle { get => _coverAccountTitle; set => _coverAccountTitle = value; }


        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            CoverPosition p = obj as CoverPosition;
            if (p == null)
            {
                return false;
            }

            return p.internalid.Equals(internalid);
        }

        public bool Equals(CoverPosition p)
        {
            if (p == null)
            {
                return false;
            }

            return p.internalid.Equals(internalid);
        }

        public override int GetHashCode()
        {
            return internalid.GetHashCode();
        }
    }
}
