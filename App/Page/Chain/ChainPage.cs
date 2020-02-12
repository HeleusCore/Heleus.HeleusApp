using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Chain.Core;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp.Page.Chain
{
    public enum ChainItemAction
    {
        Edit,
        Delete,
        Revoke,
        Cancel
    }

    public enum ChainItemStatus
    {
        New,
        Revoked,
        Delete,
        Live
    }

    public class ChainItem<T>
    {
        public ChainItemStatus Status;
        public readonly T Item;

        public ChainItem(ChainItemStatus status, T item)
        {
            Status = status;
            Item = item;
        }
    }

    public class ChainKeyItem : ChainItem<PublicChainKey>
    {
        public Key Key;
        public string Name;
        public string Password;

        public ChainKeyItem(ChainItemStatus status, PublicChainKey item) : base(status, item)
        {
            if (item == null)
                throw new ArgumentException("SignedPublicKey is null", nameof(item));
        }
    }

    public class ChainPage : StackPage
    {
        public readonly ChainInfo ChainInfo;

        readonly ChainKeyStore _chainKey;

        readonly List<ChainItem<string>> _endPoints = new List<ChainItem<string>>();
        readonly List<ChainKeyItem> _chainKeys = new List<ChainKeyItem>();
        readonly List<ChainItem<PurchaseInfo>> _purchases = new List<ChainItem<PurchaseInfo>>();

        readonly EntryRow _name;
        readonly EntryRow _website;

        readonly ButtonRow _endPointsButton;
        readonly ButtonRow _chainKeysButton;
        //readonly ButtonRow _purchasesButton;

        readonly IStatusViewItem _adminKeyStatus;
        readonly IStatusViewItem _endPointStatus;

        readonly EntryRow _keyPassword;

        async Task NewChainKey(ButtonRow button)
        {
            await Navigation.PushAsync(new ChainKeyPage(this, _chainKeys));
        }

        async Task NewEndPoint(ButtonRow button)
        {
            await Navigation.PushAsync(new EndpointPage(this, _endPoints));
        }

        async Task NewPurchase(ButtonRow button)
        {
            await Navigation.PushAsync(new PurchasePage(this, _purchases));
        }

        Task Submit(ButtonRow button)
        {
            var name = _name.Edit.Text;
            var webSite = _website.Edit.Text;

            var keys = new List<WalletClient.ChainKey>();
            var revokedKeys = new List<short>();
            var endPoints = new List<Uri>();
            var removedEndPoints = new List<Uri>();
            var purchases = new List<PurchaseInfo>();
            var revokedPurchases = new List<int>();

            foreach (var chainKey in _chainKeys)
            {
                if (chainKey.Status == ChainItemStatus.New)
                {
                    keys.Add(new WalletClient.ChainKey(chainKey.Key, chainKey.Item, chainKey.Name, chainKey.Password));
                }
                else if (chainKey.Status == ChainItemStatus.Revoked)
                {
                    revokedKeys.Add(chainKey.Item.KeyIndex);
                }
            }

            foreach (var endPoint in _endPoints)
            {
                if (endPoint.Status == ChainItemStatus.New)
                {
                    endPoints.Add(new Uri(endPoint.Item));
                }
                else if (endPoint.Status == ChainItemStatus.Revoked)
                {
                    removedEndPoints.Add(new Uri(endPoint.Item));
                }
            }

            foreach (var purchase in _purchases)
            {
                if (purchase.Status == ChainItemStatus.New)
                {
                    purchases.Add(purchase.Item);
                }
                else if (purchase.Status == ChainItemStatus.Revoked)
                {
                    revokedPurchases.Add(purchase.Item.PurchaseItemId);
                }
            }

            if (ChainInfo == null)
            {
                _ = WalletApp.RegisterChain(name, webSite, keys.ToArray(), endPoints.ToArray(), purchases.ToArray());
            }
            else
            {
                if(keys.Count == 0 && endPoints.Count == 0 && purchases.Count == 0 && revokedKeys.Count == 0 && removedEndPoints.Count == 0 && revokedPurchases.Count == 0)
                {
                    Message("NoChanges");
                    return Task.CompletedTask;
                }

                var password = _keyPassword.Edit.Text;
                _ = WalletApp.UpdateChain(_chainKey, password, name, webSite, keys.ToArray(), endPoints.ToArray(), purchases.ToArray(), revokedKeys.ToArray(), removedEndPoints.ToArray(), revokedPurchases.ToArray());
            }

            IsBusy = true;

            return Task.CompletedTask;
        }

        async Task Chain(ChainRegistrationEvent chainEvent)
        {
            IsBusy = false;

            if (chainEvent.ChainOperation != null)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
                await ErrorTextAsync(chainEvent.Response.GetErrorMessage());
        }

        public ChainPage(ChainInfo chainInfo, ChainKeyStore chainKey) : base("ChainPage")
        {
            ChainInfo = chainInfo;
            _chainKey = chainKey;
            Subscribe<ChainRegistrationEvent>(Chain);

            if(chainInfo != null)
            {
                var endPoints = chainInfo.GetPublicEndpoints();
                foreach (var endPoint in endPoints)
                    _endPoints.Add(new ChainItem<string>(ChainItemStatus.Live, endPoint));
                foreach(var key in chainInfo.GetRevokeableChainKeys())
                    _chainKeys.Add(new ChainKeyItem(key.IsRevoked ? ChainItemStatus.Revoked : ChainItemStatus.Live, key.Item));
                foreach (var purchase in chainInfo.GetChainPurchases())
                    _purchases.Add(new ChainItem<PurchaseInfo>(purchase.IsRevoked ? ChainItemStatus.Revoked : ChainItemStatus.Live, purchase.Item));
            }

            AddTitleRow("Title");

            AddHeaderRow("Info");
            _name = AddEntryRow(null, "Name");
            _name.SetDetailViewIcon(Icons.Pencil);
            if (chainInfo != null)
                _name.Edit.Text = chainInfo.Name;
            _website = AddEntryRow(null, "Website");
            _website.SetDetailViewIcon(Icons.RowLink);
            if (chainInfo != null)
                _website.Edit.Text = chainInfo.Website;
            AddFooterRow();

            AddHeaderRow("ChainKeys");
            _chainKeysButton = AddButtonRow("ChainKeysButton", NewChainKey);
            Status.AddBusyView(_chainKeysButton);
            AddFooterRow();

            AddHeaderRow("EndPoints");
            _endPointsButton = AddButtonRow("EndPointsButton", NewEndPoint);
            Status.AddBusyView(_endPointsButton);
            AddFooterRow();

            /*
            AddHeaderRow("Purchases");
            _purchasesButton = AddButtonRow("PurchasesButton", NewPurchase);
            Status.AddBusyView(_purchasesButton);
            AddFooterRow();
            */

            Status.Add(_name.Edit, T("NameStatus"), (view, entry, newText, oldTex) =>
            {
                if (string.IsNullOrEmpty(newText))
                    return false;

                return true;
            }).
            Add(_website.Edit, T("WebsiteStatus"), (view, entry, newText, oldText) => 
            {
                if (string.IsNullOrEmpty(newText))
                    return true;

                return newText.IsValdiUrl(true);
            });

            _adminKeyStatus = Status.Add(T("AdminKeyStatus"), (sv) =>
            {
                foreach (var key in _chainKeys)
                {
                    if ((key.Status == ChainItemStatus.Live || key.Status == ChainItemStatus.New) && ((key.Item.Flags & PublicChainKeyFlags.ChainAdminKey) != 0))
                    {
                        return true;
                    }
                }

                return false;
            });

            _endPointStatus = Status.Add(T("EndPointStatus"), (sv) =>
            {
                return _endPoints.Any((a) => a.Status == ChainItemStatus.New || a.Status == ChainItemStatus.Live);
            });

            AddIndex = AddSubmitRow("Submit", Submit);
            AddIndexBefore = true;
            if (_chainKey != null)
            {
                _keyPassword = AddPasswordRow("", "KeyPassword");

                Status.Add(_keyPassword.Edit, T("PasswordStatus"), (sv, entry, newText, oldText) =>
                {
                    return _chainKey.IsPasswordValid(newText);
                });
            }

            AddIndex = null;
            AddIndexBefore = false;

            UpdateChainKeys();
            UpdateEndpoints();
            UpdatePurchases();
        }

        void UpdateChainKeys()
        {
            AddIndex = _chainKeysButton;
            AddIndexBefore = true;

            var buttons = GetHeaderSectionRows("ChainKeys");
            foreach(var key in _chainKeys)
            {
                var added = false;
                foreach(var button in buttons)
                {
                    if(button.Tag is ChainSignedPublicKeyView keyView && keyView.PublicKey == key)
                    {
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    var view = new ChainSignedPublicKeyView(key);
                    AddButtonViewRow(view, ChainKeyAction).Tag = view;
                }
            }


            AddIndex = null;
            _adminKeyStatus.ReValidate();
        }

        void UpdateEndpoints()
        {
            AddIndex = _endPointsButton;
            AddIndexBefore = true;

            var buttons = GetHeaderSectionRows("EndPoints");
            foreach (var endPoint in _endPoints)
            {
                var added = false;
                foreach (var button in buttons)
                {
                    if(button.Tag is ChainEndPointView endPointView && endPointView.EndPoint == endPoint)
                    {
                        added = true;
                        break;
                    }
                }

                if(!added)
                {
                    var view = new ChainEndPointView(endPoint);
                    AddButtonViewRow(view, EndPointAction).Tag = view;
                }
            }

            AddIndex = null;
            _endPointStatus.ReValidate();
        }

        void UpdatePurchases()
        {
            //AddIndex = _purchasesButton;
            AddIndexBefore = true;
            var buttons = GetHeaderSectionRows("Purchases");
            foreach(var purchase in _purchases)
            {
                var added = false;
                foreach(var button in buttons)
                {
                    if(button.Tag is ChainPurchaseView purchaseView && purchaseView.Purchase == purchase)
                    {
                        added = true;
                        break;
                    }
                }

                if(!added)
                {
                    var view = new ChainPurchaseView(purchase);
                    AddButtonViewRow(view, PurchaseAction).Tag = view;
                }
            }
        }

        async Task<ChainItemAction> ItemAction(params ChainItemAction[] itemactions)
        {
            string delete = null;
            var cancel = Tr.Get("Common.Cancel");
            var edit = Tr.Get("Common.Edit");
            var revoke = Tr.Get("Common.Revoke");
            var actions = new List<string>();

            foreach (var item in itemactions)
            {
                switch (item)
                {
                    case ChainItemAction.Delete:
                        delete = Tr.Get("Common.Delete");
                        break;

                    case ChainItemAction.Edit:
                        actions.Add(edit);
                        break;
                    case ChainItemAction.Revoke:
                        actions.Add(revoke);
                        break;
                }
            }

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, delete, actions.ToArray());
            if (result == delete)
                return ChainItemAction.Delete;
            else if (result == edit)
                return ChainItemAction.Edit;
            else if (result == revoke)
                return ChainItemAction.Revoke;

            return ChainItemAction.Cancel;
        }

        async Task ChainKeyAction(ButtonViewRow<ChainSignedPublicKeyView> button)
        {
            var view = button.Tag as ChainSignedPublicKeyView;
            var status = view.Status;

            if (status == ChainItemStatus.New && await ItemAction(ChainItemAction.Delete) == ChainItemAction.Delete)
            {
                view.PublicKey.Status = view.Status = ChainItemStatus.Delete;
                _chainKeys.Remove(view.PublicKey);
                RemoveView(button);
            }
            else if (status == ChainItemStatus.Live && await ItemAction(ChainItemAction.Revoke) == ChainItemAction.Revoke)
            {
                view.PublicKey.Status = view.Status = ChainItemStatus.Revoked;
            }

            _endPointStatus.ReValidate();
        }

        async Task EndPointAction(ButtonViewRow<ChainEndPointView> button)
        {
            var view = button.Tag as ChainEndPointView;
            var status = view.Status;

            if(status == ChainItemStatus.New && await ItemAction(ChainItemAction.Delete) == ChainItemAction.Delete)
            {
                view.EndPoint.Status = view.Status = ChainItemStatus.Delete;
                _endPoints.Remove(view.EndPoint);
                RemoveView(button);
            }
            else if (status == ChainItemStatus.Live && await ItemAction(ChainItemAction.Revoke) == ChainItemAction.Revoke)
            {
                view.EndPoint.Status = view.Status = ChainItemStatus.Revoked;
            }

            _endPointStatus.ReValidate();
        }

        async Task PurchaseAction(ButtonViewRow<ChainPurchaseView> button)
        {
            var view = button.Tag as ChainPurchaseView;
            var status = view.Status;

            if (status == ChainItemStatus.New && await ItemAction(ChainItemAction.Delete) == ChainItemAction.Delete)
            {
                view.Purchase.Status = view.Status = ChainItemStatus.Delete;
                _purchases.Remove(view.Purchase);
                RemoveView(button);
            }
            else if (status == ChainItemStatus.Live && await ItemAction(ChainItemAction.Revoke) == ChainItemAction.Revoke)
            {
                view.Purchase.Status = view.Status = ChainItemStatus.Revoked;
            }

            _endPointStatus.ReValidate();
        }

        public void AddChainKey(PublicChainKey chainKey, Key key, string name, string password)
        {
            _chainKeys.Add(new ChainKeyItem(ChainItemStatus.New, chainKey) { Key = key, Name = name, Password = password });
            UpdateChainKeys();
        }

        public void AddEndPoint(string uri)
        {
            _endPoints.Add(new ChainItem<string>(ChainItemStatus.New, uri));
            UpdateEndpoints();
        }

        public void AddPurchase(PurchaseInfo purchase)
        {
            _purchases.Add(new ChainItem<PurchaseInfo>(ChainItemStatus.New, purchase));
            UpdatePurchases();
        }
    }
}
