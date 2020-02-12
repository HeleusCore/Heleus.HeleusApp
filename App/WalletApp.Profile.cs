using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Network.Client;
using Heleus.ProfileService;
using Heleus.Transactions;
using Heleus.Transactions.Features;
using TinyJson;

namespace Heleus.Apps.HeleusApp
{
    public static partial class WalletApp
    {
        public static async Task UploadProfileData(byte[] imageData, List<ProfileItemJson> profileItems)
        {
            HeleusClientResponse result = null;
            Index index = null;

            var attachements = Client.NewAttachementsWithCoreAccount(ProfileServiceInfo.ChainId);
            if (imageData != null)
            {
                attachements.AddBinaryAttachement(ProfileServiceInfo.ImageFileName, imageData);
                index = ProfileServiceInfo.ImageIndex;
            }

            if (profileItems != null)
            {
                var json = profileItems.ToJson();
                attachements.AddStringAttachement(ProfileServiceInfo.ProfileJsonFileName, json);
                index = ProfileServiceInfo.ProfileIndex;

                if (imageData != null)
                    index = ProfileServiceInfo.ProfileAndImageIndex;
            }

            result = await Client.UploadAttachementsWithCoreAccount(attachements, (transaction) =>
            {
                transaction.PrivacyType = DataTransactionPrivacyType.PublicData;
                transaction.EnableFeature<AccountIndex>(AccountIndex.FeatureId).Index = index;
            });

            await UIApp.PubSub.PublishAsync(new ProfileUploadEvent(CurrentCoreAccount.AccountId, ProfileServiceInfo.ChainId, result));

            if (result.TransactionResult == TransactionResultTypes.Ok)
            {
                var transaction = result.Transaction as AttachementDataTransaction;

                UIApp.Run(async () => {
                    await Task.Delay(3000);
                    await ProfileManager.Current.GetProfileData(CurrentAccountId, ProfileDownloadType.ForceDownload, true);
                });
            }
        }
    }
}
