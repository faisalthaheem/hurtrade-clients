using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharedData.poco
{
    [JsonDictionary]
    public class GenericRequestResponseDictionary : Dictionary<string, string>
    {
        [JsonIgnore]
        private const string KEY_TYPE = "type";
        [JsonIgnore]
        private const string TYPE_ENDPOINT_RESOLUTION = "endpointResolution";
        
        public void SetIsEndpointRes()
        {
            this[KEY_TYPE] = TYPE_ENDPOINT_RESOLUTION;
        }
    }
}
