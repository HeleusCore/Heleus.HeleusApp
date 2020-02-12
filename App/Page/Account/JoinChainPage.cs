using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class JoinChainPage : ChainInfoBasePage
    {
        readonly bool _useCoreAccount;
        readonly Key _chainKey;

        protected override async Task Submit(ButtonRow button)
        {
            try
            {
                if (!await ConfirmAsync("AuthorizeConfirm"))
                    return;

                var password = _password?.Edit?.Text;
                var chainId = int.Parse(_chainIdText.Edit.Text);

                IsBusy = true;
                if (!_useCoreAccount)
                {
                    // TODO Join Key Expire
                    UIApp.Run(() => WalletApp.JoinChain(chainId, 0, _chainKey.PublicKey, password));
                }
                else
                {
                    UIApp.Run(() => WalletApp.JoinChainWithCoreAccount(chainId, password));
                }
            }
            catch { }
        }

        protected virtual async Task Joined(JoinChainEvent joinEvent)
        {
            IsBusy = false;

            if (_chainId > 0)
                _chainIdText.Edit.IsEnabled = false;

            var result = joinEvent.Response.TransactionResult;
            var valid = _useCoreAccount ? result == TransactionResultTypes.Ok || result == TransactionResultTypes.AlreadyJoined : result == TransactionResultTypes.Ok;

            if (valid)
            {
                await MessageAsync("Success");
                //await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(joinEvent.Response.GetErrorMessage());
            }
        }

        protected override void PreAddRows()
        {
            AddTextRow("ChainInfoBasePage.ServiceInfo").Label.ColorStyle = Theme.InfoColor;
        }

        public JoinChainPage(int chainId = 0, Key chainKey = null) : base(chainId, "JoinChainPage")
		{
            _useCoreAccount = chainKey == null;
            Subscribe<JoinChainEvent>(Joined);

            SetupPage();
            AddSubmitSection();

            if (!_useCoreAccount)
            {
                _chainKey = chainKey;

                AddHeaderRow("PublicKey");
                var keyView = new KeyView(chainKey, false);
                AddViewRow(keyView);
                AddFooterRow();
            }
        }
	}
}
