using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    public class CoreAccountTransferEvent
    {
        public readonly HeleusClientResponse Response;

        public CoreAccountTransferEvent(HeleusClientResponse response)
        {
            Response = response;
        }
    }
}
