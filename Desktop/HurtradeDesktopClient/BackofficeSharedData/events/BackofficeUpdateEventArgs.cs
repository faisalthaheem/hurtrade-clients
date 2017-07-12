using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BackofficeSharedData.poco.updates
{
    public class BackofficeUpdateEventArgs : EventArgs
    {
        private BackofficeUpdate _update;
        public BackofficeUpdate OfficeUpdate { get{return _update; } }

        public BackofficeUpdateEventArgs(BackofficeUpdate update)
        {
            this._update = update;
        }
    }
}
