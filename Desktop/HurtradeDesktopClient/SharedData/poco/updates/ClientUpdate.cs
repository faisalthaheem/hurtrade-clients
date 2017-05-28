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
        public Dictionary<Guid, position> Positions;
        public QuoteList ClientQuotes;
    }
}
