using System;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Purchases;

namespace Heleus.Apps.HeleusApp.Views
{
    public class PurchaseInfoView : RowView
    {
        readonly ExtLabel _desc;
        readonly ExtLabel _price;
        readonly ExtLabel _type;
        readonly ExtLabel _dur;

        public PurchaseInfoView(PurchaseInfo purchase = null) : base("ChainItemView")
        {
            (_, _desc) = AddRow("Description", "");
            (_, _price) = AddRow("Price", "");
            _price.ColorStyle = Theme.InfoColor;
            _price.FontStyle = Theme.RowTitleFont;

            (_, _type) = AddRow("PurchaseType", "");
            //if (purchase.PurchaseType == PurchaseTypes.Subscription)
            {
                (_, _dur) = AddLastRow("Duration", "");
            }

            Update(purchase);
        }

        public void Reset()
        {
            _desc.Text = "-";
            _price.Text = "-";
            _type.Text = "-";
            _dur.Text = "-";
        }

        public void Update(PurchaseInfo purchase)
        {
            if (purchase != null)
            {
                _desc.Text = purchase.Description;
                _price.Text = Currency.ToString(purchase.Price);
                _type.Text = Tr.Get("PurchaseTypes." + purchase.PurchaseType);
                _dur.Text = (purchase.PurchaseType == PurchaseTypes.Subscription) ? Time.ToDays(purchase.Duration).ToString() : "-";
            }
            else
            {
                Reset();
            }
        }
    }
}
