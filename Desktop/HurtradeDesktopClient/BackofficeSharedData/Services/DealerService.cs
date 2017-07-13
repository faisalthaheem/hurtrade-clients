using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using SharedData.poco;
using SharedData.events;
using System.Text;
using SharedData.poco.trade;
using SharedData.poco.updates;
using BackofficeSharedData.poco.updates;
using SharedData.poco.positions;
using System.Collections.Generic;
using SharedData.misc;
using BackofficeSharedData.events;

namespace BackofficeSharedData.Services
{
    public delegate void UpdateReceivedHandler(object sender, ClientUpdateEventArgs e);
    public delegate void OfficePositionsUpdateReceivedHandler(object sender, BackofficeUpdateEventArgs e);
    public delegate void CoverAccountsListReceivedHandler(object sender, CoverAccountsEventArgs e);

    public class DealerService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IModel _channel = null;
        private static DealerService _instance = null;
        private string officeExchangeName = string.Empty;
        private string officeDealerOutQName = string.Empty;
        private string officeDealerInQName = string.Empty;
        private string consumerQueueName = string.Empty;

        public event UpdateReceivedHandler OnUpdateReceived;
        public event OfficePositionsUpdateReceivedHandler OnOfficePositionsUpdateReceived;
        public event CoverAccountsListReceivedHandler OnCoverAccountsListReceived;

        private Object lockChannel = new Object();

        private string _username, _password;

        #region Command Verbs
        public const string COMMAND_VERB_APPROVE = "approve";
        public const string COMMAND_VERB_REJECT = "reject";
        public const string COMMAND_VERB_REQUOTE = "requote";
        #endregion

        private DealerService() {
            
        }

        protected void RaiseOnUpdateReceived(ClientUpdateEventArgs args)
        {
            if(null != OnUpdateReceived)
            {
                UpdateReceivedHandler handler = OnUpdateReceived;
                handler(this, args);
            }
        }
        public static void Cleanup()
        {
            if (null == _instance)
            {
                return;
            }

            _instance._channel.Close();
            _instance._channel = null;
        }

        public static DealerService GetInstance()
        {
            if (null == _instance)
            {
                _instance = new DealerService();
            }

            return _instance;
        }

        public bool init(string username, string password, string officeExchangeName, string officeDealerOutQName, string officeDealerInQName)
        {
            bool ret = true;
            try
            {
                this.officeExchangeName = officeExchangeName;
                this.officeDealerOutQName = officeDealerOutQName;
                this.officeDealerInQName = officeDealerInQName;
                _username = username;
                _password = password;
                
                _channel = new ConnectionFactory() {
                    AutomaticRecoveryEnabled = true,
                    HostName = System.Configuration.ConfigurationManager.AppSettings["brokerip"],
                    UserName = username,
                    Password = password
                }.CreateConnection().CreateModel();

                #region setup

                consumerQueueName = _channel.QueueDeclare(string.Empty, false, false, true, null).QueueName;
                _channel.QueueBind(consumerQueueName, officeExchangeName, "todealer");

                var qResponseMsgConsumer = new EventingBasicConsumer(_channel);
                qResponseMsgConsumer.Received += qResponseMsgConsumer_Received;

                _channel.BasicConsume(consumerQueueName, false, qResponseMsgConsumer);

                #endregion
            }
            catch (Exception ex)
            {
                log.Error(ex);
                ret = false;
            }

            return ret;
        }
        

        void qResponseMsgConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                string messageType = e.BasicProperties.Type;
                _channel.BasicAck(e.DeliveryTag, false);

