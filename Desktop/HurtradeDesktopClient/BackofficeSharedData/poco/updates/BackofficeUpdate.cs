using SharedData.poco;
using SharedData.poco.positions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class BackofficeUpdate
    {
        private Dictionary<string, List<TradePosition>> userPositions;
        private QuoteList quotes;
        private List<CoverAccount> coverAccounts;
        private List<CoverPosition> coverPositions;
        private List<OfficeFloatingStatus> floatingStatus;

        public Dictionary<string, List<TradePosition>> UserPositions { get => userPositions; set => userPositions = value; }
        public QuoteList Quotes { get => quotes; set => quotes = value; }
        public List<CoverAccount> CoverAccounts { get => coverAccounts; set => coverAccounts = value; }
        public List<CoverPosition> CoverPositions { get => coverPositions; set => coverPositions = value; }
        public List<OfficeFloatingStatus> FloatingStatus { get => floatingStatus; set => floatingStatus = value; }
    }
}
