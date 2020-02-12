using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Heleus.Apps.Shared;

namespace Heleus.Apps.HeleusApp.Page.Chain
{
    public class EndpointPage : StackPage
    {
        readonly ChainPage _chainPage;
        readonly EntryRow _endPoint;
        readonly ButtonRow _submit;
        readonly List<ChainItem<string>> _endPoints;

        public EndpointPage(ChainPage chainPage, List<ChainItem<string>> endPoints) : base("EndpointPage")
        {
            _chainPage = chainPage;
            _endPoints = endPoints;

            AddTitleRow("Title");

            AddHeaderRow("EndPoint");
            _endPoint = AddEntryRow("https://", "EndPoint");
            _endPoint.Edit.TextChanged += Entry_TextChanged;
            _endPoint.SetDetailViewIcon(Icons.RowLink);
            AddFooterRow("EndPointInfo");

            _submit = AddSubmitRow("Submit", Submit);
            _submit.IsEnabled = false;
        }

        void Entry_TextChanged(object sender, Xamarin.Forms.TextChangedEventArgs e)
        {
            var uri = _endPoint.Edit.Text;
            if(uri.IsValdiUrl(false))
            {
                foreach (var endpoint in _endPoints)
                {
                    if (endpoint.Item == uri)
                        goto invalid;
                }

                _submit.IsEnabled = true;
                return;
            }

            invalid:
            _submit.IsEnabled = false;
        }

        async Task Submit(ButtonRow button)
        {
            try
            {
                var uri = new Uri(_endPoint.Edit.Text);
                _chainPage.AddEndPoint(_endPoint.Edit.Text);

                await Navigation.PopAsync();
            }
            catch { }
        }
    }
}