                if (messageType.Equals("officePositions"))
                {
                    BackofficeUpdate update = JsonConvert.DeserializeObject<BackofficeUpdate>(ASCIIEncoding.UTF8.GetString(body));

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {

                            //fill in the current price against each of the positions
                            foreach (var u in update.UserPositions)
                            {
                                foreach (var position in u.Value)
                                {
                                    if (position.OrderType.Equals(TradePosition.ORDER_TYPE_BUY))
                                    {
                                        position.CurrentPrice = (update.Quotes.ContainsKey(position.Commodity)) ? update.Quotes[position.Commodity].Bid : decimal.Zero;
                                    }
                                    else
                                    {
                                        position.CurrentPrice = (update.Quotes.ContainsKey(position.Commodity)) ? update.Quotes[position.Commodity].Ask : decimal.Zero;
                                    }
                                }
                            }

                            Dictionary<string, TradePosition> openFloatingStatus = CalculateFloatingStatus(update.UserPositions);
                            Dictionary<string, CoverPosition> coverFloatingStatus = CalculateCoverFloatingStatus(update.CoverPositions);
                            List<OfficeFloatingStatus> floatingStatus = new List<OfficeFloatingStatus>();

                            foreach (var row in openFloatingStatus.Values)
                            {
                                OfficeFloatingStatus status = new OfficeFloatingStatus()
                                {
                                    Commodity = row.Commodity,
                                    Ask = update.Quotes.ContainsKey(row.Commodity) ? update.Quotes[row.Commodity].Ask : 0,
                                    Bid = update.Quotes.ContainsKey(row.Commodity) ? update.Quotes[row.Commodity].Bid : 0,
                                    BuyAmt = row.SumBuyAmt,
                                    SellAmt = row.SumSellAmt,
                                    BuyDeals = row.BuysIn,
                                    SellDeals = row.SellsIn,
                                    BuyAvg = CalculateAverage(row.SumBuyPrice, row.BuysIn),
                                    SellAvg = CalculateAverage(row.SumSellPrice, row.SellsIn),
                                    NetAmt = (row.SumBuyAmt - (coverFloatingStatus.ContainsKey(row.Commodity) ? coverFloatingStatus[row.Commodity].SumBuyAmt : 0))
                                    - (row.SumSellAmt - (coverFloatingStatus.ContainsKey(row.Commodity) ? coverFloatingStatus[row.Commodity].SumSellAmt : 0))
                                };

                                decimal buyCovAvg = coverFloatingStatus.ContainsKey(row.Commodity) ? CalculateAverage(coverFloatingStatus[row.Commodity].SumBuyPrice, coverFloatingStatus[row.Commodity].BuysIn) : 0;
                                decimal sellCovAvg = coverFloatingStatus.ContainsKey(row.Commodity) ? CalculateAverage(coverFloatingStatus[row.Commodity].SumSellPrice, coverFloatingStatus[row.Commodity].SellsIn) : 0;

                                decimal plBuyAvg = Utils.calculate_pl(status.BuyAvg, status.BuyAmt, TradePosition.ORDER_TYPE_BUY, status.Commodity, update.Quotes);
                                decimal plSellAvg = Utils.calculate_pl(status.SellAvg, status.SellAmt, TradePosition.ORDER_TYPE_SELL, status.Commodity, update.Quotes);
                                decimal plCovBuyAvg = (buyCovAvg > 0) ? Utils.calculate_pl(buyCovAvg, coverFloatingStatus[row.Commodity].SumBuyAmt, TradePosition.ORDER_TYPE_BUY, status.Commodity, update.Quotes) : 0;
                                decimal plCovSellAvg = (sellCovAvg > 0) ? Utils.calculate_pl(sellCovAvg, coverFloatingStatus[row.Commodity].SumSellAmt, TradePosition.ORDER_TYPE_SELL, status.Commodity, update.Quotes) : 0;

                                status.Netpl = ((plBuyAvg + plSellAvg) * -1) + (plCovBuyAvg + plCovSellAvg);
                                status.Pl = ((plBuyAvg + plSellAvg) * -1) + "/" + (plCovBuyAvg + plCovSellAvg);

                                floatingStatus.Add(status);
                            }

                            update.FloatingStatus = floatingStatus;

                            //finally map cover accounts to cover positions
                            Dictionary<int, CoverAccount> covAccs = new Dictionary<int, CoverAccount>();
                            foreach (var acc in update.CoverAccounts)
                            {
                                covAccs[acc.Id] = acc;
                            }

                            foreach (var pos in update.CoverPositions)
                            {
                                pos.CovAcc = covAccs.ContainsKey(pos.Coveraccount_id) ? covAccs[pos.Coveraccount_id] : new CoverAccount() { Title = string.Empty };
                                pos.CoverAccountTitle = pos.CovAcc.Title;
                            }

                            OnOfficePositionsUpdateReceived(this, new BackofficeUpdateEventArgs(update));
                        }
                        catch (Exception ex)
                        {
                            log.Error(ex);
                        }
                    });
                }
                else if (messageType.Equals("CoverAccounts"))
                {
                    Dictionary<string, string> response = JsonConvert.DeserializeObject<Dictionary<string, string>>(ASCIIEncoding.UTF8.GetString(body));
                    List<CoverAccount> coverAccounts = JsonConvert.DeserializeObject<List<CoverAccount>>(response["CoverAccounts"]);

                    CoverAccountsEventArgs args = new CoverAccountsEventArgs() { CoverAccounts = coverAccounts };

                    OnCoverAccountsListReceived(this, args);

                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private decimal CalculateAverage(decimal sum, decimal count)
        {
            if(count > 0)
            {
                return sum / count;
            }

            return 0;
        }

        private Dictionary<string, CoverPosition> CalculateCoverFloatingStatus(List<CoverPosition> receivedPositions)
        {
            Dictionary<string, CoverPosition> ret = new Dictionary<string, CoverPosition>();

            string key = string.Empty;

            foreach (var p in receivedPositions)
            {
                key = p.Commodity;
                if (!ret.ContainsKey(key))
                {
                    ret[key] = new CoverPosition() {
                        Commodity = p.Commodity
                    };
                }
            }

            foreach (var p in receivedPositions)
            {
                key = p.Commodity;

                if (p.OrderType.Equals(TradePosition.ORDER_TYPE_BUY))
                {
                    ret[key].SumBuyAmt += p.Amount;
                    ret[key].SumPlBuy += p.CurrentPL;
                    ret[key].SumBuyPrice += p.OpenPrice;
                    ret[key].BuysIn++;
                }
                else
                {
                    ret[key].SumSellAmt += p.Amount;
                    ret[key].SumPlSell += p.CurrentPL;
                    ret[key].SumSellPrice += p.OpenPrice;
                    ret[key].SellsIn++;
                }
            }
            
            return ret;
        }

        private Dictionary<string, TradePosition> CalculateFloatingStatus(Dictionary<string,List<TradePosition>> receivedPositions)
        {
            Dictionary<string, TradePosition> ret = new Dictionary<string, TradePosition>();

            string key = string.Empty;

            foreach (var l in receivedPositions.Values)
            {
                foreach (var p in l)
                {
                    key = p.Commodity;
                    if (!ret.ContainsKey(key))
                    {
                        ret[key] = new TradePosition()
                        {
                            Commodity = p.Commodity
                        };
                    }
                }
            }

            foreach (var l in receivedPositions.Values)
            {
                foreach (var p in l)
                {
                    key = p.Commodity;

                    if (p.OrderType.Equals(TradePosition.ORDER_TYPE_BUY))
                    {
                        ret[key].SumBuyAmt += p.Amount;
                        ret[key].SumPlBuy += p.CurrentPl;
                        ret[key].SumBuyPrice += p.OpenPrice;
                        ret[key].BuysIn++;
                    }
                    else
                    {
                        ret[key].SumSellAmt += p.Amount;
                        ret[key].SumPlSell += p.CurrentPl;
                        ret[key].SumSellPrice += p.OpenPrice;
                        ret[key].SellsIn++;
                    }
                }
            }

            return ret;
        }
        
        public void approveRejectOrder(string clientUsername, Guid orderid, string commandVerb)
        {
            GenericRequestResponseDictionary request = new GenericRequestResponseDictionary();
            request["client"] = clientUsername;
            request["orderId"] = orderid.ToString();
            request["command"] = commandVerb;

            lock (lockChannel)
            {
                try
                {
                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.UserId = _username;
                    props.Type = "client"; //as we want server to process a client's positions

                    _channel.BasicPublish(
                        officeExchangeName,
                        "fromdealer",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
                    );
                    
                }catch(Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }
        }


        public void listCoverAccounts()
        {
            GenericRequestResponseDictionary request = new GenericRequestResponseDictionary();
            request["command"] = "listCoverAccounts";
            request["responseQueue"] = consumerQueueName;

            lock (lockChannel)
            {
                try
                {
                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.UserId = _username;
                    props.Type = "office";

                    _channel.BasicPublish(
                        officeExchangeName,
                        "fromdealer",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
                    );

                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }
        }

        public void saveUpdateCloseCoverPosition(CoverPosition position, string command)
        {
            GenericRequestResponseDictionary request = new GenericRequestResponseDictionary();
            request["position"] = JsonConvert.SerializeObject(position);
            request["command"] = command;

            lock (lockChannel)
            {
                try
                {
                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.UserId = _username;
                    props.Type = "office";

                    _channel.BasicPublish(
                        officeExchangeName,
                        "fromdealer",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
                    );

                }
                catch (Exception ex)
                {
                    log.Error(ex.Message, ex);
                }
            }
        }

    }
}
