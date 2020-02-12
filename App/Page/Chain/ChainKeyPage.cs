using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Cryptography;

namespace Heleus.Apps.HeleusApp.Page.Chain
{
    public class ChainKeyPage : StackPage
    {
		readonly ChainPage _chainPage;

        readonly EditorRow _key;
        readonly EntryRow _keyIndex;
        readonly EntryRow _chainIndex;

        readonly EntryRow _name;
        readonly SwitchRow _admin;

        readonly SwitchRow _serviceKey;
        readonly SwitchRow _serviceVote;

        readonly SwitchRow _dataKey;
        readonly SwitchRow _dataVote;

        readonly EntryRow _pw1;
        readonly EntryRow _pw2;

        public ChainKeyPage(ChainPage chainPage, List<ChainKeyItem> chainKeys) : base("ChainKeyPage")
        {
			_chainPage = chainPage;

            AddTitleRow("Title");

            AddHeaderRow("Key");

            _name = AddEntryRow(null, "Name");
            _name.SetDetailViewIcon(Icons.Pencil);

            _key = AddEditorRow(null, "Key");
            _key.SetDetailViewIcon(Icons.Key);

            AddButtonRow("KeyButton", NewKey);

            _keyIndex = AddEntryRow(string.Empty, "KeyIndex");
            _keyIndex.SetDetailViewIcon(Icons.Highlighter);

            AddFooterRow();

            AddHeaderRow("KeyOptions");

            _admin = AddSwitchRow("Admin");
            _serviceKey = AddSwitchRow("ServiceKey");
            _serviceVote = AddSwitchRow("ServiceVote");
            _dataKey = AddSwitchRow("DataKey");
            _dataVote = AddSwitchRow("DataVote");
            _chainIndex = AddEntryRow(string.Empty, "ChainIndex");

            _admin.Switch.Toggled = (swt) =>
            {
                if(swt.IsToggled)
                {
                    _serviceKey.Switch.IsToggled = false;
                    _serviceVote.Switch.IsToggled = false;
                    _dataKey.Switch.IsToggled = false;
                    _dataVote.Switch.IsToggled = false;
                    _chainIndex.Edit.Text = null;
                }
                Status.ReValidate();
            };

            _serviceKey.Switch.Toggled = (swt) =>
            {
                if(swt.IsToggled)
                {
                    _admin.Switch.IsToggled = false;
                    _dataKey.Switch.IsToggled = false;
                    _dataVote.Switch.IsToggled = false;
                    _chainIndex.Edit.Text = null;
                }
                else
                {
                    _serviceVote.Switch.IsToggled = false;
                }
                Status.ReValidate();
            };

            _serviceVote.Switch.Toggled = (swt) =>
            {
                if (swt.IsToggled)
                    _serviceKey.Switch.IsToggled = true;
            };

            _dataKey.Switch.Toggled = (swt) =>
            {
                if(swt.IsToggled)
                {
                    _admin.Switch.IsToggled = false;
                    _serviceKey.Switch.IsToggled = false;
                    _serviceVote.Switch.IsToggled = false;
                }
                else
                {
                    _dataVote.Switch.IsToggled = false;
                }
                Status.ReValidate();
            };

            _dataVote.Switch.Toggled = (swt) =>
            {
                if (swt.IsToggled)
                    _dataKey.Switch.IsToggled = true;
            };

            AddFooterRow();

            AddHeaderRow("Password");

            _pw1 = AddEntryRow(string.Empty, "Password");
            _pw1.SetDetailViewIcon(Icons.Unlock);
            _pw2 = AddEntryRow(string.Empty, "Password2");
            _pw2.SetDetailViewIcon(Icons.Unlock);
            _pw1.Edit.IsPassword = _pw2.Edit.IsPassword = true;

            AddFooterRow();

            Status.Add(_key.Edit, T("KeyStatus"), (view, entry, newText, oldtext) =>
            {
                try
                {
                    var key = Key.Restore(entry.Text);
                    return key.KeyType == Protocol.TransactionKeyType;
                }
                catch { }
                return false;
            }).
            Add(_keyIndex.Edit, T("KeyIndexStatus", short.MinValue, short.MaxValue), (view, entry, newText, oldText) =>
            {
                if (short.TryParse(newText, out var idx))
                {
                    foreach (var key in chainKeys)
                    {
                        if (key.Item.KeyIndex == idx)
                            return false;
                    }
                    return true;
                }
                if (!newText.IsNullOrEmpty())
                    entry.Text = oldText;
                return false;
            }).
            Add(_chainIndex.Edit, T("ChainIndexStatus"), (view, entry, newText, oldText) =>
            {
                if(_dataKey.Switch.IsToggled)
                {
                    return StatusValidators.PositiveNumberValidatorWithZero(view, entry, newText, oldText);
                }

                if (!string.IsNullOrEmpty(newText))
                    entry.Text = null;

                return true;
            }).
            Add(_name.Edit, T("NameStatus"), (view, entry, newText, oldtext) =>
            {
                return !newText.IsNullOrWhiteSpace();
            }).
            Add(_pw1.Edit, T("PasswordStatus", WalletApp.MinPasswordLength), (view, entry, newText, oldtext) =>
            {
                var pw1 = _pw1.Edit.Text;
                var pw2 = _pw2.Edit.Text;

                return WalletApp.IsValidPassword(pw1) && WalletApp.IsValidPassword(pw2) && pw1 == pw2;
            });

            _pw2.Edit.TextChanged += (sender, e) =>
            {
                Status.ReValidate();
            };

            AddSubmitRow("Submit", Submit);
        }

        Task NewKey(ButtonRow button)
        {
            _key.Edit.Text = Key.Generate(Protocol.TransactionKeyType).HexString;
            return Task.CompletedTask;
        }

        async Task Submit(ButtonRow button)
        {
            try
            {
                // TODO Chain Key Expire
                var key = Key.Restore(_key.Edit.Text);
                var keyIndex = short.Parse(_keyIndex.Edit.Text);
                var chainIndex = 0u;

                var flags = PublicChainKeyFlags.None;

                if (_admin.Switch.IsToggled)
                    flags |= PublicChainKeyFlags.ChainAdminKey;

                if (_serviceKey.Switch.IsToggled)
                    flags |= PublicChainKeyFlags.ServiceChainKey;
                if (_serviceVote.Switch.IsToggled)
                    flags |= PublicChainKeyFlags.ServiceChainVoteKey;

                if (_dataKey.Switch.IsToggled)
                {
                    flags |= PublicChainKeyFlags.DataChainKey;
                    chainIndex = uint.Parse(_chainIndex.Edit.Text);
                }
                if (_dataVote.Switch.IsToggled)
                    flags |= PublicChainKeyFlags.DataChainVoteKey;

                var name = _name.Edit.Text;
                var password = _pw1.Edit.Text;

                var coreAccount = WalletApp.CurrentCoreAccount;

                var chainKey = new PublicChainKey(flags, Protocol.CoreChainId, chainIndex, 0, keyIndex, key);
                _chainPage.AddChainKey(chainKey, key, name, password);
                await Navigation.PopAsync();
            } 
            catch { }
        }
    }
}
