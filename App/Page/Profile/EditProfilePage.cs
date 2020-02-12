using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Network.Client;
using Heleus.Transactions;
using Heleus.ProfileService;
using Xamarin.Forms;

namespace Heleus.Apps.HeleusApp.Page.Profile
{
    public class EditProfilePage : StackPage
    {
        ProfileDataResult _profileData;

        ImageRow _image;
        byte[] _imageData;

        EntryRow _profileName;
        EntryRow _realName;
        EntryRow _bio;

        async Task SelectImage(ButtonRow button)
        {
            await ImageSelectionPage.OpenImagePicker(async (img) =>
            {
                byte[] imgData;
                if (img.Width > ProfileServiceInfo.ImageMaxDimensions || img.Height > ProfileServiceInfo.ImageMaxDimensions)
                {
                    using (var resize = await img.Resize(ProfileServiceInfo.ImageMaxDimensions))
                    {
                        imgData = await resize.Save(70);
                    }
                }
                else
                {
                    imgData = await img.Save(70);
                }

                _imageData = imgData;
                _image.ImageView.Source = ImageSource.FromStream(() => new MemoryStream(_imageData));
            });
        }

        enum ProfileItemActions
        {
            Cancel,
            MoveUp,
            MoveDown,
            Edit,
            Delete,
            Invoke
        }

        async Task<ProfileItemActions> DisplayProfileItemAction()
        {
            var cancel = Tr.Get("Common.Cancel");
            var moveUp = Tr.Get("Common.MoveUp");
            var moveDown = Tr.Get("Common.MoveDown");
            var edit = Tr.Get("Common.Edit");
            var delete = Tr.Get("Common.Delete");
            var invoke = T("Invoke");

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, moveUp, moveDown, edit, delete, invoke);
            if (result == moveUp)
                return ProfileItemActions.MoveUp;
            if (result == moveDown)
                return ProfileItemActions.MoveDown;
            if (result == edit)
                return ProfileItemActions.Edit;
            if (result == delete)
                return ProfileItemActions.Delete;
            if (result == invoke)
                return ProfileItemActions.Invoke;
            return ProfileItemActions.Cancel;
        }

        async Task ProfileItemSelect(ButtonRow button)
        {
            var result = await DisplayProfileItemAction();
            if (result == ProfileItemActions.Edit)
                await Navigation.PushAsync(new ProfileItemPage(this, button.Tag as ProfileItemJson));
            else if (result == ProfileItemActions.Delete)
            {
                Status.RemoveBusyView(button);
                RemoveView(button);
            }
            else if (result == ProfileItemActions.MoveUp || result == ProfileItemActions.MoveDown)
            {
                var idx = StackLayout.Children.IndexOf(button);
                if (idx > 0)
                {
                    var hasNext = (StackLayout.Children[idx + 1] as StackRow)?.Tag is ProfileItemJson;
                    var hasPrev = (StackLayout.Children[idx - 1] as StackRow)?.Tag is ProfileItemJson;

                    if (hasNext && result == ProfileItemActions.MoveDown)
                    {
                        StackLayout.Children.RemoveAt(idx);
                        StackLayout.Children.Insert(idx + 1, button);
                    }
                    else if (hasPrev && result == ProfileItemActions.MoveUp)
                    {
                        StackLayout.Children.RemoveAt(idx);
                        StackLayout.Children.Insert(idx - 1, button);
                    }
                }
            }
            else if (result == ProfileItemActions.Invoke)
            {
                await ProfilePageSections.ProfileItemHandler(button);
            }
        }

        enum AddActions
        {
            Cancel,
            Mail,
            Website
        }

