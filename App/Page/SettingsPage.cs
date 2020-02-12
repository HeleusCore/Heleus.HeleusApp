using System;
using System.Threading.Tasks;
using Heleus.Base;
using Xamarin.Forms;

namespace Heleus.Apps.Shared
{
    class SettingsPage : SettingsPageBase
    {
        readonly Chain.Index _notificationIndex = Chain.Index.New().Add((short)1).Build();

        protected override void AddPushNotificationSectionExtras()
        {
            var sw = AddSwitchRow("PNTransfer");
            sw.Switch.IsToggled = UIApp.Current.IsPushChannelSubscribed(_notificationIndex);
            sw.Switch.ToggledAsync = Toggled;
            AddSeparatorRow();
        }

        async Task Toggled(ExtSwitch swt)
        {
            IsBusy = true;

            await UIApp.Current.ChangePushChannelSubscription(this, _notificationIndex);
            swt.SetToogle(UIApp.Current.IsPushChannelSubscribed(_notificationIndex));

            IsBusy = false;
        }

        public SettingsPage()
        {
            AddTitleRow("Title");

            AddHeaderRow().Label.Text = Tr.Get("App.FullName");

            AddButtonRow(HandleRequestPage.HandleRequestTranslation, async (button) =>
            {
                await Navigation.PushAsync(new HandleRequestPage());
            }).SetDetailViewIcon(HandleRequestPage.HandleRequestIcon);

            AddButtonRow("About", async (button) =>
            {
                await Navigation.PushAsync(new AboutPage());
            }).SetDetailViewIcon(Icons.Info);

            AddFooterRow();

            AddAppInfoSection(AppInfoType.Heleus);

            AddPushNotificationSection();

            AddThemeSection();

#if DEBUG
            AddButtonRow("Icons", async (button) =>
            {
                await Navigation.PushAsync(new IconsPage());
            });

            AddButtonRow("Crash", async (button) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    string bla = null;
                    Log.Write(bla.Length);
                });
                await Task.Delay(0);
            });
#endif
        }
    }
}
