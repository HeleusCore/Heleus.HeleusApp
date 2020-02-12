using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Network.Client;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp
{
    class TransferCommand : Command
    {
        public const string CommandName = "transfer";
        public const string CommandDescription = "Transfer coins. Returns success.";

        long amount;

        long targetaccount;
        string reason;
        string password;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(targetaccount), "The target account"),
                new KeyValuePair<string, string>(nameof(amount), "The amount (use dot . as decimal separator)"),
                new KeyValuePair<string, string>(nameof(reason), $"The reason (optional, max {AccountUpdateOperation.MaxReasonLength} characters)"),
                new KeyValuePair<string, string>(nameof(password), "The account passowrd")
            };
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            targetaccount = arguments.Long(nameof(targetaccount), -1);
            reason = arguments.String(nameof(reason), null);
            password = arguments.String(nameof(password), null);

            var amountstr = arguments.String(nameof(amount), null);
            if (targetaccount < 0 || string.IsNullOrWhiteSpace(amountstr) || !WalletApp.IsValidPassword(password) || !AccountUpdateOperation.IsReasonValid(reason))
                return false;

            var hasDot = false;
            foreach (var c in amountstr)
            {
                if (c == '.' && !hasDot)
                {
                    hasDot = true;
                    continue;
                }

                if (!(c >= '0' && c <= '9'))
                {
                    SetError("Amount invalid");
                    return false;
                }
            }

            if (!decimal.TryParse(amountstr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var d))
            {
                SetError("Amount invalid");
                return false;
            }

            amount = Currency.ToHel(d);

            return amount > 0;
        }

        protected override async Task Run()
        {
            var result = await WalletApp.TransferCoins(targetaccount, amount, reason, password);
            if (result.TransactionResult == TransactionResultTypes.Ok)
                SetSuccess("transfer successfull");
            else
                SetError(result.GetErrorMessage());
        }
    }
}
