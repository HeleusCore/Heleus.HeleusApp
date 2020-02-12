using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Chain.Purchases;
using Heleus.Cryptography;
using Heleus.Messages;
using Heleus.Network.Client;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp
{
    public class WalletClient : HeleusClient
    {
        public readonly Key LiveNetworkKey = Key.Restore("227046811eb831ee2303b534687b5273b331754cce3d016ad7e6538d21d1f87af7a2f2");

        public CoreAccountKeyStore CurrentCoreAccount { get; private set; }
        public ChainKeyStore CurrentChainAccount { get; private set; }

        public WalletClient(Uri endPoint, Storage storage, bool forceDefaults) : base(endPoint, Protocol.CoreChainId, storage, forceDefaults)
        {
#if DEBUG
            AddInvalidNetworkKey(LiveNetworkKey);
#else
            AddAllowedNetworkKey(LiveNetworkKey);
#endif
        }

        public List<CoreAccountKeyStore> GetCoreAccounts()
        {
            return GetStoredAccounts<CoreAccountKeyStore>(KeyStoreTypes.CoreAccount, Protocol.CoreChainId);
        }

        public List<ChainKeyStore> GetChainAccounts()
        {
            return GetStoredAccounts<ChainKeyStore>(KeyStoreTypes.Chain, 0);
        }

        public List<ChainKeyStore> GetChainAccounts(int chainId)
        {
            return GetStoredAccounts<ChainKeyStore>(KeyStoreTypes.Chain, chainId);
        }

        public async Task<bool> SetCoreAccount(CoreAccountKeyStore account, string password)
        {
            if (await UnlockAccount(account, password, KeyStoreTypes.CoreAccount))
            {
                CurrentCoreAccount = account;
            }

            return account == null || CurrentCoreAccount != null;
        }

        public async Task<bool> SetChainAccount(ChainKeyStore account, string password)
        {
            if (await UnlockAccount(account, password, KeyStoreTypes.Chain))
            {
                CurrentChainAccount = account;
            }

            return account == null || CurrentCoreAccount != null;
        }

        Task<HeleusClientResponse> SendCoreTransaction(Transaction transaction, KeyStore clientAccount, bool awaitResponse)
        {
            transaction.SignKey = clientAccount.DecryptedKey;
            return SendTransaction(transaction, awaitResponse);
        }

        public async Task<HeleusClientResponse> SendCoreTransaction(Transaction transaction, bool awaitResponse)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Sending transaction {transaction.GetType().Name} failed, no core account set.", this);
                return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
            }

            return await SendCoreTransaction(transaction, CurrentCoreAccount, awaitResponse);
        }

        public async Task<HeleusClientResponse> SendCoreTransactionWithChainKey(Transaction transaction, bool awaitResponse)
        {
            if (CurrentChainAccount == null)
            {
                Log.Trace($"Sending transaction {transaction.GetType().Name} failed, no chain account set.", this);
                return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
            }

            return await SendCoreTransaction(transaction, CurrentChainAccount, awaitResponse);
        }

        Task<HeleusClientResponse> SendDataTransactionWithCoreAccount(DataTransaction transaction, bool awaitResponse)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Sending data transaction {transaction.GetType().Name} with core account failed, no core account set.", this);
                return Task.FromResult(new HeleusClientResponse(HeleusClientResultTypes.InternalError));
            }

            return SendDataTransaction(transaction, awaitResponse, CurrentCoreAccount);
        }

        async Task<HeleusClientResponse> SendServiceTransactionWithCoreAccount(ServiceTransaction transaction, bool awaitResponse)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Sending service transaction {transaction.GetType().Name} with core account failed, no core account set.", this);
                return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
            }

            if (!await SetTargetChain(transaction.TargetChainId))
                return new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);

            transaction.SignKey = CurrentCoreAccount.DecryptedKey;
            return await SendTransaction(transaction, awaitResponse);
        }

        public CoreAccountKeyStore GetRegisteredCoreAccount(HeleusClientResponse response)
        {
            if (response != null)
            {
                if (response.Transaction is AccountOperation accountOperation)
                {
                    var accounts = GetCoreAccounts();
                    foreach (var account in accounts)
                    {
                        if (account.AccountId == accountOperation.AccountId)
                        {
                            return account;
                        }
                    }
                }
            }

            return null;
        }

        public async Task<bool> UploadErrorReportsCoreAccount(byte[] reports, int chainId)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"UploadErrorReports failed, no core account set.", this);
                return false;
            }

            var m = new ClientErrorReportMessage(CurrentCoreAccount.KeyIndex, chainId, new SignedData(reports, CurrentCoreAccount.DecryptedKey));
            var sent = await SendMessage(CurrentCoreAccount.AccountId, m);
            return sent && await WaitResponse(m) is ClientErrorReportResponseMessage;
        }

        public async Task<long> CheckBalance()
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Transfer coins failed, no core account set.", this);
                return -1;
            }

            await Connect(CurrentCoreAccount.AccountId);
            var token = ConnectionToken;
            if (token != null)
            {
                var m = new ClientBalanceMessage(new SignedData(token, CurrentCoreAccount.DecryptedKey));
                var sent = await SendMessage(CurrentCoreAccount.AccountId, m);
                if (sent && await WaitResponse(m) is ClientBalanceResponseMessage balanceReponse)
                {
                    return balanceReponse.Balance;
                }
            }
            return -1;
        }

        public async Task<HeleusClientResponse> RegisterAccount(string name, Key key, string password)
        {
            if (name.IsNullOrWhiteSpace())
                throw new ArgumentException("Name is empty.", nameof(name));
            if (password.IsNullOrWhiteSpace())
                throw new ArgumentException("Password is empty.", nameof(password));
            if (key == null)
                key = Key.Generate(Protocol.TransactionKeyType);

            if (key.KeyType != Protocol.TransactionKeyType || !key.IsPrivate)
                throw new ArgumentException("Key type is wrong.", nameof(key));

            try
            {
                var transaction = new AccountRegistrationCoreTransaction(key.PublicKey) { SignKey = key };
                var response = await SendTransaction(transaction, true);

                if (response.TransactionResult == TransactionResultTypes.AlreadyProcessed)
                {
                    var m = new ClientKeyCheckMessage(key, false);
                    var sent = await SendMessage(0, m);

                    if (sent && await WaitResponse(m) is ClientKeyCheckResponseMessage checkResponse && checkResponse.KeyCheck != null)
                    {
                        var check = checkResponse.KeyCheck;
                        if (check != null && check.ChainId == Protocol.CoreChainId)
                        {
                            var coreAccount = (await DownloadCoreAccount(check.AccountId)).Data;
                            if (coreAccount != null)
                            {
                                if (coreAccount.AccountKey == key.PublicKey)
                                {
                                    var account = await StoreAccount($"{name} ({check.AccountId})", check.AccountId, key, password);
                                    if (CurrentCoreAccount == null)
                                        await SetCoreAccount(account, password);

                                    return new HeleusClientResponse(HeleusClientResultTypes.Ok, TransactionResultTypes.Ok, new AccountOperation(account.AccountId, key, 0), 0);
                                }
                            }
                        }
                    }
                }

                var operation = response.Transaction;
                if (operation != null && operation is AccountOperation registration && registration.PublicKey == key.PublicKey)
                {
                    var accountId = registration.AccountId;
                    Log.Trace($"New account registered with id {accountId} and public key {key.PublicKey.HexString} with transaction id {operation.OperationId}.", this);

                    var account = await StoreAccount($"{name} ({accountId})", accountId, key, password);
                    if (CurrentCoreAccount == null)
                        await SetCoreAccount(account, password);

                    _ = SendMessage(accountId, new ClientInfoMessage(new ClientInfo(accountId, _clientKey)));
                }
                else
                {
                    Log.Trace($"Account registration failed.", this);
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, this);
            }

            return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
        }

        public Task<HeleusClientResponse> TransferCoins(long targetAccountId, long amount, string reason = null)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Transfer coins failed, no core account set.", this);
                return Task.FromResult(new HeleusClientResponse(HeleusClientResultTypes.InternalError));
            }

            return SendCoreTransaction(new TransferCoreTransaction(CurrentCoreAccount.AccountId, targetAccountId, amount, reason), true);
        }

        public ChainUpdateCoreTransaction NewChainUpdateTransaction(bool useChainKey)
        {
            if (useChainKey)
            {
                if (CurrentChainAccount == null)
                {
                    Log.Trace($"NewChainUpdateTransaction failed, no chain account set.", this);
                    return null;
                }
                return new ChainUpdateCoreTransaction(CurrentChainAccount.KeyIndex, CurrentChainAccount.AccountId, CurrentChainAccount.ChainId);
            }

            if (CurrentCoreAccount == null)
            {
                Log.Trace($"NewChainUpdateTransaction failed, no core account set.", this);
                return null;
            }
            return new ChainUpdateCoreTransaction(CurrentCoreAccount.KeyIndex, CurrentCoreAccount.AccountId, CurrentCoreAccount.ChainId);
        }

        public async Task<HeleusClientResponse> UpdateChain(ChainUpdateCoreTransaction transaction, ChainKey[] chainKeys)
        {
            HeleusClientResponse result = null;
            if (transaction.SignKeyIndex == Protocol.CoreAccountSignKeyIndex)
                result = await SendCoreTransaction(transaction, true);
            else
                result = await SendCoreTransactionWithChainKey(transaction, true);

            var operation = result.Transaction;
            if (operation != null && operation is ChainInfoOperation registration && registration.AccountId == CurrentCoreAccount.AccountId)
            {
                var chainId = registration.ChainId;
                if (chainKeys != null)
                {
                    for (var i = 0; i < chainKeys.Length; i++)
                    {
                        var ck = chainKeys[i];
                        await StoreAccount(ck.Name, ck.SignedPublicKey, ck.Key, ck.Password);

                        Log.Trace($"New chain account with keyindex {ck.SignedPublicKey.KeyIndex} and public key {ck.Key.PublicKey.HexString}.", this);
                    }
                }
            }
            else
            {
                Log.Trace($"Chain registration failed.", this);
            }

            return result;
        }

        public class ChainKey
        {
            public readonly PublicChainKey SignedPublicKey;
            public readonly Key Key;
            public readonly string Name;
            public readonly string Password;

            public ChainKey(Key key, PublicChainKey signedPublicKey, string name, string password)
            {
                SignedPublicKey = signedPublicKey;
                Key = key;
                Name = name;
                Password = password;
            }
        }

        public async Task<HeleusClientResponse> RegisterChain(string chainName, string chainWebsite, ChainKey[] chainKeys, Uri[] endPoints, PurchaseInfo[] purchases)
        {
            if (CurrentCoreAccount == null)
            {
                Log.Trace($"Register new chain failed, no core account set.", this);
                throw new Exception("CurrentCoreAccount is null");
            }

            if (!chainWebsite.IsValdiUrl())
                throw new ArgumentException("Invalid uri.", nameof(chainWebsite));

            if (endPoints == null || endPoints.Length == 0)
                throw new ArgumentException("EndPoints is invalid.", nameof(endPoints));

            if (chainKeys == null || chainKeys.Length == 0)
                throw new ArgumentException("ChainKeys required", nameof(chainKeys));

            var found = false;
            foreach (var ck in chainKeys)
            {
                if (!ck.Key.IsPrivate)
                    throw new ArgumentException("ChainKeys must be private", nameof(chainKeys));

                //if (ck.Password.IsNullOrWhiteSpace())
                //    throw new ArgumentException("Password is empty.", nameof(chainKeys));

                if ((ck.SignedPublicKey.Flags & PublicChainKeyFlags.ChainAdminKey) != 0)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                throw new ArgumentException("ChainKeys need one admin key.", nameof(chainKeys));

            try
            {
                var accountId = CurrentCoreAccount.AccountId;
                var transaction = new ChainRegistrationCoreTransaction(chainName, chainWebsite, accountId);
                foreach (var endPoint in endPoints)
                    transaction.AddPublicEndpoint(endPoint.AbsoluteUri);

                for (var i = 0; i < chainKeys.Length; i++)
                {
                    var ck = chainKeys[i];
                    transaction.AddChainKey(ck.SignedPublicKey);
                }

                if (purchases != null)
                {
                    foreach (var purchase in purchases)
                        transaction.AddPurchase(purchase);
                }

                var result = await SendCoreTransaction(transaction, true);
                var operation = result.Transaction;
                if (operation != null && operation is ChainInfoOperation registration && registration.AccountId == CurrentCoreAccount.AccountId)
                {
                    var chainId = registration.ChainId;
                    Log.Trace($"New chain registered with id {chainId} with transaction id {operation.OperationId}.", this);
                    for (var i = 0; i < chainKeys.Length; i++)
                    {
                        var ck = chainKeys[i];
                        await StoreAccount(ck.Name, ck.SignedPublicKey, ck.Key, ck.Password);

                        Log.Trace($"New chain account with keyindex {ck.SignedPublicKey.KeyIndex} and public key {ck.Key.PublicKey.HexString}.", this);
                    }
                }
                else
                {
                    Log.Trace($"Chain registration failed.", this);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex, this);
            }

            return new HeleusClientResponse(HeleusClientResultTypes.InternalError);
        }

        public async Task<HeleusClientResponse> JoinChain(int chainId, PublicServiceAccountKey publicKey, Key chainKey = null, string serviceKeyName = null, string password = null)
        {
            if (chainId < Protocol.CoreChainId || (publicKey != null && publicKey.ChainId != chainId))
                throw new ArgumentException("ChainId is invalid.", nameof(chainId));

            if (CurrentCoreAccount == null)
            {
                Log.Warn($"Join chain failed, no core account set.", this);
                throw new Exception("CurrentCoreAccount is null");
            }

            HeleusClientResponse result = null;
            try
            {
                if (!await SetTargetChain(chainId))
                {
                    result = new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);
                    goto end;
                }

                JoinServiceTransaction joinTransaction = null;

                if (publicKey != null)
                    joinTransaction = new JoinServiceTransaction(publicKey);
                else
                    joinTransaction = new JoinServiceTransaction(CurrentCoreAccount.AccountId, chainId);

                result = await SendServiceTransactionWithCoreAccount(joinTransaction, true);
                var operation = result.Transaction;
                if (operation != null && operation is JoinServiceTransaction join)
                {
                    var keyIndex = publicKey != null ? publicKey.KeyIndex : Protocol.CoreAccountSignKeyIndex;
                    Log.Trace($"Joined chain {chainId} with accountid {CurrentCoreAccount.AccountId} and keyindex {keyIndex} with transaction id {join.OperationId}.", this);
                    if (chainKey != null && chainKey.IsPrivate && chainKey.PublicKey == publicKey.PublicKey && !string.IsNullOrEmpty(serviceKeyName) && !string.IsNullOrEmpty(password))
                    {
                        var account = await StoreAccount(serviceKeyName, publicKey, chainKey, password);
                        if (CurrentServiceAccount == null)
                            await SetServiceAccount(account, password, false);
                    }
                }
                else
                {
                    Log.Trace($"Joining chain {chainId} failed.", this);
                }

            }
            catch (Exception ex)
            {
                Log.HandleException(ex, this);
                result = new HeleusClientResponse(HeleusClientResultTypes.InternalError);
            }

        end:
            return result;
        }

        public Task<HeleusClientResponse> PurchaseItem(int chainId, int purchaseId)
        {
            return PurchaseItem(CurrentCoreAccount.AccountId, chainId, purchaseId);
        }

        public async Task<HeleusClientResponse> PurchaseItem(long receiverAccountId, int chainId, int purchaseId)
        {
            if (!await SetTargetChain(chainId))
                return new HeleusClientResponse(HeleusClientResultTypes.EndpointConnectionError);

            var purchase = ChainInfo.GetPurchase(purchaseId);
            if (purchase == null)
                return new HeleusClientResponse(HeleusClientResultTypes.Ok, TransactionResultTypes.PurchaseNotFound, 0);

            var purchaseTransaction = new PurchaseServiceTransaction(receiverAccountId, purchase.PurchaseGroupId, purchase.PurchaseItemId, purchase.Price, CurrentCoreAccount.AccountId, chainId);

            return await SendServiceTransactionWithCoreAccount(purchaseTransaction, true);
        }

        public Attachements NewAttachementsWithCoreAccount(int chainId)
        {
            return new Attachements(CurrentCoreAccount.AccountId, chainId, 0);
        }

        public Task<HeleusClientResponse> UploadAttachementsWithCoreAccount(Attachements attachements, Action<AttachementDataTransaction> setupCallback)
        {
            return UploadDataAttachements(attachements, CurrentCoreAccount, setupCallback);
        }
    }
}
