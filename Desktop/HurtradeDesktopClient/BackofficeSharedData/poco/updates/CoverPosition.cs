using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class CoverPosition
    {
        private int id;
        private int coveraccount_id;
        private string commodity;
        private string orderType;
        private string openedBy;
        private string closedBy;
        private decimal currentPL;
        private decimal amount;
        private decimal openPrice;
        private decimal closePrice;
        private DateTime opentime;
        private DateTime closetime;
        private DateTime created;
        private DateTime endedat;
        private Guid internalid;
        private string remoteid;

        public int Id { get => id; set => id = value; }
        public int Coveraccount_id { get => coveraccount_id; set => coveraccount_id = value; }
        public string Commodity { get => commodity; set => commodity = value; }
        public string OrderType { get => orderType; set => orderType = value; }
        public string OpenedBy { get => openedBy; set => openedBy = value; }
        public string ClosedBy { get => closedBy; set => closedBy = value; }
        public decimal CurrentPL { get => currentPL; set => currentPL = value; }
        public decimal Amount { get => amount; set => amount = value; }
        public decimal OpenPrice { get => openPrice; set => openPrice = value; }
        public decimal ClosePrice { get => closePrice; set => closePrice = value; }
        public DateTime Opentime { get => opentime; set => opentime = value; }
        public DateTime Closetime { get => closetime; set => closetime = value; }
        public DateTime Created { get => created; set => created = value; }
        public DateTime Endedat { get => endedat; set => endedat = value; }
        public Guid Internalid { get => internalid; set => internalid = value; }
        public string Remoteid { get => remoteid; set => remoteid = value; }
    }
}
