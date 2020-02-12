using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Purchases;

namespace Heleus.Apps.HeleusApp.Page.Chain
{
    public class PurchasePage : StackPage
    {
        readonly ChainPage _chainPage;

        readonly SelectionRow<PurchaseTypes> _type;

        readonly EntryRow _purchaseId;
        readonly EntryRow _groupId;
        readonly EntryRow _description;
        readonly EntryRow _price;
        readonly EntryRow _duration;

        public PurchasePage(ChainPage chainPage, List<ChainItem<PurchaseInfo>> purchases) : base("PurchasePage")
        {
            _chainPage = chainPage;

            AddTitleRow("Title");

            AddHeaderRow("Type");

            _type = AddSelectionRows(new SelectionItem<PurchaseTypes>[] {
                new SelectionItem<PurchaseTypes>(PurchaseTypes.Feature, Tr.Get("PurchaseTypes.Feature")),
                new SelectionItem<PurchaseTypes>(PurchaseTypes.Subscription, Tr.Get("PurchaseTypes.Subscription"))
            }, PurchaseTypes.Feature);

            AddFooterRow();

            AddHeaderRow("Ids");

            _purchaseId = AddEntryRow(null, "PurchaseId");
            _purchaseId.SetDetailViewIcon(Icons.CreditCardFront);
            _groupId = AddEntryRow(null, "GroupId");
            _groupId.SetDetailViewIcon(Icons.LayerGroup);

            AddFooterRow("IdsInfo");

            AddHeaderRow("Info");

            _description = AddEntryRow(null, "Description");
            _description.SetDetailViewIcon(Icons.AlignLeft);
            _price = AddEntryRow(null, "Price");
            _price.SetDetailViewIcon(Icons.MoneyBillAlt, 22);

            _duration = AddEntryRow(null, "Duration");
            _duration.SetDetailViewIcon(Icons.Stopwatch);

            AddFooterRow();

            Status.Add(_purchaseId.Edit, T("PurchaseIdStatus"), (sv, entry, newText, oldText) =>
            {
                if (int.TryParse(newText, out var id))
                {
                    foreach (var p in purchases)
                    {
                        if (p.Item.PurchaseItemId == id)
                            return false;
                    }
                    return true;
                }

                if (!newText.IsNullOrEmpty())
                    entry.Text = oldText;

                return false;
            }).Add(_groupId.Edit, T("GroupIdStatus"), (sv, entry, newText, oldText) =>
            {
                if (short.TryParse(newText, out var id))
                {
                    foreach (var p in purchases)
                    {
                        if (p.Item.PurchaseGroupId == id && _type.Selection != p.Item.PurchaseType)
                            return false;
                    }
                    return true;
                }

                if (!newText.IsNullOrEmpty())
                    entry.Text = oldText;

                return false;
            }).Add(_description.Edit, T("DescriptionStatus"), (sv, entry, newText, oldText) =>
            {
                return !string.IsNullOrWhiteSpace(newText);
            }).Add(_price.Edit, T("PriceStatus"), StatusValidators.HeleusCoinValidator).
            Add(_duration.Edit, T("DurationStatus"), (sv, entry, newText, oldText) =>
            {
                if (_type.Selection == PurchaseTypes.Feature)
                {
                    if (!newText.IsNullOrEmpty())
                        entry.Text = oldText;
                    return true;
                }

                return StatusValidators.PositiveNumberValidator(sv, entry, newText, oldText);
            });

            _type.SelectionChanged = (item) =>
            {
                Status.ReValidate();
                return Task.CompletedTask;
            };

            AddSubmitRow("Submit", Submit);
        }

        async Task Submit(ButtonRow button)
        {
            try
            {
                var type = _type.Selection;
                var itemId = int.Parse(_purchaseId.Edit.Text);
                var groupId = short.Parse(_groupId.Edit.Text);
                var desc = _description.Edit.Text;
                var price = decimal.Parse(_price.Edit.Text, System.Globalization.NumberStyles.AllowDecimalPoint);

                var p = Currency.ToHel(price);

                if (type == PurchaseTypes.Feature)
                    _chainPage.AddPurchase(PurchaseInfo.NewFeature(groupId, itemId, desc, p));
                else if (type == PurchaseTypes.Subscription)
                {
                    var hours = long.Parse(_duration.Edit.Text);
                    _chainPage.AddPurchase(PurchaseInfo.NewSubscription(groupId, itemId, desc, p, Time.Hours(hours)));
                }

                await Navigation.PopAsync();
            }
            catch { }
        }
    }
}
