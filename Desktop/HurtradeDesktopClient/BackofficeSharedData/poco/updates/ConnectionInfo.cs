using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class ConnectionInfo
    {
        private String username;
        private String ipaddress;
        private DateTime connectedat;
        //this name identifies the connection to rabbitmq, and is required to disconnect the user
        private String mqName;

        public string Username { get => username; set => username = value; }
        public string Ipaddress { get => ipaddress; set => ipaddress = value; }
        public DateTime Connectedat { get => connectedat; set => connectedat = value; }
        public string MqName { get => mqName; set => mqName = value; }
    }
}
