using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.poco.updates
{
    public class ActiveConnections
    {
        private String username;
        List<ConnectionInfo> connections = new List<ConnectionInfo>();

        public string Username { get => username; set => username = value; }
        public List<ConnectionInfo> Connections { get => connections; set => connections = value; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            ActiveConnections c = obj as ActiveConnections;
            if (c == null)
            {
                return false;
            }

            return c.username.Equals(this.username);
        }

        public bool Equals(ActiveConnections c)
        {
            if (c == null)
            {
                return false;
            }

            return c.username.Equals(this.username);
        }
        
        public override int GetHashCode()
        {
            return username.GetHashCode();
        }
    }
}
