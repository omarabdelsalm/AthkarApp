using Microsoft.Extensions.Logging;
using AthkarApp.Services;
using AthkarApp.Views;
using Plugin.Maui.Audio;
using Camera.MAUI;
using CommunityToolkit.Maui;


namespace AthkarApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCameraView()
            .UseMauiCommunityToolkitMediaElement()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // الخطوط العثمانية (يجب إضافتها للمجلد Resources/Fonts أولاً)
                fonts.AddFont("UthmanicHafs.ttf", "UthmanicHafs");
                fonts.AddFont("Amiri-Regular.ttf", "Amiri");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("WebRTCPermissions", (handler, view) =>
        {
            handler.PlatformView.SetWebChromeClient(new AthkarApp.Platforms.Android.CustomWebChromeClient());
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            handler.PlatformView.Settings.DomStorageEnabled = true;
            handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
            handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
        });
#endif

        // تسجيل الخدمات
        builder.Services.AddSingleton<QuranDatabase>();
        builder.Services.AddSingleton<AthkarDatabase>();
        builder.Services.AddSingleton<AthkarService>();
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
        builder.Services.AddSingleton<IQuranDownloadService, QuranDownloadService>();
        builder.Services.AddSingleton<IAthkarNotificationService, AthkarNotificationService>();
        builder.Services.AddSingleton<ISiraService, SiraService>();
        builder.Services.AddSingleton<IFiqhService, FiqhService>();
        builder.Services.AddSingleton<IProphetService, ProphetService>();
        builder.Services.AddSingleton<IStreakService, StreakService>();
        builder.Services.AddSingleton<IQuranNormalizationService, QuranNormalizationService>();
        builder.Services.AddSingleton<IHifzAssessmentService, HifzAssessmentService>();
        builder.Services.AddSingleton<AchievementsService>();
        builder.Services.AddSingleton(AudioManager.Current);

        builder.Services.AddHttpClient<IQuranApiService, QuranApiService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(5); // زيادة الوقت للمزامنة الكاملة
        });

        builder.Services.AddHttpClient<IPrayerService, PrayerService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // تسجيل الصفحات
        builder.Services.AddSingleton<AthkarPage>();
        builder.Services.AddSingleton<QuranPage>();
        builder.Services.AddSingleton<MushafPage>();
        builder.Services.AddTransient<QuranUthmaniPage>();
        builder.Services.AddTransient<AchievementsPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<TasbeehPage>();
        builder.Services.AddSingleton<PrayerPage>();
        builder.Services.AddSingleton<KhatmahPage>();
        builder.Services.AddSingleton<SiraPage>();
        builder.Services.AddSingleton<FiqhPage>();
        builder.Services.AddSingleton<ProphetsPage>();
        builder.Services.AddSingleton<MushafTeacherPage>();
        builder.Services.AddTransient<ProphetDetailPage>();
        
        // Shared Khatmah
        builder.Services.AddSingleton<SharedKhatmahService>();
        builder.Services.AddTransient<SharedKhatmahListPage>();
        builder.Services.AddTransient<SharedKhatmahDetailsPage>();
        
        // Maqraa
        builder.Services.AddSingleton<MaqraaService>();
        builder.Services.AddTransient<MaqraaListPage>();
        builder.Services.AddTransient<CreateMaqraaPage>();
        builder.Services.AddTransient<MaqraaRoomPage>();

        return builder.Build();
    }
}