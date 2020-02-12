using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp.Page.Account
{
	public class RegisterPage : StackPage
	{
		readonly EntryRow _name;
		readonly EntryRow _password1;
        readonly SelectionRow<AuthorizeType> _selectionRow;
        readonly EditorRow _keyRow;

        public RegisterPage() : base("RegisterPage")
		{
            EnableStatus();

			Subscribe<CoreAccountRegisterEvent>(AccountRegistered);

			AddTitleRow("Title");

            AddHeaderRow("NewAccount");

            _name = AddEntryRow("", "Name");
            _name.SetDetailViewIcon(Icons.Pencil);
            _password1 = AddPasswordRow("", "Password1");
            var password2 = AddPasswordRow("", "Password2");

            Status.Add(_password1.Edit, T("PasswordStatus", WalletApp.MinPasswordLength), (sv, entry, newText, oldText) =>
            {
                var pw1 = _password1.Edit.Text;
                var pw2 = password2.Edit.Text;

                return (WalletApp.IsValidPassword(pw1) && WalletApp.IsValidPassword(pw2) && pw1 == pw2);
            }).
            AddBusyView(password2);

            AddFooterRow();


            AddHeaderRow();

            AddLinkRow("Common.DataLicence", Tr.Get("Link.DataLicence"));
            var _agree = AddSwitchRow("Agree", Tr.Get("Common.DataLicence"));
            AddFooterRow();

            Status.

            Add(_agree.Switch, T("AgreeStatus", Tr.Get("Common.DataLicence")), StatusValidators.IsToggled);

            password2.Edit.TextChanged += (sender, e) =>
            {
                Status.ReValidate();
            };

			AddSubmitRow("Register", Register);

            AddHeaderRow("SignatureKeyInfo");

            _selectionRow = AddSelectionRows(new SelectionItemList<AuthorizeType>
            {
                new SelectionItem<AuthorizeType>(AuthorizeType.Random, Tr.Get("AuthorizeType.Random")),
                new SelectionItem<AuthorizeType>(AuthorizeType.Passphrase, Tr.Get("AuthorizeType.Passphrase"))
            }, AuthorizeType.Random);
            _selectionRow.SelectionChanged = SecretTypeChanged;

            AddSeparatorRow();

            _keyRow = AddEditorRow("", "SignatureKey");
            _keyRow.SetDetailViewIcon(Icons.Key);

            Status.Add(_keyRow.Edit, T("AuthorizeAccountPage.KeyStatus"), (sv, edit, newText, oldText) =>
            {
                return StatusValidators.HexValidator(64, sv, edit, newText, oldText);
            });

            AddInfoRow("AuthorizeAccountPage.SignatureKeyInfo");

            AddFooterRow();

            AddHeaderRow();
            AddLinkRow("Common.TermsOfUse", Tr.Get("Link.TermsOfUse"));
            AddLinkRow("Common.Privacy", Tr.Get("Link.Privacy"));
            AddFooterRow();

            IsBusy = true;
            Update();
        }

        async Task AccountRegistered(CoreAccountRegisterEvent registerEvent)
		{
            IsBusy = false;

			if (registerEvent.Account != null)
			{
                await MessageAsync("Success");
				await Navigation.PopAsync();
			}
			else
			{
				await ErrorTextAsync(registerEvent.Response.GetErrorMessage());
			}
		}

		Task Register(ButtonRow button)
		{
            IsBusy = true;

            var seed = Hex.FromString(_keyRow.Edit.Text);
            var key = Key.GenerateEd25519(seed);

            var name = _name.Edit.Text;
			var password = _password1.Edit.Text;

            UIApp.Run(() => WalletApp.RegisterCoreAccount(name, password, key));

			return Task.CompletedTask;
		}

        void Update()
        {
            RemoveView(GetRow("AuthorizeAccountPage.NewRandom"));
            RemoveView(GetRow("AuthorizeAccountPage.PassphraseInfo"));
            RemoveView(GetRow("AuthorizeAccountPage.NewPassphrase"));
            RemoveView(GetRow("AuthorizeAccountPage.Passphrase"));

            _keyRow.Edit.Text = string.Empty;
            Status.ReValidate();


            AddIndex = GetRow("SignatureKey");
            AddIndexBefore = true;

            var type = _selectionRow.Selection;
            if (type == AuthorizeType.Random)
            {
                AddButtonRow("AuthorizeAccountPage.NewRandom", NewRandom);

                _ = NewRandom(null);
            }
            else if (type == AuthorizeType.Passphrase)
            {
                var entry = AddEntryRow(null, "AuthorizeAccountPage.Passphrase", ServiceNodeManager.MinimumServiceAccountPassphraseLength);

                var button = AddButtonRow("AuthorizeAccountPage.NewPassphrase", NewPassphrase);
                button.IsEnabled = false;

                entry.Edit.TextChanged += (a, evt) =>
                {
                    button.IsEnabled = evt.NewTextValue.Length >= ServiceNodeManager.MinimumServiceAccountPassphraseLength;
                };

                AddInfoRow("AuthorizeAccountPage.PassphraseInfo", ServiceNodeManager.MinimumServiceAccountPassphraseLength);
            }
        }

        async Task NewRandom(ButtonRow arg)
        {
            var button = GetRow("AuthorizeAccountPage.NewRandom");

            IsBusy = true;
            button.IsEnabled = false;

            var secretKey = await RandomSecretKeyInfo.NewRandomSecretKey(Protocol.CoreChainId);
            _keyRow.Edit.Text = Hex.ToString(secretKey.SecretHash);
            Status.ReValidate();

            button.IsEnabled = true;
            IsBusy = false;
        }

        async Task NewPassphrase(ButtonRow arg)
        {
            var button = GetRow("AuthorizeAccountPage.NewPassphrase");

            IsBusy = true;
            button.IsEnabled = false;

            var newEdit = GetRow<EntryRow>("AuthorizeAccountPage.Passphrase");

            var secretKey = await PassphraseSecretKeyInfo.NewPassphraseSecretKey(Protocol.CoreChainId, newEdit.Edit.Text);
            _keyRow.Edit.Text = Hex.ToString(secretKey.SecretHash);
            Status.ReValidate();

            button.IsEnabled = true;
            IsBusy = false;
        }

        Task SecretTypeChanged(AuthorizeType obj)
        {
            Update();
            return Task.CompletedTask;
        }
    }
}
