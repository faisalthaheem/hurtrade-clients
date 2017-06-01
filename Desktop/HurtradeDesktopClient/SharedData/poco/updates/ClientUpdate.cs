using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedData.poco.positions;

namespace SharedData.poco.updates
{
    public class ClientUpdateEventArgs : EventArgs
    {
        public Dictionary<Guid, Position> Positions;
        public QuoteList ClientQuotes;
    }
}
