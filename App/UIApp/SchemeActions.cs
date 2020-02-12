using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Cryptography;

namespace Heleus.Apps.HeleusApp
{
    public class TransferCoinsSchemeAction : SchemeAction
    {
        public const string ActionName = "transfercoins";

        public readonly long ReceiverId;
        public readonly long Amount;
        public readonly string Reason;

        public override bool IsValid => ReceiverId > 0 && Amount > 0;

        public TransferCoinsSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetLong(StartIndex, out ReceiverId);
            GetLong(StartIndex + 1, out Amount);
            Reason = GetString(StartIndex + 2);
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if (app.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                    app.MainTabbedPage.ShowPage(typeof(Page.Account.AccountPage));

                await app.CurrentPage.Navigation.PushAsync(new Page.Account.TransferPage(ReceiverId, Amount, Reason));
            }
        }
    }

    public class AuthorizeServiceDerivedSchemeAction : SchemeAction
    {
        public const string ActionName = "authorizeservicederived";

        public readonly int ChainId;
        public readonly string DerivedPassword;

        public override bool IsValid => ChainId > 0 && !string.IsNullOrEmpty(DerivedPassword);

        public AuthorizeServiceDerivedSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetInt(StartIndex, out ChainId);
            DerivedPassword = GetString(StartIndex + 1);
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if (app.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                    app.MainTabbedPage.ShowPage(typeof(Page.Account.AccountPage));

                await app.CurrentPage.Navigation.PushAsync(new Page.Account.AuthorizeDerivedKeyPage(ChainId, DerivedPassword));
            }
        }
    }
    public class AuthorizeServiceSchemeAction : SchemeAction
    {
        public const string ActionName = "authorizeservice";

        public readonly int ChainId;
        public readonly Key ChainKey;

        public override bool IsValid => ChainId > 0 && ChainKey != null && !ChainKey.IsPrivate && ChainKey.KeyType == Protocol.TransactionKeyType;

        public AuthorizeServiceSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetInt(StartIndex, out ChainId);
            var key = GetString(StartIndex + 1);
            try
            {
                if (key != null)
                    ChainKey = Key.Restore(key);
            }
            catch { }
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if(app.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                    app.MainTabbedPage.ShowPage(typeof(Page.Account.AccountPage));

                await app.CurrentPage.Navigation.PushAsync(new Page.Account.JoinChainPage(ChainId, ChainKey));
            }
        }
    }

    public class AuthorizePurchaseSchemeAction : SchemeAction
    {
        public const string ActionName = "authorizepurchase";

        public readonly int ChainId;
        public readonly int PurchaseId;

        public override bool IsValid => ChainId > 0 && PurchaseId > 0;

        public AuthorizePurchaseSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetInt(StartIndex, out ChainId);
            GetInt(StartIndex + 1, out PurchaseId);
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if (app.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                    app.MainTabbedPage.ShowPage(typeof(Page.Account.AccountPage));

                await app.CurrentPage.Navigation.PushAsync(new Page.Account.BuyPurchasePage(ChainId, PurchaseId));
            }
        }
    }

    public class ViewTransactionsSchemeAction : SchemeAction
    {
        public const string ActionName = "viewtransactions";

        public override bool IsValid => true;

        public ViewTransactionsSchemeAction(SchemeData schemeData) : base(schemeData)
        {
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;           

            var app = UIApp.Current;
            if (app != null)
            {
                await app.ShowPage(typeof(Page.TransactionsPage));
            }
        }
    }

    public class ViewProfileSchemeAction : SchemeAction
    {
        public const string ActionName = "viewprofile";

        public readonly long AccountId;

        public override bool IsValid => AccountId > 0;


        public ViewProfileSchemeAction(SchemeData schemeData) : base(schemeData)
        {
            GetLong(StartIndex, out AccountId);
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if (app.CurrentPage != null)
            {
                if (app.MainTabbedPage != null)
                    app.MainTabbedPage.ShowPage(typeof(Page.Profile.ProfilePage));

                await app.CurrentPage.Navigation.PushAsync(new ViewProfilePage(AccountId));
            }
        }
    }

    public class EditProfileSchemeAction : SchemeAction
    {
        public const string ActionName = "editprofile";

        public override bool IsValid => true;

        public EditProfileSchemeAction(SchemeData schemeData) : base(schemeData)
        {
        }

        public override async Task Run()
        {
            if (!IsValid)
                return;

            var app = UIApp.Current;
            if (app != null)
            {
                await app.ShowPage(typeof(Page.Profile.ProfilePage));
            }
        }
    }
}
