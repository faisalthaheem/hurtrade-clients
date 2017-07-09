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

        public Dictionary<string, List<TradePosition>> UserPositions { get => userPositions; set => userPositions = value; }
        public QuoteList Quotes { get => quotes; set => quotes = value; }
    }
}
