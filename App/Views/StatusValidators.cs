using System;
using Heleus.Apps.HeleusApp;
using Heleus.Chain;

namespace Heleus.Apps.Shared
{
    public static partial class StatusValidators
    {
        public static bool HeleusPasswordValidator(StatusView statusView, ExtEntry edit, string newText, string oldText)
        {
            return WalletApp.IsValidPassword(newText);
        }

        public static bool HeleusCoinValidator(StatusView statusView, ExtEntry edit, string newText, string oldText)
        {
            if (decimal.TryParse(newText, System.Globalization.NumberStyles.AllowDecimalPoint, null, out var p))
            {
                try
                {
                    var h = Currency.ToHel(p);
                    if (h < 0)
                    {
                        edit.Text = oldText;
                        goto err;
                    }

                    return h > 0;
                }
                catch { }
            }

            if (!newText.IsNullOrEmpty())
                edit.Text = oldText;

            err:
            return false;
        }
    }
}
