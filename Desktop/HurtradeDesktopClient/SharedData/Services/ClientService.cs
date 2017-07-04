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

namespace SharedData.Services
{
    public delegate void UpdateReceivedHandler(object sender, ClientUpdateEventArgs e);
    public delegate void OrderUpdateEventHandler(object sender, GenericResponseEventArgs e);
    public delegate void AccountStatusEventHandler(object sender, GenericResponseEventArgs e);
    public delegate void CandleStickDataEventHandler(object sender, CandleStickDataEventArgs e);

    public class ClientService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IModel _channel = null;
        private static ClientService _instance = null;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;

        public event UpdateReceivedHandler OnUpdateReceived;
        public event OrderUpdateEventHandler OnOrderUpdateReceived;
        public event AccountStatusEventHandler OnAccountStatusEventReceived;
        public event CandleStickDataEventHandler OnCandleStickDataEventHandler;

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

            _instance._channel.Close();
            _instance._channel = null;
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
                
                _channel = new ConnectionFactory() {
                    AutomaticRecoveryEnabled = true,
                    HostName = System.Configuration.ConfigurationManager.AppSettings["brokerip"],
                    UserName = username,
                    Password = password
                }.CreateConnection().CreateModel();

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

                    
                });
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void requestTrade(TradeRequest request)
        {
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
