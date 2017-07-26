using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using SharedData.poco;
using SharedData.events;
using System.Text;

namespace SharedData.Services
{
    public delegate void GenericResponseReceivedEventHandler(object sender, GenericResponseEventArgs e);

    public class AuthService
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IModel _channel = null;
        private static AuthService _instance = null;
        private string responseQueueName = string.Empty;

        public event GenericResponseReceivedEventHandler OnGenericResponseReceived;

        private string _username, _password;

        private AuthService() { }

        protected void RaiseOnGenericResponseReceived(GenericResponseEventArgs args)
        {
            if(null != OnGenericResponseReceived)
            {
                GenericResponseReceivedEventHandler handler = OnGenericResponseReceived;
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

        public static AuthService GetInstance()
        {
            if (null == _instance)
            {
                _instance = new AuthService();
            }

            return _instance;
        }

        public bool init(string username, string password)
        {
            bool ret = true;
            try
            {
                _channel = new ConnectionFactory() {
                    AutomaticRecoveryEnabled = true,
                    HostName = System.Configuration.ConfigurationManager.AppSettings["brokerip"],
                    UserName = username,
                    Password = password
                }.CreateConnection().CreateModel();

                #region setup


                //create a temporary queue to receive the response on
                var qResponse = _channel.QueueDeclare("", false, true, true, null);
                responseQueueName = qResponse.QueueName;
                _channel.QueueBind(responseQueueName,
                    System.Configuration.ConfigurationManager.AppSettings["exchangeNameAuth"], username);

                var qResponseMsgConsumer = new EventingBasicConsumer(_channel);
                qResponseMsgConsumer.Received += qResponseMsgConsumer_Received;

                _channel.BasicConsume(responseQueueName, false, qResponseMsgConsumer);

                _username = username;
                _password = password;

                #endregion
            }
            catch (Exception ex)
            {
                log.Error(ex);
                ret = false;
            }

            return ret;
        }

        public bool ResolveUserEndpoints()
        {
            bool ret = false;

            try
            {
                if (null != _channel && _channel.IsOpen)
                {

                    var dic = new GenericRequestResponseDictionary();
                    //dic.Add("username", _username);
                    //dic.Add("password", _password);
                    dic.SetIsEndpointRes();

                    IBasicProperties props = _channel.CreateBasicProperties();
                    props.ReplyTo = responseQueueName;
                    props.UserId = _username;

                    _channel.BasicPublish(
                        System.Configuration.ConfigurationManager.AppSettings["exchangeNameAuth"],
                        "", 
                        props, 
                        UTF8Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(dic))
                    );

                    ret = true; //indiicate request sent
                }

            }catch(Exception ex)
            {
                log.Error(ex);
            }

            return ret;
        }

        void qResponseMsgConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                _channel.BasicAck(e.DeliveryTag, false);

                GenericRequestResponseDictionary dic = JsonConvert.DeserializeObject<GenericRequestResponseDictionary>(ASCIIEncoding.UTF8.GetString(body));

                Task.Factory.StartNew(() =>
                {
                    RaiseOnGenericResponseReceived(
                        new GenericResponseEventArgs() { GenericResponse = dic }
                    );
                });
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
        
    }
}