        async Task<AddActions> DisplayAddAction()
        {
            var cancel = Tr.Get("Common.Cancel");
            var mail = Tr.Get("Common.Email");
            var website = Tr.Get("Common.Website");

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, mail, website);
            if (result == mail)
                return AddActions.Mail;
            if (result == website)
                return AddActions.Website;
            return AddActions.Cancel;
        }

        async Task AddItem(ButtonRow button)
        {
            var result = await DisplayAddAction();
            if (result == AddActions.Mail)
                await Navigation.PushAsync(new ProfileItemPage(this, new ProfileItemJson { p = ProfileItemJson.MailItem}));
            else if (result == AddActions.Website)
                await Navigation.PushAsync(new ProfileItemPage(this, new ProfileItemJson { p = ProfileItemJson.WebSiteItem }));
        }

        async Task Submit(ButtonRow button)
        {
            var profileName = _profileName.Edit.Text;
            var realName = _realName.Edit.Text;
            var bio = _bio.Edit.Text;
            var items = new List<ProfileItemJson>();

            if (!string.IsNullOrEmpty(profileName))
                items.Add(new ProfileItemJson { k = ProfileItemJson.ProfileNameItem, v = profileName, p = ProfileItemJson.ProfileNameItem });
            if(!string.IsNullOrEmpty(realName))
                items.Add(new ProfileItemJson { k = ProfileItemJson.RealNameItem, v = realName, p = ProfileItemJson.RealNameItem });
            if (!string.IsNullOrEmpty(bio))
                items.Add(new ProfileItemJson { k = ProfileItemJson.BioItem, v = bio, p = ProfileItemJson.BioItem });

            var rows = GetHeaderSectionRows("EditSection");
            foreach(var row in rows)
            {
                if (row.Tag is ProfileItemJson profileItem)
                    items.Add(profileItem);
            }

            if (ProfileItemJson.ListsEqual(items, _profileData.ProfileJsonItems))
                items = null;

            if(items == null && _imageData == null)
            {
                await MessageAsync("NoChanges");
                return;
            }

            IsBusy = true;
            UIApp.Run(() => WalletApp.UploadProfileData(_imageData, items));
        }

        async Task ProfileUploded(ProfileUploadEvent uploadEvent)
        {
            IsBusy = false;

            if(uploadEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(uploadEvent.Response.GetErrorMessage());
            }
        }

        public EditProfilePage() : base("EditProfilePage")
        {
            Subscribe<ProfileUploadEvent>(ProfileUploded);
            AddTitleRow("Title");
            Loading = true;
        }

        public override async Task InitAsync()
        {
            await Setup(await ProfileManager.Current.GetProfileData(WalletApp.CurrentAccountId, ProfileDownloadType.ForceDownload, true));
            Loading = false;
        }

        public void AddUpdateProfileItem(ProfileItemJson profileItem)
        {
            var rows = GetHeaderSectionRows("EditSection");
            foreach(var row in rows)
            {
                if(row.Tag == profileItem)
                {
                    var button = row as ButtonRow;
                    button.SetMultilineText(profileItem.k, profileItem.v);
                    return;
                }
            }

            AddIndex = GetRow<ButtonRow>("AddItemButton");
            AddIndexBefore = true;

            AddProfileButton(profileItem);

            AddIndex = null;
        }

        void AddProfileButton(ProfileItemJson profileItem)
        {
            if (profileItem.p == ProfileItemJson.BioItem || profileItem.p == ProfileItemJson.ProfileNameItem || profileItem.p == ProfileItemJson.RealNameItem)
                return;

            var button = AddButtonRow(null, ProfileItemSelect);
            button.SetMultilineText(profileItem.k, profileItem.v);
            button.Tag = new ProfileItemJson(profileItem);

            Status.AddBusyView(button);
        }

        void SetupEdit(ProfileDataResult profileData)
        {
            _profileData = profileData;

            AddHeaderRow("ImageSection");
            _image = AddImageRow(1, "Image");
            if (profileData.Image != null)
                _image.ImageView.Source = ImageSource.FromStream(() => new MemoryStream(profileData.Image));
            else
                _image.ImageView.Source = AccountDummyImage.ImageSource;

            var sel = AddButtonRow("SelectImage", SelectImage);
            sel.SetDetailViewIcon(Icons.UserCircle);
            Status.AddBusyView(sel);
            AddFooterRow();

            AddHeaderRow("EditSection");

            _profileName = AddEntryRow(ProfileItemJson.GetItemValue(profileData.ProfileJsonItems, ProfileItemJson.ProfileNameItem), "ProfileName");
            _profileName.SetDetailViewIcon(Icons.FullName);

            _realName = AddEntryRow(ProfileItemJson.GetItemValue(profileData.ProfileJsonItems, ProfileItemJson.RealNameItem), "RealName");
            _realName.SetDetailViewIcon(Icons.UserAlt);

            _bio = AddEntryRow(ProfileItemJson.GetItemValue(profileData.ProfileJsonItems, ProfileItemJson.BioItem), "Bio");
            _bio.SetDetailViewIcon(Icons.Info);

            Status.Add(_profileName.Edit, T("ProfileNameStatus"), (sv, edit, newValue, oldVAlue) =>
            {
                var valid = ProfileServiceInfo.IsProfileNameValid(newValue);
                if (!valid)
                    edit.Text = ProfileServiceInfo.ToValidProfileName(newValue);
                return valid;
            });
            Status.Add(_realName.Edit, T("RealNameStatus"), (sv, edit, newValue, oldVAlue) =>
            {
                return ProfileServiceInfo.IsRealNameValid(newValue);
            });

            Status.Add(_bio.Edit, T("BioStatus"), StatusValidators.NoEmptyString);

            if(profileData.ProfileJsonItems != null)
            {
                foreach (var item in profileData.ProfileJsonItems)
                    AddProfileButton(item);
            }

            Status.AddBusyView(AddButtonRow("AddItemButton", AddItem));

            AddFooterRow();

            AddSubmitRow("Common.Submit", Submit);
        }

        Task Setup(ProfileDataResult profileData)
        {
            if (profileData.ImageResult != ProfileDownloadResult.NetworkError && profileData.ProfileJsonResult != ProfileDownloadResult.NetworkError)
            {
                SetupEdit(profileData);
                return Task.CompletedTask;
            }

            AddHeaderRow("TimeoutSection");
            AddTextRow("TimeoutText");
            AddFooterRow();

            return Task.CompletedTask;
        }
    }
}
