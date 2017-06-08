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

namespace BackofficeSharedData.Services
{
    public delegate void UpdateReceivedHandler(object sender, ClientUpdateEventArgs e);
    public delegate void OfficePositionsUpdateReceivedHandler(object sender, OfficePositionsUpdateEventArgs e);

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
                
                if (messageType.Equals("officePositions")) {
                    OfficePositionsUpdate update = JsonConvert.DeserializeObject<OfficePositionsUpdate>(ASCIIEncoding.UTF8.GetString(body));

                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            OnOfficePositionsUpdateReceived(this, new OfficePositionsUpdateEventArgs(update));
                        }catch(Exception ex)
                        {
                            log.Error(ex);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        
        public void approveRejectOrder(string clientUsername, Guid orderid, string commandVerb)
        {
            GenericRequestResponseDictionary request = new GenericRequestResponseDictionary();
            request["client"] = clientUsername;
            request["orderId"] = orderid.ToString();

            lock (lockChannel)
            {
                try
                {
                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.UserId = _username;
                    props.Type = commandVerb;

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

    }
}
