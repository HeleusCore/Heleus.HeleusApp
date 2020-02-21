using System;
using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Chain.Core;

namespace Heleus.Apps.HeleusApp.Views
{
    public class ChainInfoView : RowView
    {
        readonly ExtLabel _account;
        readonly ExtLabel _name;
        readonly ExtLabel _website;
        readonly ExtLabel _revenue;

        public ChainInfoView(ChainInfo chainInfo = null) : base("ChainInfoView")
        {
            (_, _name) = AddRow("Name", "");
            (_, _website) = AddRow("Website", "");
            (_, _revenue) = AddRow("Revenue", "");
            (_, _account) = AddLastRow("Account", "");

            Update(chainInfo);
        }

        public void Reset()
        {
            _account.Text = "-";
            _name.Text = "-";
            _website.Text = "-";
            _revenue.Text = "-";
        }

        public void Update(ChainInfo chainInfo)
        {
            if(chainInfo != null)
            {
                _account.Text = chainInfo.AccountId.ToString();
                _name.Text = chainInfo.Name;
                _website.Text = !string.IsNullOrEmpty(chainInfo.Website) ? chainInfo.Website : "-";

                var revenue = 0;
                var revenueInfo = chainInfo.CurrentRevenueInfo;
                if (revenueInfo != null)
                    revenue = revenueInfo.AccountRevenueFactor * revenueInfo.Revenue;

                _revenue.Text = Currency.ToString(revenue);
            }
            else
            {
                Reset();
            }
        }
    }
}
