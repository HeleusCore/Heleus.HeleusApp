using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Heleus.Apps.Shared
{
    class TestPage : StackPage
	{
		#if !DEBUG
		public static bool ShowTestPage = false;
		#else
		public static bool ShowTestPage = false;
		#endif

        async Task Start(ButtonRow button)
		{
            //await Navigation.PushAsync(new IconsPage());
			await Task.Delay (0);
		}

        async Task Close(ButtonRow button)
        {
            await UIApp.Current.PopModal(this);
        }

		public TestPage() : base("TestPage")
		{
			if (UIApp.IsUWP)
				NavigationPage.SetHasNavigationBar(this, false);

			AddHeaderRow();
			AddButtonRow("Start", Start);
			AddButtonRow("Close", Close, true);
			AddFooterRow();

            var closeItem = new ExtToolbarItem("Close", null, async () =>
            {
                await Close(null);
            });

            ToolbarItems.Add(closeItem);

            UIApp.Run(async () =>
			{
				await Task.Delay(100);
				await Start(null);
			});
		}
	}
}

