using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    public class JoinChainEvent
    {
        public readonly long AccountId;
        public readonly int ChainId;
        public readonly HeleusClientResponse Response;

        public JoinChainEvent(long accountId, int chainId, HeleusClientResponse response)
        {
            AccountId = accountId;
            ChainId = chainId;
            Response = response;
        }
    }
}
