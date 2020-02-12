using System;
using Heleus.Apps.Shared;
using Heleus.Base;
using Heleus.Chain;
using Heleus.Operations;

namespace Heleus.Apps.HeleusApp.Views
{
    public class CoreOperationView : RowView
    {
        public CoreOperationView(CoreOperation operation, long accountId) : base("CoreOperationView")
        {
            AddRow("Type", Tr.Get("CoreOperation." + operation.CoreOperationType.ToString()));

            switch (operation.CoreOperationType)
            {
                case CoreOperationTypes.Account:
                    var ao = operation as AccountOperation;
                    AddRow("AccountId", ao.AccountId.ToString());
                    AddRow("PublicKey", ao.PublicKey.HexString);
                    break;

                case CoreOperationTypes.AccountUpdate:
                    var bo = operation as AccountUpdateOperation;

                    var balanceChanged = false;
                    foreach (var account in bo.Updates)
                    {
                        foreach (var transfer in account.Value.Transfers)
                        {
                            if (transfer.ReceiverId == accountId)
                            {
                                balanceChanged = true;
                            }
                        }
                    }

                    if (bo.Updates.TryGetValue(accountId, out var update))
                    {
                        if (update.Purchases.Count > 0 || update.Transfers.Count > 0)
                            balanceChanged = true;

                        if (balanceChanged)
                        {
                            AddRow("NewBalance", Currency.ToString(update.Balance));
                        }

                        foreach (var purchase in update.Purchases)
                        {
                            AddRow("Purchased", Tr.Get("CoreOperationView.PurchasedInfo", purchase.PurchaseItemId, purchase.ChainId, Currency.ToString(purchase.Price)));
                        }

                        foreach(var join in update.Joins)
                        {
                            AddRow("Joined", Tr.Get("CoreOperationView.JoinInfo", join.ChainId, join.KeyIndex));
                        }

                        foreach (var transfer in update.Transfers)
                        {
                            AddRow("Sent", Tr.Get("CoreOperationView.SentInfo", Currency.ToString(transfer.Amount), transfer.ReceiverId, transfer.Reason ?? "-"));
                        }
                    }

                    foreach (var account in bo.Updates)
                    {
                        foreach(var transfer in account.Value.Transfers)
                        {
                            if(transfer.ReceiverId == accountId)
                            {
                                AddRow("Received", Tr.Get("CoreOperationView.ReceivedInfo", Currency.ToString(transfer.Amount), account.Value.AccountId, transfer.Reason ?? "-"));
                            }
                        }
                    }

                    break;

                case CoreOperationTypes.ChainInfo:
                    var co = operation as ChainInfoOperation;
                    AddRow("ChainId", co.ChainId.ToString());
                    break;
            }

            AddRow("Id", operation.OperationId.ToString());
            AddLastRow("Date", Time.DateTimeString(operation.Timestamp));
        }
    }
}
