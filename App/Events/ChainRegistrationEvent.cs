using Heleus.Chain.Core;
using Heleus.Network.Client;
using Heleus.Operations;

namespace Heleus.Apps.HeleusApp
{
    public struct ChainRegistrationEvent
    {
        public readonly HeleusClientResponse Response;
        public readonly ChainInfoOperation ChainOperation;
        public readonly ChainInfo ChainInfo;

        public ChainRegistrationEvent(HeleusClientResponse response, ChainInfo chainInfo)
        {
            ChainOperation = response.Transaction as ChainInfoOperation;
            Response = response;
            ChainInfo = chainInfo;
        }
    }
}
