using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.ProfileService;

namespace Heleus.Apps.HeleusApp.Page.Profile
{
    public class ProfileItemPage : StackPage
    {
        readonly EditProfilePage _editProfilePage;
        readonly ProfileItemJson _profileItem;

        readonly EntryRow _key;
        readonly EntryRow _value;

        async Task Submit(ButtonRow button)
        {
            _profileItem.k = _key.Edit.Text;
            _profileItem.v = _value.Edit.Text;

            _editProfilePage.AddUpdateProfileItem(_profileItem);

            await Navigation.PopAsync();
        }

        public ProfileItemPage(EditProfilePage editProfilePage, ProfileItemJson profileItem) : base("ProfileItemPage")
        {
            _editProfilePage = editProfilePage;
            _profileItem = profileItem;

            AddTitleRow("Title");

            AddHeaderRow();

            _key = AddEntryRow(profileItem.k, "Key");
            _key.SetDetailViewIcon(Icons.Pencil);
            _value = AddEntryRow(profileItem.v ?? (profileItem.IsWebSite() ? "https://" : ""), profileItem.IsWebSite() ? "WebsiteValue" : "MailValue");
            _value.SetDetailViewIcon(profileItem.IsWebSite() ? Icons.RowLink : Icons.At);

            Status.Add(_key.Edit, T("KeyStatus"), (sv, edit, newText, oldText) =>
            {
                return !string.IsNullOrWhiteSpace(newText);
            }).Add(_value.Edit, profileItem.IsWebSite() ? T("WebsiteStatus") : T("MailStatus"), (sv, edit, newText, oldText) =>
            {
                if (profileItem.IsWebSite())
                    return newText.IsValdiUrl(false);
                if (profileItem.IsMail())
                    return newText.IsValidMail(false);

                return false;
            });

            AddFooterRow();

            AddSubmitRow("Common.Submit", Submit);
        }
    }
}
