using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedData.poco.charting
{
    public class CandleStick
    {
        private double highest;
        private double open;
        private double close;
        private double lowest;
        private DateTime sampleFor;

        public double Highest { get => highest; set => highest = value; }
        public double Open { get => open; set => open = value; }
        public double Close { get => close; set => close = value; }
        public double Lowest { get => lowest; set => lowest = value; }
        public DateTime SampleFor { get => sampleFor; set => sampleFor = value; }
    }
}
