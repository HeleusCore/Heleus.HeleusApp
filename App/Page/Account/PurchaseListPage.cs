using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Chain.Core;
using Heleus.Chain.Purchases;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class PurchaseListPage : StackPage
    {
        readonly BuyPurchasePage _purchasePage;

        async Task SetPurchase(ButtonViewRow<PurchaseInfoView> button)
        {
            var purchaseInfo = button.Tag as PurchaseInfo;
            _purchasePage.SetPurchaseId(purchaseInfo.PurchaseItemId);
            await Navigation.PopAsync();
        }

        public PurchaseListPage(BuyPurchasePage purchasePage, ChainInfo chainInfo) : base("PurchaseListPage")
        {
            _purchasePage = purchasePage;

            AddTitleRow("Title");

            AddHeaderRow("Purchases");

            var list = new List<PurchaseInfo>();
            foreach(var purchase in chainInfo.GetChainPurchases())
            {
                if (!purchase.IsRevoked)
                    list.Add(purchase.Item);
            }

            if (list.Count > 0)
            {
                list.Sort((a, b) => a.PurchaseItemId.CompareTo(b.PurchaseItemId));
                foreach(var item in list)
                {
                    var view = new PurchaseInfoView(item);
                    AddButtonViewRow(view, SetPurchase).Tag = item;
                }
            }
            else
            {
                AddTextRow("NoPurchases");
            }

            AddFooterRow();
        }
    }
}
