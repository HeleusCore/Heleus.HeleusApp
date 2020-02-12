using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Network.Client;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp
{
    class RegisterCommand : Command
    {
        public const string CommandName = "register";
        public const string CommandDescription = "Register a new heleus account. Returns the account id on success.";

        string name;
        string password;
        bool agreedatalicence;

        protected override bool Parse(ArgumentsParser arguments)
        {
            name = arguments.String(nameof(name), null);
            password = arguments.String(nameof(password), null);
            agreedatalicence = arguments.Bool(nameof(agreedatalicence), false);

            if (!agreedatalicence || string.IsNullOrEmpty(name) || !WalletApp.IsValidPassword(password))
                return false;
            return true;
        }

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(name), "The name of the account key"),
                new KeyValuePair<string, string>(nameof(password), $"The password, must be {WalletApp.MinPasswordLength} characters"),
                new KeyValuePair<string, string>(nameof(agreedatalicence), "Must be yes, see https://heleuscore.com/datalicence")
            };
        }

        protected override async Task Run()
        {
            var result = await WalletApp.RegisterCoreAccount(name, password);

            if (result.TransactionResult == TransactionResultTypes.Ok)
                SetSuccess((result.Transaction as AccountOperation).AccountId.ToString());
            else
                SetError(result.GetErrorMessage());
        }
    }
}
