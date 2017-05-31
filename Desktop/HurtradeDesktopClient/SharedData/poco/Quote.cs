using System;

namespace SharedData.poco
{

    public class Quote
    {
        private decimal bid;
        private decimal ask;
        private DateTime quoteTime;
        private decimal rate;
        private string name;
        private decimal lotSize;

        public DateTime QuoteTime { get => quoteTime; set => quoteTime = value; }
        public decimal Ask { get => ask; set => ask = value; }
        public decimal Bid { get => bid; set => bid = value; }
        public decimal Rate { get => rate; set => rate = value; }
        public string Name { get => name; set => name = value; }
        public decimal LotSize { get => lotSize; set => lotSize = value; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Quote q = obj as Quote;
            if (q == null)
            {
                return false;
            }

            return q.Name.Equals(Name);
        }

        public bool Equals(Quote q)
        {
            if (q == null)
            {
                return false;
            }

            return q.Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Bid.GetHashCode() ^ Ask.GetHashCode();
        }
    }
}
