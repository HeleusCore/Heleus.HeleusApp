using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class RestorePage : StackPage
    {
        readonly EntryRow _name;
        readonly EntryRow _password1;

        readonly EntryRow _accountId;
        readonly EntryRow _passphrase;
        readonly EditorRow _key;

        public RestorePage() : base("RestorePage")
        {
            EnableStatus();

            AddTitleRow("Title");

            AddHeaderRow("Account");

            _accountId = AddEntryRow(null, "AccountId");
            _accountId.SetDetailViewIcon(Icons.Coins);

            AddSeparatorRow();

            _name = AddEntryRow("", "RegisterPage.Name");
            _name.SetDetailViewIcon(Icons.Pencil);
            _password1 = AddPasswordRow("", "RegisterPage.Password1");
            var password2 = AddPasswordRow("", "RegisterPage.Password2");

            Status.AddBusyView(_accountId);
            Status.Add(_accountId.Edit, T("AccountStatus"), StatusValidators.PositiveNumberValidator);

            Status.Add(_password1.Edit, T("RegisterPage.PasswordStatus", WalletApp.MinPasswordLength), (sv, entry, newText, oldText) =>
            {
                var pw1 = _password1.Edit.Text;
                var pw2 = password2.Edit.Text;

                return (WalletApp.IsValidPassword(pw1) && WalletApp.IsValidPassword(pw2) && pw1 == pw2);
            }).
            AddBusyView(password2);

            AddFooterRow();


            AddHeaderRow("AuthorizeAccountPage.SignatureKey");

            _passphrase = AddEntryRow(null, "AuthorizeAccountPage.Passphrase", ServiceNodeManager.MinimumServiceAccountPassphraseLength);
            var button = AddButtonRow("AuthorizeAccountPage.NewPassphrase", Generate);
            Status.AddBusyView(button);

            _key = AddEditorRow(null, "AuthorizeAccountPage.SignatureKey");
            _key.SetDetailViewIcon(Icons.Key);
            Status.Add(_key.Edit, T("RestoreAccountPage.KeyStatus"), (sv, edit, newText, oldText) =>
            {
                return StatusValidators.HexValidator(64, sv, edit, newText, oldText);
            });

            AddFooterRow();


            password2.Edit.TextChanged += (sender, e) =>
            {
                Status.ReValidate();
            };

            AddSubmitRow("Restore", Restore);
        }

        async Task Generate(ButtonRow arg)
        {
            IsBusy = true;

            var secretKey = await PassphraseSecretKeyInfo.NewPassphraseSecretKey(Protocol.CoreChainId, _passphrase.Edit.Text);
            _key.Edit.Text = Hex.ToString(secretKey.SecretHash);

            Status.ReValidate();
            IsBusy = false;
        }

        async Task Restore(ButtonRow arg)
        {
            IsBusy = true;

            var seed = Hex.FromString(_key.Edit.Text);
            var key = Key.GenerateEd25519(seed);
            var accountId = long.Parse(_accountId.Edit.Text);
            var name = _name.Edit.Text;
            var password = _password1.Edit.Text;

            var result = await WalletApp.RequestRestore(accountId, name, password, key);

            IsBusy = false;

            if (result.ResultType == HeleusClientResultTypes.Ok)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(result.GetErrorMessage());
            }
        }
    }
}
