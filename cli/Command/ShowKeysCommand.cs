using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Network.Client;

namespace Heleus.Apps.HeleusApp
{
    class ShowKeysCommand : Command
    {
        public const string CommandName = "showkeys";
        public const string CommandDescription = "Shows all available keys.";

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>> { };
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            return true;
        }

        void AddAccount(StringBuilder result, ClientAccount account)
        {
            var name = (account.Name ?? string.Empty).PadRight(20);

            result.AppendLine($" Name: {name} Account Id {account.AccountId.ToString().PadRight(10)} Chain Id {account.ChainId.ToString().PadRight(10)} Key Index {account.KeyIndex.ToString().PadRight(10)}");
        }

        protected override Task Run()
        {
            var client = WalletApp.Client;

            var result = new StringBuilder();

            var coreAccounts = client.GetCoreAccounts(false);

            if (coreAccounts.Count > 0)
            {
                result.AppendLine("Core Accounts");
                result.AppendLine("-------------");

                foreach (var account in coreAccounts)
                {
                    AddAccount(result, account);
                }
            }

            var chainAccouns = client.GetChainAccounts(false);
            if (chainAccouns.Count > 0)
            {
                result.AppendLine("Service Accounts");
                result.AppendLine("----------------");

                foreach (var account in chainAccouns)
                {
                    AddAccount(result, account);
                }
            }

            var chainKeys = client.GetChainKeys(false);
            if (chainKeys.Count > 0)
            {
                result.AppendLine("Chain Keys");
                result.AppendLine("----------");

                foreach (var account in chainKeys)
                {
                    AddAccount(result, account);
                }
            }


            SetSuccess(result.ToString());

            return Task.CompletedTask;
        }
    }
}
