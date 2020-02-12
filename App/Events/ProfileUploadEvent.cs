using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    public class ProfileUploadEvent
    {
        public readonly long AccountId;
        public readonly int ChainId;
        public readonly HeleusClientResponse Response;

        public ProfileUploadEvent(long accountId, int chainId, HeleusClientResponse response)
        {
            AccountId = accountId;
            ChainId = chainId;
            Response = response;
        }
    }
}
