using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Network.Client;
using Heleus.Operations;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp.Page.Account
{
	public class TransferPage : StackPage
	{
        readonly long _receiverId;

        readonly EntryRow _receiver;
        readonly EntryRow _amount;
        readonly EntryRow _reason;
        readonly EntryRow _password;

        async Task ShowReceiver(ButtonRow button)
        {
            var receiver = long.Parse(_receiver.Edit.Text);
            await Navigation.PushAsync(new ViewProfilePage(receiver));
        }

        Task Submit(ButtonRow button)
        {
            IsBusy = true;

            var receiver = long.Parse(_receiver.Edit.Text);
            var amount = Currency.ToHel(decimal.Parse(_amount.Edit.Text));
            var reason = _reason.Edit.Text;
            var password = _password.Edit.Text;

            _ = WalletApp.TransferCoins(receiver, amount, reason, password);

            return Task.CompletedTask;
        }

        async Task TransferEvent(CoreAccountTransferEvent transferEvent)
        {
            IsBusy = false;

            if (_receiverId > 0)
                _receiver.Edit.IsEnabled = false;

            if(transferEvent.Response.TransactionResult == TransactionResultTypes.Ok)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(transferEvent.Response.GetErrorMessage());
            }
        }

        public TransferPage(long receiverId = 0, long amount = 0, string reason = null) : base("TransferPage")
		{
            _receiverId = receiverId;

            Subscribe<CoreAccountTransferEvent>(TransferEvent);

            AddTitleRow("Title");

            AddHeaderRow("TransferDetail");

            _receiver = AddEntryRow(null, "Receiver");
            _receiver.SetDetailViewIcon(Icons.Coins);

            if(receiverId > 0)
            {
                _receiver.Edit.Text = receiverId.ToString();
                _receiver.Edit.IsEnabled = false;
            }

            var showreceiver = AddButtonRow("ReceiverButton", ShowReceiver);
            showreceiver.IsEnabled = false;

            _amount = AddEntryRow(null, "Amount");
            _amount.SetDetailViewIcon(Icons.CreditCardFront);
            if (amount > 0)
                _amount.Edit.Text = Currency.ToString(amount, false);

            _reason = AddEntryRow(null, "Reason");
            _reason.SetDetailViewIcon(Icons.Pencil);
            _reason.Edit.Text = reason;

            _reason.Edit.TextChanged += (sender, e) =>
            {
                var txt = e.NewTextValue;
                if (!string.IsNullOrEmpty(txt) && txt.Length > AccountUpdateOperation.MaxReasonLength)
                    _reason.Edit.Text = txt.Substring(0, AccountUpdateOperation.MaxReasonLength);
            };

            AddFooterRow();

            Status.Add(_receiver.Edit, T("ReceiverStatus"), (sv, edit, newText, oldText) =>
            {
                return showreceiver.IsEnabled = StatusValidators.PositiveNumberValidator(sv, edit, newText, oldText);
            }).
            Add(_amount.Edit, T("AmountStatus"), StatusValidators.HeleusCoinValidator).
            AddBusyView(showreceiver).
            AddBusyView(_reason.Edit);

            AddIndex = AddSubmitRow("Submit", Submit);
            AddIndexBefore = true;

            _password = AddPasswordRow(null, "Password");

            Status.Add(_password.Edit, T("PasswordStatus"), StatusValidators.HeleusPasswordValidator);
        }
    }
}
