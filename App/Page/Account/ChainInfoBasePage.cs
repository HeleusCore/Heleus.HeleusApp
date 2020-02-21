using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Apps.HeleusApp.Views;
using Heleus.Base;
using Heleus.Chain.Core;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public abstract class ChainInfoBasePage : StackPage
    {
        protected readonly int _chainId;

        int _queryChainId;
        long _lastQueryAttemp;
        bool _querying;

        protected EntryRow _chainIdText { get; private set; }
        protected ChainInfo _chainInfo { get; private set; }
        protected EntryRow _password { get; private set; }

        ChainInfoView _chainView;
        ButtonRow _link;
        ButtonRow _account;

        async Task QueryChainInfo(int chainId)
        {
            if (_queryChainId == chainId)
                return;

            _lastQueryAttemp = Time.Timestamp;
            _queryChainId = chainId;

            if (_querying)
                return;
            _querying = true;

            Loading = true;
            while (Time.PassedSeconds(_lastQueryAttemp) < 0.5f)
                await Task.Delay(100);

            _chainInfo = (await WalletApp.Client.DownloadChainInfo(_queryChainId)).Data;

            await QueryDoneAsync(chainId, _chainInfo);
            QueryDone(chainId, _chainInfo);

            Loading = false;

            _querying = false;
            Status.ReValidate();
        }

        async Task Account(ButtonRow button)
        {
            if(_chainInfo != null)
            {
                await Navigation.PushAsync(new ViewProfilePage(_chainInfo.AccountId));
            }
        }

        abstract protected Task Submit(ButtonRow button);

        virtual protected void PreAddRows()
        {

        }

        virtual protected void PostAddRows()
        {

        }

        virtual protected void ChainInfoChanged()
        {

        }

        virtual protected Task QueryDoneAsync(int chainId, ChainInfo chainInfo)
        {
            return Task.CompletedTask;
        }

        virtual protected void QueryDone(int chainId, ChainInfo chainInfo)
        {

        }

        protected void SetupPage()
        {
            AddTitleRow("Title");

            AddHeaderRow("Service");

#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
            PreAddRows();
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor

            _chainIdText = AddEntryRow(null, "ChainInfoBasePage.ChainId");
            _chainIdText.SetDetailViewIcon(Icons.Link);

            if (_chainId > 0)
            {
                _chainIdText.Edit.Text = _chainId.ToString();
                _chainIdText.Edit.IsEnabled = false;
            }

            _chainView = new ChainInfoView();
            AddViewRow(_chainView);
            _link = AddLinkRow("ChainInfoBasePage.ChainLink", "");
            _link.IsEnabled = false;

            _account = AddButtonRow("ChainInfoBasePage.AccountProfile", Account);
            _account.SetDetailViewIcon(Icons.Coins);
            _account.IsEnabled = false;

#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
            PostAddRows();
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor

            AddFooterRow();

            Status.Add(_chainIdText.Edit, T("ChainInfoBasePage.ChainIdStatus"), (sv, edit, newText, oldText) =>
            {
                var success = false;
                if (StatusValidators.PositiveNumberValidator(sv, edit, newText, oldText))
                {
                    if (int.TryParse(newText, out var id))
                    {
                        if (_chainInfo != null && _chainInfo.ChainId == id)
                        {
                            success = true;
                        }
                        else
                        {
                            _ = QueryChainInfo(id);
                        }
                    }
                }
                else
                {
                    _queryChainId = -1;
                    _chainInfo = null;
                    QueryDone(-1, null);
                    UIApp.Run(() => QueryDoneAsync(-1, null));
                }

                _chainView.Update(_chainInfo);
                if (_chainInfo != null)
                {
                    if (!string.IsNullOrEmpty(_chainInfo.Website))
                    {
                        _link.IsEnabled = true;
                        _link.Tag = _chainInfo.Website;
                    }
                    _account.IsEnabled = true;
                }
                else
                {
                    _link.IsEnabled = false;
                    _account.IsEnabled = false;
                }

                ChainInfoChanged();

                return success;
            });
        }

        protected ChainInfoBasePage(int chainId, string name) : base(name)
        {
            _chainId = chainId;
        }

        protected void AddSubmitSection()
        {
            AddIndex = AddSubmitRow("Submit", Submit);
            AddIndexBefore = true;

            if (!WalletApp.IsCoreAccountUnlocked)
            {
                _password = AddPasswordRow(null, "ChainInfoBasePage.Password");
                Status.Add(_password.Edit, T("ChainInfoBasePage.PasswordStatus"), StatusValidators.HeleusPasswordValidator);
            }

            if (_chainId > 0)
                _ = QueryChainInfo(_chainId);

            AddIndex = null;
        }
    }
}
