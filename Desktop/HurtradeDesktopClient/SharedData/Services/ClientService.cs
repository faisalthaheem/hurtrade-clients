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
using System.Collections.Generic;
using SharedData.poco.charting;
using SharedData.poco.positions;

namespace SharedData.Services
{
    public delegate void UpdateReceivedHandler(object sender, ClientUpdateEventArgs e);
    public delegate void OrderUpdateEventHandler(object sender, GenericResponseEventArgs e);
    public delegate void AccountStatusEventHandler(object sender, GenericResponseEventArgs e);
    public delegate void CandleStickDataEventHandler(object sender, CandleStickDataEventArgs e);
    public delegate void NotificationReceivedEventHandler(string notification);
    public delegate void ConnectionClosedByServerEventHandler();

    public class ClientService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IConnection _connection = null;
        private IModel _channel = null;
        private static ClientService _instance = null;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;

        public event UpdateReceivedHandler OnUpdateReceived;
        public event OrderUpdateEventHandler OnOrderUpdateReceived;
        public event AccountStatusEventHandler OnAccountStatusEventReceived;
        public event CandleStickDataEventHandler OnCandleStickDataEventHandler;
        public event NotificationReceivedEventHandler OnNotificationReceived;
        public event ConnectionClosedByServerEventHandler OnConnectionClosedByServerEventHandler;

        private string _username, _password;

        private ClientService() {
            
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

            if (_instance._channel != null && _instance._channel.IsOpen)
            {
                _instance._channel.Close();
                _instance._channel = null;
            }
        }

        public static ClientService GetInstance()
        {
            if (null == _instance)
            {
                _instance = new ClientService();
            }

            return _instance;
        }

        public bool init(string username, string password, string requestExchange, string responseQueue)
        {
            bool ret = true;
            try
            {
                this.clientExchangeName = requestExchange;
                this.responseQueueName = responseQueue;
                _username = username;
                _password = password;

                _connection = new ConnectionFactory()
                {
                    AutomaticRecoveryEnabled = true,
                    HostName = System.Configuration.ConfigurationManager.AppSettings["brokerip"],
                    UserName = username,
                    Password = password
                }.CreateConnection();
                _connection.ConnectionShutdown += _connection_ConnectionShutdown;

                _channel = _connection.CreateModel();

                #region setup
                
                _channel.QueueBind(responseQueueName, clientExchangeName, "response");

                var qResponseMsgConsumer = new EventingBasicConsumer(_channel);
                qResponseMsgConsumer.Received += qResponseMsgConsumer_Received;

                _channel.BasicConsume(responseQueueName, false, qResponseMsgConsumer);

                #endregion
            }
            catch (Exception ex)
            {
                log.Error(ex);
                ret = false;
            }

            return ret;
        }

