using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.ProfileService;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp.Page.Profile
{
    public class ProfilePage : StackPage
	{
        static bool _hasDownloaded;

        async Task ViewProfile(ButtonRow button)
        {
            await Navigation.PushAsync(new SearchProfilePage(new ProfileSearch()));
        }

        async Task EditProfile(ButtonRow button)
        {
            await Navigation.PushAsync(new EditProfilePage());
        }

        async Task Join(ButtonRow button)
        {
            await Navigation.PushAsync(new Account.JoinChainPage(ProfileServiceInfo.ChainId));
        }

        public ProfilePage() : base("ProfilePage")
		{
            IsSuspendedLayout = true;

            Subscribe<CoreAccountRegisterEvent>(AccountRegistered);
            Subscribe<ProfileDataResultEvent>(ProfileAvailable);
            Subscribe<JoinChainEvent>(JoinChain);
            Subscribe<CoreAccountUnlockedEvent>(AccountUnlocked);

            SetupPage();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!_hasDownloaded)
                _ = ProfileManager.Current.GetProfileData(WalletApp.CurrentAccountId, ProfileDownloadType.ForceDownload, true);
        }

        void SetupPage()
        {
            StackLayout.Children.Clear();

            AddTitleRow("Title");

            var profileData = ProfileManager.Current.GetCachedProfileData(WalletApp.CurrentAccountId);
            ProfilePageSections.AddProfileSections(this, profileData, "Profile");

            AddHeaderRow("Profiles");

            if(WalletApp.IsCoreAccountUnlocked)
            {
                if (UIAppSettings.JoinedProfile)
                {
                    AddButtonRow("Edit", EditProfile);
                }
                else
                {
                    AddTextRow("JoinInfo").Label.ColorStyle = Theme.InfoColor;
                    AddLinkRow("Common.LearnMore", Tr.Get("Link.Profile"));
                    AddSubmitRow("Join", Join, false);
                }
            }
            else
            {
                AddTextRow("Common.Unlock").Label.ColorStyle = Theme.InfoColor;
            }

            AddButtonRow("ViewProfile", ViewProfile);
            AddFooterRow();

            UpdateSuspendedLayout();
        }

        Task ProfileAvailable(ProfileDataResultEvent profileEvent)
        {
            if(profileEvent.AccountId == WalletApp.CurrentAccountId)
            {
                switch(profileEvent.ProfileData.ProfileJsonResult)
                {
                    case ProfileDownloadResult.Available:
                        ProfilePageSections.UpdateProfileSections(this, profileEvent.ProfileData);
                        UpdateSuspendedLayout();
                        _hasDownloaded = true;
                        break;

                    case ProfileDownloadResult.NotAvailable:
                        _hasDownloaded = true;
                        break;

                    case ProfileDownloadResult.NetworkError:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        Task JoinChain(JoinChainEvent joinEvent)
        {
            if (joinEvent.ChainId == ProfileServiceInfo.ChainId)
            {
                var result = joinEvent.Response.TransactionResult;
                if (result == TransactionResultTypes.Ok || result == TransactionResultTypes.AlreadyJoined)
                {
                    UIAppSettings.JoinedProfile = true;
                    UIApp.Current.SaveSettings();

                    SetupPage();
                }

                _ = ProfileManager.Current.GetProfileData(WalletApp.CurrentAccountId, ProfileDownloadType.ForceDownload, true);
            }
            return Task.CompletedTask;
        }

        Task AccountRegistered(CoreAccountRegisterEvent registerEvent)
        {
            SetupPage();

            return Task.CompletedTask;
        }

        Task AccountUnlocked(CoreAccountUnlockedEvent unlockedEvent)
        {
            SetupPage();

            return Task.CompletedTask;
        }
    }
}
