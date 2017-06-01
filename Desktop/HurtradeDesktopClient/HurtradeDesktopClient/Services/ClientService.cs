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

namespace HurtradeDesktopClient.Services
{
    public delegate void UpdateReceivedHandler(object sender, ClientUpdateEventArgs e);

    public class ClientService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IModel _channel = null;
        private static ClientService _instance = null;
        private string officeExchangeName = string.Empty;
        private string clientExchangeName = string.Empty;
        private string responseQueueName = string.Empty;

        public event UpdateReceivedHandler OnUpdateReceived;

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

        public bool init(string username, string password, string officeExchange, string requestExchange, string responseQueue)
        {
            bool ret = true;
            try
            {
                this.officeExchangeName = officeExchange;
                this.clientExchangeName = requestExchange;
                this.responseQueueName = responseQueue;
                _username = username;
                _password = password;

                _channel = new ConnectionFactory() {
                    AutomaticRecoveryEnabled = true,
                    HostName = Properties.Settings.Default.brokerip,
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
                _channel.BasicAck(e.DeliveryTag, false);

                ClientUpdateEventArgs update = JsonConvert.DeserializeObject<ClientUpdateEventArgs>(ASCIIEncoding.UTF8.GetString(body));

                Console.WriteLine(ASCIIEncoding.UTF8.GetString(body));

                Task.Factory.StartNew(() =>
                {
                    RaiseOnUpdateReceived(update);
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

            _channel.BasicPublish(
                        clientExchangeName,
                        "request",
                        props,
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))
                    );
        }
        
    }
}
