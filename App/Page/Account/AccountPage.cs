using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Cryptography;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class AccountPage : StackPage
	{
        EntryRow _unlockPassword;
        ButtonRow _unlockButton;

        BalanceView _balanceView;

        async Task Register(ButtonRow button)
        {
            await Navigation.PushAsync(new RegisterPage());
        }

        async Task Import(ButtonRow button)
        {
            await Navigation.PushAsync(new ImportAccountPage(KeyStoreTypes.CoreAccount, WalletApp.MinPasswordLength, WalletApp.ImportAccount, 1));
        }

        async Task Restore(ButtonRow button)
        {
            await Navigation.PushAsync(new RestorePage());
        }

        async Task Unlock(ButtonRow button)
        {
            IsBusy = true;
            var pw = _unlockPassword?.Edit?.Text;
            var success = await WalletApp.UnlockCoreAccount(pw);
            IsBusy = false;

            if (!success)
            {
                await MessageAsync("UnlockFailed");
            }
        }

        async Task Transfer(ButtonRow button)
        {
            await Navigation.PushAsync(new TransferPage());
        }

        async Task HandleRequest(ButtonRow button)
        {
            await Navigation.PushAsync(new HandleRequestPage());
        }

        async Task Join(ButtonRow button)
        {
            await Navigation.PushAsync(new JoinChainPage());
        }

        async Task Purchase(ButtonRow button)
        {
            await Navigation.PushAsync(new BuyPurchasePage());
        }

        async Task Profile(ButtonRow button)
        {
            await Navigation.PushAsync(new Profile.ProfilePage());
        }

        async Task Chain(ButtonRow button)
        {
            await Navigation.PushAsync(new Chain.ChainOverviewPage());
        }

        async Task DisplayKeyAction(ButtonViewRow<ClientAccountView> button)
        {
            var cancel = Tr.Get("Common.Cancel");
            var export = T("ExportButton");

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, export);
            if (result == export)
            {
                var ac = WalletApp.CurrentCoreAccount;
                if (ac != null)
                    await Navigation.PushAsync(new ExportAccountPage(ac, WalletApp.MinPasswordLength));
            }
        }

        async Task Export(ButtonRow buttonRow)
        {
            var ac = WalletApp.CurrentCoreAccount;
            if (ac != null)
                await Navigation.PushAsync(new ExportAccountPage(ac, WalletApp.MinPasswordLength));
        }

        void UnlockPasswordChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            _unlockButton.IsEnabled = WalletApp.IsValidPassword(_unlockPassword.Edit.Text);
        }

        Task BalanceUpdated(CoreAccountBalanceEvent balanceEvent)
        {
            if (_balanceView != null)
            {
                _balanceView.UpdateBalance(balanceEvent.Balance);
            }

            return Task.CompletedTask;
        }

        public AccountPage() : base("AccountPage")
		{
            IsSuspendedLayout = true;

			Subscribe<CoreAccountRegisterEvent>(AccountRegistered);
			Subscribe<CoreAccountUnlockedEvent>(AccountUnlocked);
            Subscribe<CoreAccountBalanceEvent>(BalanceUpdated);

			SetupPage();
		}

        void SetupPage()
		{
            _balanceView = null;
            _unlockButton = null;
            _unlockPassword = null;

            StackLayout.Children.Clear();

            AddTitleRow("Title");

            if (!WalletApp.HasCoreAccount)
			{
				AddRegisterSection();
			}
			else if (!WalletApp.IsCoreAccountUnlocked)
			{
				AddUnlockSection();
			}
			else
			{
				AddAccountSection();
			}

            UpdateSuspendedLayout();
		}

		void AddRegisterSection()
		{
			AddHeaderRow("Register");
            AddTextRow("RegisterInfo").Label.ColorStyle = Theme.InfoColor;
            AddLinkRow("Common.LearnMore", Tr.Get("Link.Account"));

            AddSeparatorRow();

			var reg = AddButtonRow("RegisterButton", Register);
			reg.SetDetailViewIcon(Icons.Coins);

			var imp = AddButtonRow("ImportButton", Import);
			imp.SetDetailViewIcon(Icons.CloudUpload, 25);

            var restore = AddButtonRow("RestoreButton", Restore);
            restore.SetDetailViewIcon(Icons.Retweet);

			AddFooterRow();
		}

        void AddUnlockSection()
        {
            AddHeaderRow("Unlock");

            _unlockPassword = AddPasswordRow("", "UnlockPassword");
            _unlockPassword.Edit.TextChanged += UnlockPasswordChanged;

            _unlockButton = AddSubmitRow("UnlockButton", Unlock, false);
            _unlockButton.IsEnabled = false;

            AddSeparatorRow();

            AddButtonRow("ProfilePage.Title", Profile).SetDetailViewIcon(Icons.UserCircle);
            AddButtonRow(HandleRequestPage.HandleRequestTranslation, HandleRequest).SetDetailViewIcon(HandleRequestPage.HandleRequestIcon);

            AddFooterRow();

            AddHeaderRow("Key");
            AddViewRow(new ClientAccountView(WalletApp.CurrentCoreAccount));

            AddFooterRow();
        }

        void AddAccountSection()
		{
			AddHeaderRow("Account");

            var lastBalance = WalletApp.LastBalanceEvent;

            _balanceView = new BalanceView();
            AddViewRow(_balanceView);

            AddButtonRow("Transfer", Transfer).SetDetailViewIcon(Icons.CreditCardFront);
#if DEBUG
            AddButtonRow("Derived", async (button) =>
            {
                await Navigation.PushAsync(new AuthorizeDerivedKeyPage(0, Hex.ToString(new byte[16])));
            });
            AddButtonRow("Join", Join);
            AddButtonRow("Purchase", Purchase);
#endif
            AddSeparatorRow();
            AddButtonRow("ProfilePage.Title", Profile).SetDetailViewIcon(Icons.UserCircle);
            AddButtonRow("ChainOverviewPage.Title", Chain).SetDetailViewIcon(Icons.Link);
            AddButtonRow(HandleRequestPage.HandleRequestTranslation, HandleRequest).SetDetailViewIcon(HandleRequestPage.HandleRequestIcon);

            AddFooterRow();

            AddHeaderRow("Key");
            AddButtonViewRow(new ClientAccountView(WalletApp.CurrentCoreAccount), DisplayKeyAction);
            AddSubmitButtonRow("Export", Export);

            AddFooterRow();
		}

        Task AccountRegistered(CoreAccountRegisterEvent accountRegister)
		{
			SetupPage();

            return Task.CompletedTask;
		}

		Task AccountUnlocked(CoreAccountUnlockedEvent accountUnlocked)
		{
			SetupPage();

            return Task.CompletedTask;
        }
	}
}
