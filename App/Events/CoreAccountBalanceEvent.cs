namespace Heleus.Apps.HeleusApp
{
    public class CoreAccountBalanceEvent
    {
        public readonly long Balance;

        public CoreAccountBalanceEvent(long balance)
        {
            Balance = balance;
        }
    }
}
