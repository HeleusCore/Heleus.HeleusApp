using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp
{
    class JoinCommand : Command
    {
        public const string CommandName = "join";
        public const string CommandDescription = "Join a heleus chain. Returns the keyindex on success.";

        int chainid;
        string accountpassword;
        string chainkeyname;
        string chainkeypassword;

        protected override bool Parse(ArgumentsParser arguments)
        {
            chainid = arguments.Integer(nameof(chainid), 0);
            accountpassword = arguments.String(nameof(accountpassword), null);
            chainkeyname = arguments.String(nameof(chainkeyname), null);
            chainkeypassword = arguments.String(nameof(chainkeypassword), null);

            if (chainid <= Protocol.CoreChainId || !WalletApp.IsValidPassword(accountpassword) || string.IsNullOrEmpty(chainkeyname) || !WalletApp.IsValidPassword(chainkeypassword))
                return false;

            return true;
        }

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(chainid), "The chain id"),
                new KeyValuePair<string, string>(nameof(accountpassword), "The password of the account"),
                new KeyValuePair<string, string>(nameof(chainkeyname), "The name of the chain key"),
                new KeyValuePair<string, string>(nameof(chainkeypassword), $"The password of the chain key, must be {WalletApp.MinPasswordLength} characters")
            };
        }

        protected override async Task Run()
        {
            var key = Key.Generate(Protocol.TransactionKeyType);

            var result = await WalletApp.JoinChain(chainid, 0, key, accountpassword, chainkeyname, chainkeypassword);
            if (result.TransactionResult == TransactionResultTypes.Ok)
                SetSuccess(WalletApp.Client.CurrentChainAccount?.KeyIndex.ToString());
            else
                SetError(result.GetErrorMessage());
        }
    }
}
