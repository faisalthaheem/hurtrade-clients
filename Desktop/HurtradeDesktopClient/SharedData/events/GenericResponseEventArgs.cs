using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedData.poco;

namespace SharedData.events
{
    public class GenericResponseEventArgs : System.EventArgs
    {
        public GenericRequestResponseDictionary GenericResponse {get;set;}
    }
}