        private void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (e.ReplyCode == RabbitMQ.Client.Framing.Constants.ConnectionForced)
            {
                log.Info(e.ReplyText);

                if(null != OnConnectionClosedByServerEventHandler)
                {
                    OnConnectionClosedByServerEventHandler();
                }
            }
        }

        void qResponseMsgConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                IBasicProperties props = e.BasicProperties;
                _channel.BasicAck(e.DeliveryTag, false);

                Task.Factory.StartNew(() =>
                {
                    if(props.Type != null && props.Type.Equals("orderUpdate", StringComparison.OrdinalIgnoreCase))
                    {
                        if (null != OnOrderUpdateReceived)
                        {
                            GenericRequestResponseDictionary update = JsonConvert.DeserializeObject<GenericRequestResponseDictionary>(ASCIIEncoding.UTF8.GetString(body));
                            OnOrderUpdateReceived(this, new GenericResponseEventArgs() { GenericResponse = update });
                        }
                    }
                    else if (props.Type != null && props.Type.Equals("update", StringComparison.OrdinalIgnoreCase))
                    {
                        if (null != OnUpdateReceived)
                        {
                            ClientUpdateEventArgs update = JsonConvert.DeserializeObject<ClientUpdateEventArgs>(ASCIIEncoding.UTF8.GetString(body));
                            if(null == update.Positions)
                            {
                                update.Positions = new Dictionary<Guid, TradePosition>();
                            }
                            //for net calculation
                            Dictionary<string, List<TradePosition>> netPosition = new Dictionary<string, List<TradePosition>>(update.Positions.Count);

                            //fill in the current price against each of the positions
                            foreach (var p in update.Positions)
                            {
                                if (p.Value.OrderType.Equals(TradePosition.ORDER_TYPE_BUY))
                                {
                                    p.Value.CurrentPrice = (update.ClientQuotes.ContainsKey(p.Value.Commodity)) ? update.ClientQuotes[p.Value.Commodity].Bid : decimal.Zero;
                                }
                                else {
                                    p.Value.CurrentPrice = (update.ClientQuotes.ContainsKey(p.Value.Commodity)) ? update.ClientQuotes[p.Value.Commodity].Ask : decimal.Zero;
                                }

                                string key = p.Value.Commodity + "_" + p.Value.OrderType;
                                if (!netPosition.ContainsKey(key))
                                {
                                    netPosition[key] = new List<TradePosition>();
                                }
                                netPosition[key].Add(p.Value);
                            }

                            //determine net position
                            update.NetPosition = new Dictionary<Guid, TradePosition>();
                            foreach (var row in netPosition)
                            {
                                string commodity = row.Key.Split(new char[] { '_' })[0];
                                string type = row.Key.Split(new char[] { '_' })[1];

                                decimal sumAmt = 0;
                                decimal sumPL = 0;
                                decimal price = 0;

                                foreach(var p in row.Value)
                                {
                                    sumAmt += p.Amount;
                                    sumPL += p.CurrentPl;
                                    price += p.OpenPrice;
                                }

                                price /= (decimal)row.Value.Count;

                                TradePosition position = new TradePosition();
                                position.OrderId = Guid.NewGuid();
                                position.Commodity = commodity;
                                position.OrderType = type;
                                position.OpenPrice = price;
                                position.Amount = sumAmt;
                                position.CurrentPl = sumPL;
                                if (type.Equals(TradePosition.ORDER_TYPE_BUY)) {
                                    position.CurrentPrice = update.ClientQuotes[commodity].Bid;
                                }
                                else
                                {
                                    position.CurrentPrice = update.ClientQuotes[commodity].Ask;
                                }

                                update.NetPosition.Add(position.OrderId, position);

                            }

                            //update net prices on the net positions

                            OnUpdateReceived(this, update);
                        }
                    }
                    else if (props.Type != null && props.Type.Equals("accountStatus", StringComparison.OrdinalIgnoreCase))
                    {
                        if(null != OnAccountStatusEventReceived)
                        {
                            GenericRequestResponseDictionary update = JsonConvert.DeserializeObject<GenericRequestResponseDictionary>(ASCIIEncoding.UTF8.GetString(body));
                            OnAccountStatusEventReceived(this, new GenericResponseEventArgs() { GenericResponse = update });
                        }
                    }
                    else if (props.Type != null && props.Type.Equals("candlestick", StringComparison.OrdinalIgnoreCase))
                    {
                        if (null != OnCandleStickDataEventHandler)
                        {
                            List<CandleStick> data = JsonConvert.DeserializeObject<List<CandleStick>>(ASCIIEncoding.UTF8.GetString(body));
                            CandleStickDataEventArgs args = new CandleStickDataEventArgs() { Data = data };
                            OnCandleStickDataEventHandler(this, args);
                        }
                    }
                    else if (props.Type != null && props.Type.Equals("notification"))
                    {
                        string payload = ASCIIEncoding.UTF8.GetString(body);
                        if (null != OnNotificationReceived)
                        {
                            OnNotificationReceived(payload);
                        }
                    }


                });
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        public void acceptRequote(Guid orderid)
        {
            if(null == _channel || !_channel.IsOpen)
            {
                return;
            }

            IBasicProperties props = _channel.CreateBasicProperties();
            props.UserId = _username;
            props.Type = "requote";

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic["orderid"] = orderid.ToString();

            _channel.BasicPublish(
                        clientExchangeName,
                        "request",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic))
                    );
        }

        public void requestTrade(TradeRequest request)
        {
            if (null == _channel || !_channel.IsOpen)
            {
                return;
            }

            IBasicProperties props = _channel.CreateBasicProperties();
            props.UserId = _username;
            props.Type = "trade";

            _channel.BasicPublish(
                        clientExchangeName,
                        "request",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
                    );
        }

        public void requestTradeClosure(Guid orderid)
        {
            if (null == _channel || !_channel.IsOpen)
            {
                return;
            }

            IBasicProperties props = _channel.CreateBasicProperties();
            props.UserId = _username;
            props.Type = "tradeClosure";

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic["orderid"] = orderid.ToString();

            _channel.BasicPublish(
                        clientExchangeName,
                        "request",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic))
                    );
        }


        public void requestCandleStickChartData(string commodity)
        {
            if (null == _channel || !_channel.IsOpen)
            {
                return;
            }

            IBasicProperties props = _channel.CreateBasicProperties();
            props.UserId = _username;
            props.Type = "candlestick";

            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic["commodity"] = commodity;
            dic["resolution"] = "hourly";
            dic["samples"] = "30";

            _channel.BasicPublish(
                        clientExchangeName,
                        "request",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic))
                    );
        }

    }
}
