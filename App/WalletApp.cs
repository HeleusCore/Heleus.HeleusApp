using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Chain.Maintain;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Network.Results;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp
{
    public class RequestRevenueEvent
    {
        public readonly HeleusClientResponse Response;
        public readonly AccountRevenueInfo RevenueInfo;
        public RequestRevenueEvent(HeleusClientResponse response, AccountRevenueInfo revenueInfo)
        {
            Response = response;
            RevenueInfo = revenueInfo;
        }
    }

    public static partial class WalletApp
    {
        static readonly string _debugEndPoint;
        static readonly string _endPoint = "https://heleusnode.heleuscore.com";

        static bool _busy;
        static ServiceNode _serviceNode;

        public static Storage DocumentStorage { get; private set; }
        public static Storage CacheStorage { get; private set; }
        public static WalletClient Client { get; private set; }
        public static HeleusClient ProfileClient { get; private set; }

        public static CoreAccountKeyStore CurrentCoreAccount { get; private set; }
        public static long CurrentAccountId => HasCoreAccount ? CurrentCoreAccount.AccountId : 0;

        public static bool HasCoreAccount => CurrentCoreAccount != null;
        public static bool IsCoreAccountUnlocked => HasCoreAccount && CurrentCoreAccount.IsDecrypted;

        public static CoreAccountBalanceEvent LastBalanceEvent;

        public static string EndPoint => _debugEndPoint ?? _endPoint;

        static CoreTransactionsDownload _transactionDownload;

        static WalletApp()
        {
            Transaction.Init();

#if DEBUG
            var debugEndPointData = EmbeddedResource.GetEmbeddedResource<UIApp>("debugendpoint.txt");
            if (debugEndPointData != null)
            {
                var preamble = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
                var endPoint = Encoding.UTF8.GetString(debugEndPointData).Trim().Replace(preamble, string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(endPoint) && endPoint.StartsWith("http", StringComparison.Ordinal) && Uri.TryCreate(endPoint, UriKind.RelativeOrAbsolute, out _))
                {
                    _debugEndPoint = endPoint;
                }
            }
#else
            _debugEndPoint = null;
#endif
        }

        public static void Init()
        {
            if (DocumentStorage == null)
            {
                DocumentStorage = new Storage(StorageInfo.DocumentStorage.RootPath);

                CacheStorage = new Storage(StorageInfo.CacheStorage.RootPath);

                Client = new WalletClient(new Uri(EndPoint), DocumentStorage, true);

                var snm = new ServiceNodeManager(Protocol.CoreChainId, new Uri(EndPoint), 1, "Core Service", null, null, UIApp.PubSub);
                _serviceNode = new ServiceNode(new Uri(EndPoint), Protocol.CoreChainId, false, "coreservice", snm, Client);
                snm.AddServiceNodeForCoreChain(_serviceNode);

                CurrentCoreAccount = Client.GetCoreAccounts().FirstOrDefault();

                _ = new ProfileManager(Client, CacheStorage, UIApp.PubSub);
                _ = ProfileManager.Current.GetProfileData(CurrentAccountId, ProfileDownloadType.QueryStoredData, false);
            }
        }

#if DEBUG
        public const int MinPasswordLength = 1;
#else
        public const int MinPasswordLength = 12;
#endif
        public static bool IsValidPassword(string password)
        {
            return !(password.IsNullOrWhiteSpace() || password.Length < MinPasswordLength);
        }

        public static async Task<bool> UnlockCoreAccount(string password)
        {
            if (CurrentCoreAccount != null)
            {
                try
                {
                    if (await Client.SetCoreAccount(CurrentCoreAccount, password))
                    {
                        await CurrentCoreAccount.DecryptKeyAsync(password, true);
                        _serviceNode.AddServiceAccountForCoreChain(CurrentCoreAccount);

                        await UIApp.PubSub.PublishAsync(new CoreAccountUnlockedEvent(CurrentCoreAccount));

                        UIApp.Run(async () =>
                        {
                            await UpdateCoreAccountBalance();

                            if (UIApp.Current.PushNotificationsEnabled)
                                await UIApp.Current.SyncPushToken(false);

                            if (UIApp.Current.SendErrorReports)
                                await UIApp.UploadErrorReports(ServiceNodeManager.Current.FirstDefaultServiceNode);
                        });

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                }
            }

            return false;
        }

        public static async Task<long> UpdateCoreAccountBalance()
        {
            if (CurrentCoreAccount == null)
                return -1;

            var balance = await Client.CheckBalance();
            if (balance >= 0)
            {
                LastBalanceEvent = new CoreAccountBalanceEvent(balance);
                await UIApp.PubSub.PublishAsync(LastBalanceEvent);
                return balance;
            }

            return -1;
        }

        public static async Task<bool> ValidateAccount(KeyStore account, string password)
        {
            try
            {
                await account.DecryptKeyAsync(password, true);

                if (account.KeyStoreType == KeyStoreTypes.CoreAccount)
                {
                    var coreAccount = (await Client.DownloadCoreAccount(account.AccountId)).Data;
                    if (coreAccount != null)
                    {
                        return coreAccount.AccountKey == account.DecryptedKey.PublicKey;
                    }
                }
                else if (account.KeyStoreType == KeyStoreTypes.Chain)
                {
                    var chainData = (await Client.DownloadChainInfo(account.ChainId)).Data;
                    if (chainData != null)
                    {
                        var key = chainData.GetChainKey(account.KeyIndex);
                        return key != null && key.PublicKey == account.DecryptedKey.PublicKey;
                    }
                }
            }
            catch
            {

            }

            return false;
        }


        public static async Task<ImportAccountResult> ImportAccount(KeyStore account, string password)
        {
            try
            {
                await account.DecryptKeyAsync(password, true);

                if (CurrentCoreAccount != null && account.KeyStoreType == KeyStoreTypes.CoreAccount)
                    return ImportAccountResult.CoreAccountAlreadyPresent;

                if (!await ValidateAccount(account, password))
                {
                    return ImportAccountResult.ValidationFailed;
                }

                await Client.StoreAccount(account);

                if (account.KeyStoreType == KeyStoreTypes.CoreAccount)
                {
                    CurrentCoreAccount = (CoreAccountKeyStore)account;
                    await UnlockCoreAccount(password);
                }

                await UIApp.PubSub.PublishAsync(new AccountImportEvent(account));

                return ImportAccountResult.Ok;
            }
            catch (Exception ex)
            {
                global::Heleus.Base.Log.IgnoreException(ex);
            }

            return ImportAccountResult.PasswordInvalid;
        }

        public static async Task<HeleusClientResponse> RegisterCoreAccount(string name, string password, Key key)
        {
            HeleusClientResponse result = null;

            if (CurrentCoreAccount != null)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.CoreAccountAlreadyAvailable);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            if (!IsValidPassword(password))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                goto end;
            }

            if (name.IsNullOrWhiteSpace())
                name = $"Heleus Account";


            if (!await Client.SetTargetChain(Protocol.CoreChainId))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            try
            {
                result = await Client.RegisterAccount(name, key, password);
                CurrentCoreAccount = Client.GetRegisteredCoreAccount(result);
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

        end:
            await UIApp.PubSub.PublishAsync(new CoreAccountRegisterEvent(result, CurrentCoreAccount));

            if (CurrentCoreAccount != null)
            {
                await UnlockCoreAccount(password);
            }

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;

            return result;
        }

        public static async Task<HeleusClientResponse> RequestRestore(long accountId, string name, string password, Key key)
        {
            HeleusClientResponse result = null;

            if (CurrentCoreAccount != null)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.CoreAccountAlreadyAvailable);
                goto end;
            }

            if (!IsValidPassword(password))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            var coreAccountDownload = await Client.DownloadCoreAccount(accountId);
            var coreAccount = coreAccountDownload.Data;

            if (name.IsNullOrWhiteSpace())
                name = $"Heleus Account ({accountId})";

            if (coreAccount != null)
            {
                if (key.PublicKey == coreAccount.AccountKey)
                {

                    var coreAccountStore = new CoreAccountKeyStore(name, accountId, key, password);
                    await coreAccountStore.DecryptKeyAsync(password, true);
                    await Client.StoreAccount(coreAccountStore);
                    CurrentCoreAccount = coreAccountStore;

                    result = new HeleusClientResponse(HeleusClientResultTypes.Ok);
                }
                else
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.RestoreInvalidSignatureKey);
                }
            }
            else
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.RestoreCoreAccountNotFound);
            }

        end:
            await UIApp.PubSub.PublishAsync(new CoreAccountRegisterEvent(result, CurrentCoreAccount));

            if (CurrentCoreAccount != null)
            {
                await UnlockCoreAccount(password);
            }

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;

            return result;
        }

        public static async Task RegisterChain(string chainName, string chainWebsite, WalletClient.ChainKey[] chainKeys, Uri[] endPoints, PurchaseInfo[] purchases)
        {
            if (CurrentCoreAccount == null)
                return;

            HeleusClientResponse result = null;
            ChainInfo chainInfo = null;
            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            try
            {
                if (!await Client.SetTargetChain(Protocol.CoreChainId))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                    goto end;
                }

                result = await Client.RegisterChain(chainName, chainWebsite, chainKeys, endPoints, purchases);
                if (result.TransactionResult == TransactionResultTypes.Ok)
                    chainInfo = (await Client.DownloadChainInfo((result.Transaction as ChainInfoOperation).ChainId)).Data;
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

        end:
            await UIApp.PubSub.PublishAsync(new ChainRegistrationEvent(result, chainInfo));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;
        }

        public static async Task UpdateChain(ChainKeyStore chainKey, string password, string chainName, string chainWebsite, WalletClient.ChainKey[] chainKeys, Uri[] endPoints, PurchaseInfo[] purchases, short[] revokedChainKeys, Uri[] revokedEndPoints, int[] revokedPurchases)
        {
            if (CurrentCoreAccount == null)
                return;


            HeleusClientResponse result = null;
            ChainInfo chainInfo = null;
            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            try
            {
                await chainKey.DecryptKeyAsync(password, true);
                await Client.SetChainAccount(chainKey, password);
            }
            catch
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                goto end;
            }

            try
            {
                var transaction = Client.NewChainUpdateTransaction(true);
                if (chainKeys != null)
                {
                    foreach (var ck in chainKeys)
                    {
                        transaction.AddChainKey(ck.SignedPublicKey);
                    }
                }
                if (endPoints != null)
                {
                    foreach (var ep in endPoints)
                    {
                        transaction.AddPublicEndpoint(ep.AbsoluteUri);
                    }
                }
                if (purchases != null)
                {
                    foreach (var p in purchases)
                    {
                        transaction.AddPurchase(p);
                    }
                }

                if (revokedChainKeys != null)
                {
                    foreach (var key in revokedChainKeys)
                        transaction.RevokeChainKey(key);
                }

                if (revokedEndPoints != null)
                {
                    foreach (var ep in revokedEndPoints)
                        transaction.RemovePublicEndPoint(ep.AbsoluteUri);
                }

                if (revokedPurchases != null)
                {
                    foreach (var p in revokedPurchases)
                        transaction.RemovePurchase(p);
                }

                transaction.UpdateChainName(chainName);
                transaction.UpdateChainWebsite(chainWebsite);

                if (!await Client.SetTargetChain(Protocol.CoreChainId))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                    goto end;
                }

                result = await Client.UpdateChain(transaction, chainKeys);
                if (result.TransactionResult == TransactionResultTypes.Ok)
                    chainInfo = (await Client.DownloadChainInfo((result.Transaction as ChainInfoOperation).ChainId)).Data;
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

        end:
            await UIApp.PubSub.PublishAsync(new ChainRegistrationEvent(result, chainInfo));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;
        }

        // TODO: Maybe add a timer to prevent refresh spamming
        public static async Task DownloadCoreAccountTransaction(bool queryOlder = false)
        {
            if (!HasCoreAccount)
                return;

            if (_busy)
                return;
            _busy = true;

            try
            {
                await _serviceNode.Client.SetTargetChain(Protocol.CoreChainId);

                if (_transactionDownload == null || _transactionDownload.AccountId != CurrentAccountId)
                    _transactionDownload = new CoreTransactionsDownload(CurrentAccountId, _serviceNode.GetTransactionDownloadManager(0));

                _transactionDownload.QueryOlder = queryOlder;

                var result = await _transactionDownload.DownloadTransactions();
                var evt = new TransactionDownloadEvent<CoreOperation>();
                evt.AddResult(result, _transactionDownload, null);
                await UIApp.PubSub.PublishAsync(evt);
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
            }

            _busy = false;
        }

        public static async Task Purchase(int chainId, int purchaseId, string accountPassword)
        {
            if (!HasCoreAccount)
                return;

            HeleusClientResponse result = null;

            if (!HasCoreAccount)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.NoCoreAccount);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            if (!IsCoreAccountUnlocked)
            {
                if (!await CurrentCoreAccount.DecryptKeyAsync(accountPassword, false))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                    goto end;
                }

                await UnlockCoreAccount(accountPassword);
            }

            if (!await Client.SetTargetChain(chainId))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            result = await Client.PurchaseItem(chainId, purchaseId);

        end:
            await UIApp.PubSub.PublishAsync(new PurchaseEvent(result));
            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;
        }

        public static async Task JoinChainWithCoreAccount(int chainId, string accountPassword)
        {
            if (!HasCoreAccount)
                return;

            HeleusClientResponse result = null;

            if (!HasCoreAccount)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.NoCoreAccount);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }

            _busy = true;

            if (!IsCoreAccountUnlocked)
            {
                if (!await CurrentCoreAccount.DecryptKeyAsync(accountPassword, false))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                    goto end;
                }

                await UnlockCoreAccount(accountPassword);
            }

            if (!await Client.SetTargetChain(chainId))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            result = await Client.JoinChain(chainId, null);

        end:
            await UIApp.PubSub.PublishAsync(new JoinChainEvent(CurrentCoreAccount.AccountId, chainId, result));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;
        }

        public static async Task<(HeleusClientResponse, PublicServiceAccountKey)> JoinChain(int chainId, long expires, Key accountChainKey, string accountPassword, string chainKeyName = null, string chainKeyPassword = null)
        {
            HeleusClientResponse result = null;
            PublicServiceAccountKey publicKey = null;

            if (!HasCoreAccount)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.NoCoreAccount);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }

            _busy = true;

            if (!IsCoreAccountUnlocked)
            {
                if (!await CurrentCoreAccount.DecryptKeyAsync(accountPassword, false))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                    goto end;
                }

                await UnlockCoreAccount(accountPassword);
            }

            if (!await Client.SetTargetChain(chainId))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            var nextKeyIndex = (await Client.DownloadNextServiceAccountKeyIndex(CurrentCoreAccount.AccountId, chainId)).Data;
            if (nextKeyIndex == null)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            if (!nextKeyIndex.IsValid)
            {
                var err = HeleusClientResultTypes.EndpointConnectionError;
                if (nextKeyIndex.ResultType == ResultTypes.AccountNotFound)
                    err = HeleusClientResultTypes.NoCoreAccount;

                result = new HeleusClientResponse(err);
                goto end;
            }

            publicKey = PublicServiceAccountKey.GenerateSignedPublicKey(CurrentCoreAccount.AccountId, chainId, expires, nextKeyIndex.Item, accountChainKey, CurrentCoreAccount.DecryptedKey);
            result = await Client.JoinChain(chainId, publicKey, accountChainKey, chainKeyName, chainKeyPassword);

        end:
            await UIApp.PubSub.PublishAsync(new JoinChainEvent(CurrentCoreAccount == null ? 0 : CurrentCoreAccount.AccountId, chainId, result));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;

            return (result, publicKey);
        }

        public static async Task<HeleusClientResponse> TransferCoins(long targetAcount, long amount, string reason, string accountPassword)
        {
            HeleusClientResponse result;

            if (!HasCoreAccount)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.NoCoreAccount);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            if (!IsCoreAccountUnlocked)
            {
                try
                {
                    await CurrentCoreAccount.DecryptKeyAsync(accountPassword, true);
                }
                catch
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.PasswordError);
                    goto end;
                }

                await UnlockCoreAccount(accountPassword);
            }

            await Client.SetTargetChain(Protocol.CoreChainId);
            result = await Client.TransferCoins(targetAcount, amount, reason);
            if (result.TransactionResult == TransactionResultTypes.Ok)
                UIApp.Run(() => UpdateCoreAccountBalance());

            end:
            await UIApp.PubSub.PublishAsync(new CoreAccountTransferEvent(result));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;

            return result;
        }

        public static async Task<HeleusClientResponse> RequestRevenue(int chainId, AccountRevenueInfo accountRevenue)
        {
            HeleusClientResponse result = null;
            if (!HasCoreAccount)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.NoCoreAccount);
                goto end;
            }

            if (_busy)
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.Busy);
                goto end;
            }
            _busy = true;

            if (!await Client.SetTargetChain(chainId))
            {
                result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                goto end;
            }

            result = await Client.RequestRevenue(chainId, accountRevenue);
            if (result.TransactionResult == TransactionResultTypes.Ok)
                UIApp.Run(() => UpdateCoreAccountBalance());

            end:

            await UIApp.PubSub.PublishAsync(new RequestRevenueEvent(result, accountRevenue));

            if (result.ResultType != HeleusClientResultTypes.Busy)
                _busy = false;

            return result;
        }
    }
}