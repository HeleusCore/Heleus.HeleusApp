using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    class ExportKeyCommand : Command
    {
        public const string CommandName = "exportkey";
        public const string CommandDescription = "Exports a key. Returns the key on success.";

        string keytype;
        string keypassword;

        long accountid;
        int chainid;
        int keyindex;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(keytype), "The key type (coreaccount, serviceaccount, chainkey)"),
                new KeyValuePair<string, string>(nameof(keypassword), "The key password"),
                new KeyValuePair<string, string>(nameof(accountid), "The account id"),
                new KeyValuePair<string, string>(nameof(chainid), "The chain id (not required for core accounts)"),
                new KeyValuePair<string, string>(nameof(keyindex), "The key index (not required for core accounts)")
            };
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            keytype = arguments.String(nameof(keytype), null);
            keypassword = arguments.String(nameof(keypassword), null);
            accountid = arguments.Long(nameof(accountid), -1);
            chainid = arguments.Integer(nameof(chainid), -1);
            keyindex = arguments.Integer(nameof(keyindex), -1);

            return (keytype == "coreaccount" || keytype == "serviceaccount" || keytype == "chainkey") && WalletApp.IsValidPassword(keypassword);
        }

        protected override Task Run()
        {
            ClientAccount account = null;

            var client = WalletApp.Client;
            if (keytype == "coreaccount")
            {
                var coreAccounts = client.GetCoreAccounts(false);
                foreach (var a in coreAccounts)
                {
                    if (a.AccountId == accountid)
                    {
                        account = a;
                        break;
                    }
                }
            }
            else
            {
                List<ClientAccount> accounts = null;
                if (keytype == "serviceaccount")
                    accounts = client.GetChainAccounts(chainid, false);
                else if (keytype == "chainkey")
                    accounts = client.GetChainKeys(chainid, false);

                if (accounts != null)
                {
                    foreach (var a in accounts)
                    {
                        if (a.AccountId == accountid && a.KeyIndex == (short)keyindex)
                        {
                            account = a;
                            break;
                        }
                    }
                }
            }

            if (account == null)
            {
                SetError("Key not found");
            }
            else if (!account.CanDecryptKey(keypassword))
            {
                SetError("Key password is wrong");
            }
            else
            {
                SetSuccess(account.HexString);
            }

            return Task.CompletedTask;
        }
    }
}
