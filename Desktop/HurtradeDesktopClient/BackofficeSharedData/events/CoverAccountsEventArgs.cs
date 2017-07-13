using BackofficeSharedData.poco.updates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackofficeSharedData.events
{
    public class CoverAccountsEventArgs : EventArgs
    {
        List<CoverAccount> _coverAccounts;

        public List<CoverAccount> CoverAccounts { get => _coverAccounts; set => _coverAccounts = value; }
    }
}
