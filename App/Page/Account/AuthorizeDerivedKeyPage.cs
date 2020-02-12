using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Cryptography;
using Heleus.Base;
using Heleus.Transactions;
using Heleus.Network.Client;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class AuthorizeDerivedKeyPage : ChainInfoBasePage
    {
        string _derivedPassword;
        EditorRow _export;
        KeyView _keyView;

        protected override void QueryDone(int chainId)
        {
            _export.Edit.Text = null;
            _keyView.Update(null);

            UIApp.Run(async () =>
            {
                var coreAccount = WalletApp.CurrentCoreAccount;
                if (WalletApp.IsCoreAccountUnlocked)
                {
                    var secretKey = await PassphraseSecretKeyInfo.NewPassphraseSecretKey(chainId, $"{Hex.ToString(coreAccount.RawData)}.{coreAccount.AccountId}");
                    var key = await Task.Run(() => Key.GenerateEd25519(secretKey.SecretHash));
                    var encryption = await Task.Run(() => Encryption.GenerateAes256(key.Data, _derivedPassword));

                    _keyView.Update(key);
                }
            });
        }

        protected override void PreAddRows()
        {
            AddTextRow("ChainInfoBasePage.ServiceInfo").Label.ColorStyle = Theme.InfoColor;
        }

        protected override void PostAddRows()
        {
            base.PostAddRows();
        }

        protected override async Task Submit(ButtonRow button)
        {
            IsBusy = true;

            var password = _password?.Edit?.Text;
            if (!WalletApp.IsCoreAccountUnlocked)
            {
                if (!await WalletApp.UnlockCoreAccount(password))
                {
                    await ErrorAsync("PasswordWrong");
                    IsBusy = false;

                    return;
                }
            }

            var chainId = int.Parse(_chainIdText.Edit.Text);
            var coreAccount = WalletApp.CurrentCoreAccount;
            var secretKey = await PassphraseSecretKeyInfo.NewPassphraseSecretKey(chainId, $"{Hex.ToString(coreAccount.RawData)}.{coreAccount.AccountId}");
            var key = await Task.Run(() => Key.GenerateEd25519(secretKey.SecretHash));
            var encryption = await Task.Run(() => Encryption.GenerateAes256(key.Data, _derivedPassword));

            _keyView.Update(key);

            if (!await ConfirmAsync("AuthorizeConfirm"))
            {
                IsBusy = false;
                return;
            }

            (var response, var publicKey) = await WalletApp.JoinChain(chainId, 0, key.PublicKey, password);

            if (response.TransactionResult == TransactionResultTypes.Ok || response.TransactionResult == TransactionResultTypes.AlreadyJoined)
            {
                var derivedKey = new DerivedKey(coreAccount.AccountId, chainId, publicKey.KeyIndex, encryption);

                var hex = Hex.ToCrcString(derivedKey.ToByteArray());
                _export.Edit.Text = hex;
                UIApp.CopyToClipboard(hex);

                await MessageAsync("SuccessDerived");
            }
            else
            {
                await ErrorTextAsync(response.GetErrorMessage());
            }

            IsBusy = false;
        }

        public AuthorizeDerivedKeyPage(int chainId, string derivedPassword) : base(chainId, "JoinChainPage")
        {
            _derivedPassword = derivedPassword;

            SetupPage();
            AddSubmitSection();

            AddHeaderRow("PrivatePublicKey");

            _keyView = new KeyView(null, true);
            AddViewRow(_keyView);

            _export = AddEditorRow(null, "DerivedKeyInfo");
            _export.SetDetailViewIcon(Icons.Info);
            AddInfoRow("DerivedInfo");
            AddFooterRow();
        }
    }
}
