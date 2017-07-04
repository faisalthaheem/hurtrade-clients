using SharedData.poco.charting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedData.events
{
    public class CandleStickDataEventArgs : EventArgs
    {
        public List<CandleStick> Data { get; set; }
    }
}
