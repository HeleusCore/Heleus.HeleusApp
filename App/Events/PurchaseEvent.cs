using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    public class PurchaseEvent
    {
        public readonly HeleusClientResponse Response;

        public PurchaseEvent(HeleusClientResponse response)
        {
            Response = response;
        }
    }
}
