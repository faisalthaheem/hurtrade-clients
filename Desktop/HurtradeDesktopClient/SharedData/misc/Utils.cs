using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedData.poco;
using SharedData.poco.positions;

namespace SharedData.misc
{
    public class Utils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static decimal calculate_pl(decimal OpenPrice, decimal Amount, string orderType, string instrument, QuoteList quotes)
        {
            decimal retPL = decimal.Zero;
            decimal ClosePrice = decimal.Zero;
            string mBaseCurrency = instrument.Substring(0, 3);
            //string mQuoteCurrency = instrument.Substring(3, 3);
            
            
            //calculate P/L
            try
            {
                if (quotes.ContainsKey(instrument))
                {
                    decimal conversionRate = 1;

                    if (orderType.Equals(TradePosition.ORDER_TYPE_BUY))
                    {
                        ClosePrice = quotes[instrument].Bid;

                        if (mBaseCurrency != "USD")
                        {
                            conversionRate = quotes[mBaseCurrency + "USD"].Bid;
                        }

                        retPL = (ClosePrice - OpenPrice) * conversionRate * quotes[instrument].LotSize * Amount;
                    }
                    else
                    {
                        ClosePrice = quotes[instrument].Ask;

                        if (mBaseCurrency != "USD")
                        {
                            conversionRate = quotes[mBaseCurrency + "USD"].Ask;
                        }

                        retPL = (OpenPrice - ClosePrice) * conversionRate * quotes[instrument].LotSize * Amount;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                retPL = 0;
            }

            return retPL;
        }
    }
}
