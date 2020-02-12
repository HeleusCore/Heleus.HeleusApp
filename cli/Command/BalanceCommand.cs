using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp
{
    class BalanceCommand : Command
    {
        public const string CommandName = "balance";
        public const string CommandDescription = "Returns the current account balance on success.";

        string password;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(password), "The account password")
            };
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            password = arguments.String(nameof(password), null);

            return WalletApp.IsValidPassword(password);
        }

        protected override async Task Run()
        {
            if (!WalletApp.HasCoreAccount)
            {
                SetError("No core account availble");
                return;
            }

            if (!WalletApp.IsCoreAccountUnlocked)
                await WalletApp.UnlockCoreAccount(password);

            if (!WalletApp.IsCoreAccountUnlocked)
            {
                SetError("Password is invalid");
                return;
            }

            var balance = await WalletApp.UpdateCoreAccountBalance();
            if (balance != null)
                SetSuccess($"Coins: {Currency.ToString(balance.Balance, false)}\nGifted: {Currency.ToString(balance.GiftedBalance, false)}");
            else
                SetError("Downloading balance failed");
        }
    }
}
