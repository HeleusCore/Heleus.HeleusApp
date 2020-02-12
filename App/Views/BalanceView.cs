using Heleus.Apps.Shared;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp.Views
{
    public class BalanceView : RowView
    {
        readonly ExtLabel _balance;

        public BalanceView() : base("BalanceView")
        {
            (_, _balance) = AddLastRow("Balance", "-");

            _balance.ColorStyle = Theme.InfoColor;
            _balance.FontStyle = Theme.RowTitleFont;
        }

        public void Reset()
        {
            _balance.Text = "-";
        }

        public void UpdateBalance(long balance)
        {
            _balance.Text = Currency.ToString(balance);
        }
    }
}
