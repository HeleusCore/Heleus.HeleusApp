using System;
using System.Text;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Cryptography;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class SignTextPage : StackPage
    {
        public static HashTypes SignatureHashType = HashTypes.Sha256;

        readonly EntryRow _text;
        readonly EditorRow _signedText;

        async Task Submit(ButtonRow button)
        {
            var text = _text.Edit.Text.Trim();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = Hash.Generate(SignatureHashType, bytes);
            var signature = Signature.Generate(WalletApp.CurrentCoreAccount.DecryptedKey, hash);

            _signedText.Edit.Text = $"{text}|{WalletApp.CurrentCoreAccount.AccountId}|{signature.HexString}";

            await MessageAsync("Success");
        }

        public SignTextPage() : base("SignTextPage")
        {
            AddTitleRow("Title");

            AddHeaderRow("Sign");
            _text = AddEntryRow("", "TextToSign");
            _text.SetDetailViewIcon(Icons.Pencil);

            _signedText = AddEditorRow("", null);
            _signedText.SetDetailViewIcon(Icons.Signature);

            var submit = AddSubmitRow("SignButton", Submit);
            submit.IsEnabled = false;
            _text.Edit.TextChanged += (sender, e) =>
            {
                submit.IsEnabled = !string.IsNullOrWhiteSpace(e.NewTextValue);
            };

            AddFooterRow();
        }
    }
}
