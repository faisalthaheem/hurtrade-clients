using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BackofficeSharedData.poco.updates
{
    public class OfficePositionsUpdateEventArgs : EventArgs
    {
        private OfficePositionsUpdate _OfficePositionsUpdate;
        public OfficePositionsUpdate OfficePositionsUpdate { get{return _OfficePositionsUpdate;} }

        public OfficePositionsUpdateEventArgs(OfficePositionsUpdate update)
        {
            this._OfficePositionsUpdate = update;
        }
    }
}
