using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp.Page.Account
{
	public class BuyPurchasePage : ChainInfoBasePage
	{
        readonly EntryRow _purchaseIdText;
        readonly PurchaseInfoView _purchaseView;
        ButtonRow _selectPurchase;

        protected override Task Submit(ButtonRow button)
        {
            var password = _password?.Edit?.Text;
            try
            {
                var purchaseid = int.Parse(_purchaseIdText.Edit.Text);
                _ = WalletApp.Purchase(_chainInfo.ChainId, purchaseid, password);
                IsBusy = true;
            }
            catch { }

            return Task.CompletedTask;
        }

        async Task Purchased(PurchaseEvent purchaseEvent)
        {
            IsBusy = false;

            if (purchaseEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(purchaseEvent.Response.GetErrorMessage());
            }
        }

        async Task SelectPurchase(ButtonRow button)
        {
            var ci = _chainInfo;
            if (ci != null)
                await Navigation.PushAsync(new PurchaseListPage(this, ci));
        }

        readonly int _purchaseId;

        public BuyPurchasePage(int chainId = 0, int purchaseId = 0) : base(chainId, "BuyPurchasePage")
		{
            if (chainId > 0 && purchaseId > 0)
                _purchaseId = purchaseId;

            Subscribe<PurchaseEvent>(Purchased);

            SetupPage();

            AddHeaderRow("PurchaseInfo");

            _selectPurchase = AddButtonRow("SelectPurchase", SelectPurchase);
            _selectPurchase.IsEnabled = false;

            _purchaseIdText = AddEntryRow(null, "PurchaseId");
            _purchaseView = new PurchaseInfoView();
            AddViewRow(_purchaseView);

            AddFooterRow();

            Status.Add(_purchaseIdText.Edit, T("PurchaseStatus"), (sv, edit, newText, oldText) =>
            {
                if(_chainInfo != null && StatusValidators.PositiveNumberValidator(sv, edit, newText, oldText))
                {
                    if(int.TryParse(newText, out var id))
                    {
                        var purchase = _chainInfo.GetPurchase(id);
                        if(purchase != null)
                        {
                            _purchaseView.Update(purchase);
                            return true;
                        }
                    }
                }
                _purchaseView.Reset();
                return false;
            });

            AddSubmitSection();
		}

        protected override void ChainInfoChanged()
        {
            if(_selectPurchase != null)
                _selectPurchase.IsEnabled = _chainInfo != null;
            if (_purchaseIdText != null && _chainInfo == null)
                _purchaseIdText.Edit.Text = "";
            if (_purchaseId > 0 && _purchaseIdText != null && _chainInfo != null)
                _purchaseIdText.Edit.Text = _purchaseId.ToString();
        }

        public void SetPurchaseId(int id)
        {
            _purchaseIdText.Edit.Text = id.ToString();
        }
    }
}
