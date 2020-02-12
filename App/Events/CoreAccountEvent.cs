using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Apps.HeleusApp
{
    public class CoreAccountEvent
    {
        public readonly CoreAccountKeyStore Account;

        protected CoreAccountEvent(CoreAccountKeyStore account)
        {
            Account = account;
        }
    }
}
