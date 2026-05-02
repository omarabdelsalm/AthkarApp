using AthkarApp.Views;

namespace AthkarApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
            Routing.RegisterRoute(nameof(SurahDetailPage), typeof(SurahDetailPage));
            Routing.RegisterRoute(nameof(HadithDetailPage), typeof(HadithDetailPage));
        }
    }
}
