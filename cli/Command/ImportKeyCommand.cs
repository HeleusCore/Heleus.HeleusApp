using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Cryptography;

namespace Heleus.Apps.HeleusApp
{
    class ImportKeyCommand : Command
    {
        public const string CommandName = "importkey";
        public const string CommandDescription = "Imports a key.";

        string key;
        string keypassword;

        KeyStore keyStore;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(key), "The key to import"),
                new KeyValuePair<string, string>(nameof(keypassword), "The key password")
            };
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            key = arguments.String(nameof(key), null);
            keypassword = arguments.String(nameof(keypassword), null);

            try
            {
                keyStore = KeyStore.Restore(key);
            }
            catch { }

            return keyStore != null && WalletApp.IsValidPassword(keypassword);
        }

        protected override Task Run()
        {
            Key privateKey = null;
            try
            {
                privateKey = keyStore.DecryptKey(keypassword);
            }
            catch
            {
                SetError("Key password is wrong");
                return Task.CompletedTask;
            }

            WalletApp.Client.StoreAccount(keyStore.KeyStoreType, keyStore.NetworkKey, keyStore.Name, privateKey, keyStore.AccountId, keyStore.ChainId, keyStore.Expires, keyStore.KeyIndex, keyStore.Flags, keypassword);
            SetSuccess("1");

            return Task.CompletedTask;
        }
    }
}
