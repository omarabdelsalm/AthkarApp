using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Core.Primitives;

namespace AthkarApp.Views
{
    public partial class LiveRadioPage : ContentPage
    {
        public ObservableCollection<MediaChannel> Channels { get; set; } = new();
        public ICommand PlayCommand { get; }

        public LiveRadioPage()
        {
            try 
            {
                InitializeComponent();
                PlayCommand = new Command<MediaChannel>(OnPlayChannel);
                LoadChannels();
                BindingContext = this;

                // Fix for .NET 9 handler disconnection
                HandlerProperties.SetDisconnectPolicy(MainPlayer, HandlerDisconnectPolicy.Manual);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing Radio Page: {ex}");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Wire events when appearing
            if (MainPlayer != null)
            {
                MainPlayer.MediaOpened += OnMediaOpened;
                MainPlayer.MediaFailed += OnMediaFailed;
            }
        }

        protected override void OnDisappearing()
        {
            // Unwire events to prevent leaks/crashes in .NET 9
            if (MainPlayer != null)
            {
                MainPlayer.Stop();
                MainPlayer.MediaOpened -= OnMediaOpened;
                MainPlayer.MediaFailed -= OnMediaFailed;
            }
            
            base.OnDisappearing();
        }

        private void OnMediaOpened(object? sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
            });
        }

        private async void OnMediaFailed(object? sender, MediaFailedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () => {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                await DisplayAlert("خطأ", "فشل تشغيل البث، يرجى التأكد من اتصال الإنترنت.", "حسناً");
            });
        }

        private void LoadChannels()
        {
            Channels.Clear();

            Channels.Add(new MediaChannel 
            { 
                Name = "إذاعة القرآن الكريم - القاهرة", 
                Description = "البث المباشر لإذاعة القرآن الكريم من مصر", 
                Url = "https://n02.radiojar.com/8s5u8p4nzh0uv",
                Icon = "🇪🇬",
                IsVideo = false
            });

            Channels.Add(new MediaChannel 
            { 
                Name = "قناة القرآن الكريم مكة", 
                Description = "بث مباشر من المسجد الحرام بمكة المكرمة", 
                Url = "https://shls-ksa-it-live.akamaized.net/out/v1/75a7f96b58324e9389f417f763bf45c0/index.m3u8",
                Icon = "🕋",
                IsVideo = true
            });

            Channels.Add(new MediaChannel 
            { 
                Name = "قناة السنة النبوية", 
                Description = "بث مباشر من المسجد النبوي بالمدينة المنورة", 
                Url = "https://shls-ksa-it-live.akamaized.net/out/v1/7d3a0179a6174a748c9b33a59a6858e3/index.m3u8",
                Icon = "🕌",
                IsVideo = true
            });
            
            ChannelsListView.ItemsSource = Channels;
        }

        private void OnPlayChannel(MediaChannel channel)
        {
            if (channel == null) return;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            
            PlayingTitleLabel.Text = channel.Name;
            AudioOverlay.IsVisible = !channel.IsVideo;
            
            MainPlayer.Source = MediaSource.FromUri(channel.Url);
            MainPlayer.Play();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MainPlayer.Stop();
        }
    }

    public class MediaChannel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Icon { get; set; } = "📻";
        public bool IsVideo { get; set; }
    }
}
