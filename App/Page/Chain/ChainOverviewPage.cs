using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Chain.Core;
using Heleus.Cryptography;
using Heleus.Network.Client;
using Heleus.Chain;

namespace Heleus.Apps.HeleusApp.Page.Chain
{
    public enum ChainKeyAction
    {
        EditChain,
        Export,
        Cancel
    }

    public class ChainOverviewPage : StackPage
    {
        ButtonRow _import;

        async Task NewChain(ButtonRow button)
        {
            await Navigation.PushAsync(new ChainPage(null, null));
        }

        async Task ImportKey(ButtonRow button)
        {
            await Navigation.PushAsync(new ImportAccountPage(KeyStoreTypes.Chain, WalletApp.MinPasswordLength, WalletApp.ImportAccount, 1));
        }

        async Task<ChainKeyAction> DisplayKeyAction(bool canEditChain = false)
        {
            var cancel = Tr.Get("Common.Cancel");
            var editChain = T("EditChain");
            var export = T("ExportKey");
            var actions = new List<string>();

            if (canEditChain)
                actions.Add(editChain);
            actions.Add(export);

            var result = await DisplayActionSheet(Tr.Get("Common.Action"), cancel, null, actions.ToArray());
            if (result == editChain)
                return ChainKeyAction.EditChain;
            else if (result == export)
                return ChainKeyAction.Export;

            return ChainKeyAction.Cancel;
        }

        async Task KeyAction(ButtonViewRow<ClientAccountView> button)
        {
            var key = button.Tag as ChainKeyStore;
            var flags = key.PublicChainKey.Flags;
            var action = await DisplayKeyAction(flags.HasFlag(PublicChainKeyFlags.ChainAdminKey));
            if (action == ChainKeyAction.Export)
            {
                await Navigation.PushAsync(new ExportAccountPage(key, WalletApp.MinPasswordLength));
            }
            else if (action == ChainKeyAction.EditChain)
            {
                var chainInfo = (await WalletApp.Client.DownloadChainInfo(key.ChainId)).Data;
                if (chainInfo != null)
                {
                    await Navigation.PushAsync(new ChainPage(chainInfo, key));
                }
                else
                {
                    await MessageAsync("ChainInfoDownloadFailed");
                }
            }
        }

        public ChainOverviewPage() : base("ChainOverviewPage")
        {
            IsSuspendedLayout = true;

            Subscribe<CoreAccountUnlockedEvent>(AccountUnlocked);
            Subscribe<ChainRegistrationEvent>(ChainEvent);
            Subscribe<AccountImportEvent>(ImportEvent);

            SetupPage();
        }

        void SetupPage()
        {
            StackLayout.Children.Clear();

            AddTitleRow("Title");
        
            if (WalletApp.IsCoreAccountUnlocked)
            {
                AddHeaderRow("ChainKeys");
                _import = AddButtonRow("ImportKey", ImportKey);
                AddFooterRow();

                AddHeaderRow("NewChain");
                AddTextRow("ChainInfo").Label.ColorStyle = Theme.InfoColor;
                AddLinkRow("Common.LearnMore", Tr.Get("Link.Chain"));
                AddButtonRow("NewChainButton", NewChain);
                AddFooterRow();

                UpdateChainKeys();
            }
            else
            {
                AddHeaderRow("ChainKeys");
                AddTextRow("Common.Unlock").Label.ColorStyle = Theme.InfoColor;
                AddFooterRow();
            }

            UpdateSuspendedLayout();
        }

        void UpdateChainKeys()
        {
            AddIndex = _import;
            AddIndexBefore = true;

            var buttons = GetHeaderSectionRows("ChainKeys");
            var keys = WalletApp.Client.GetChainAccounts();

            foreach (var key in keys)
            {
                var added = false;

                foreach (var button in buttons)
                {
                    if (button.Tag is ChainKeyStore clientAccount && clientAccount.ChainId == key.ChainId && clientAccount.KeyIndex == key.KeyIndex)
                    {
                        added = true;
                    }
                }

                if (!added)
                {
                    var view = new ClientAccountView(key);
                    AddButtonViewRow(view, KeyAction).Tag = key;
                }
            }

            AddIndex = null;
        }

        Task AccountUnlocked(CoreAccountUnlockedEvent accountUnlocked)
        {
            SetupPage();

            return Task.CompletedTask;
        }

        Task ChainEvent(ChainRegistrationEvent chainEvent)
        {
            UpdateChainKeys();

            return Task.CompletedTask;
        }

        Task ImportEvent(AccountImportEvent importEvent)
        {
            if (importEvent.Account.KeyStoreType == KeyStoreTypes.Chain)
                UpdateChainKeys();

            return Task.CompletedTask;
        }
    }
}
