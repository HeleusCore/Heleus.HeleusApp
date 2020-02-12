using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    public class CoreAccountUnlockedEvent : CoreAccountEvent
    {
        public CoreAccountUnlockedEvent(CoreAccountKeyStore account) : base(account)
        {
        }
    }
}
