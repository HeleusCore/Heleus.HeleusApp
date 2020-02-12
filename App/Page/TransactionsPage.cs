using System;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Operations;

namespace Heleus.Apps.HeleusApp.Page
{
	public class TransactionsPage : StackPage
	{
        Task Refresh(ButtonRow button)
        {
            Download();
            return Task.CompletedTask;
        }

        Task More(ButtonRow button)
        {
            Download(true);
            return Task.CompletedTask;
        }

        public TransactionsPage() : base("TransactionsPage")
		{
            IsSuspendedLayout = true;

            Subscribe<CoreAccountUnlockedEvent>(AccountUnlocked);
            Subscribe<TransactionDownloadEvent<CoreOperation>>(AccountTransactionsDownloaded);

            SetupPage();
        }

        public override void OnOpen()
        {
            if (WalletApp.IsCoreAccountUnlocked)
                Download();
        }

        void Download(bool queryOlder = false)
        {
            IsBusy = true;
            UIApp.Run(() => WalletApp.DownloadCoreAccountTransaction(queryOlder));
        }

        void SetupPage()
        {
            StackLayout.Children.Clear();

            AddTitleRow("Title");

            if (WalletApp.IsCoreAccountUnlocked)
            {
                ToolbarItems.Add(new ExtToolbarItem(Tr.Get("Common.Refresh"), null, () =>
                {
                    Download(false);
                    return Task.CompletedTask;
                }));

                AddHeaderRow("RecentTransactions");
                AddFooterRow();
            }
            else
            {
                AddHeaderRow("RecentTransactions");
                AddTextRow("Common.Unlock").Label.ColorStyle = Theme.InfoColor;
                AddFooterRow();
            }

            UpdateSuspendedLayout();
        }

        Task AccountUnlocked(CoreAccountUnlockedEvent accountUnlocked)
        {
            SetupPage();
            Download();

            return Task.CompletedTask;
        }

        Task AccountTransactionsDownloaded(TransactionDownloadEvent<CoreOperation> downloadedEvent)
        {
            var section = GetRow<HeaderRow>("RecentTransactions");
            if(section != null)
            {
                // Todo: Make it smarter and add transactions at the top / bottom
                ClearHeaderSection(section);
                AddIndex = section;

                if (downloadedEvent.Items.Count == 1 && downloadedEvent.Items[0].Download.Transactions.Count > 0)
                {
                    var transactions = downloadedEvent.Items[0].Download.Transactions;

                    var reversedKeys = transactions.Keys.Reverse();
                    foreach(var key in reversedKeys)
                    {
                        var t = transactions[key].Transaction;
                        AddIndex = AddViewRow(new CoreOperationView(t, downloadedEvent.Items[0].Download.Id));
                    }

                    if(downloadedEvent.Items[0].Result.More)
                    {
                        AddButtonRow("MoreButton", More);
                    }
                }
                else
                {
                    Toast("NoTransactions");
                }
            }

            IsBusy = false;

            UpdateSuspendedLayout();

            return Task.CompletedTask;
        }
    }
}
