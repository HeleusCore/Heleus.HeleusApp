using System;
using System.Threading.Tasks;
using Heleus.Apps.Shared;
using Heleus.Chain;
using Heleus.Chain.Core;
using Heleus.Chain.Maintain;
using Heleus.Network.Client;
using Heleus.Transactions;

namespace Heleus.Apps.HeleusApp.Page.Account
{
    public class RequestRevenuePage : ChainInfoBasePage
    {
        readonly RevenueView _revenueView;
        AccountRevenueInfo _revenueInfo;

        public RequestRevenuePage(int chainId = 0) : base(chainId, "RequestRevenuePage")
        {
            Subscribe<RequestRevenueEvent>(Revenue);

            SetupPage();

            _revenueView = new RevenueView(null);

            AddHeaderRow("Revenue");
            AddViewRow(_revenueView);
            AddFooterRow();

            AddSubmitSection();
        }

        async Task Revenue(RequestRevenueEvent arg)
        {
            IsBusy = false;

            var response = arg.Response;

            if(response.TransactionResult == TransactionResultTypes.Ok)
            {
                await MessageAsync("Success");
                await Navigation.PopAsync();
            }
            else
            {
                await ErrorTextAsync(response.GetErrorMessage());
            }
        }

        protected override async Task QueryDoneAsync(int chainId, ChainInfo chainInfo)
        {
            _revenueInfo = null;
            if(chainInfo != null)
            {
                var endPoints = chainInfo.GetPublicEndpoints();
                if(endPoints.Count > 0)
                {
                    var client = new ClientBase(new Uri(endPoints[0]), chainId);
                    var download = (await client.DownloadRevenueInfo(chainId, WalletApp.CurrentAccountId));
                    var data = download.Data;
                    _revenueInfo = data?.Item;
                    if(_revenueInfo == null)
                    {
                        await ErrorAsync("QueryRevenueFailed");
                    }
                }
            }

            _revenueView.Update(_revenueInfo);
        }

        protected override async Task Submit(ButtonRow button)
        {
            var info = _revenueInfo;
            if (info == null)
                return;

            var amount = _revenueInfo.TotalRevenue - _revenueInfo.Payout;

            if(await ConfirmTextAsync(T("ConfirmRevenue", Currency.ToString(amount))))
            {
                IsBusy = true;
                UIApp.Run(() => WalletApp.RequestRevenue(info.ChainId, _revenueInfo));
            }
        }
    }
}
