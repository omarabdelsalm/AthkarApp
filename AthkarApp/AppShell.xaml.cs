using AthkarApp.Views;

namespace AthkarApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(QuranUthmaniPage), typeof(QuranUthmaniPage));
            Routing.RegisterRoute(nameof(HadithDetailPage), typeof(HadithDetailPage));
            Routing.RegisterRoute("SharedKhatmahMushaf", typeof(MushafPage));
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            Current.FlyoutIsPresented = false;
            await Navigation.PushAsync(new SettingsPage(
                Handler.MauiContext.Services.GetService<AthkarApp.Services.IAthkarNotificationService>(),
                Handler.MauiContext.Services.GetService<AthkarApp.Services.IQuranDownloadService>(),
                Handler.MauiContext.Services.GetService<AthkarApp.Services.IPrayerService>()
            ));
        }

        private async void OnUserGuideClicked(object sender, EventArgs e)
        {
            Current.FlyoutIsPresented = false;
            await Navigation.PushAsync(new UserGuidePage());
        }

        private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
        {
            Current.FlyoutIsPresented = false;
            await Navigation.PushAsync(new PrivacyPolicyPage());
        }
    }
}
