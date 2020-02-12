using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{

    public class CoreAccountRegisterEvent : AccountEvent
    {
        public readonly HeleusClientResponse Response;

        public CoreAccountRegisterEvent(HeleusClientResponse response, CoreAccountKeyStore account) : base(account)
        {
            Response = response;
        }
    }
}
