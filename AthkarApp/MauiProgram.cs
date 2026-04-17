using Microsoft.Extensions.Logging;
using AthkarApp.Services;
using AthkarApp.Views;
using Plugin.Maui.Audio;


namespace AthkarApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // تسجيل الخدمات
        builder.Services.AddSingleton<QuranDatabase>();
        builder.Services.AddSingleton<AthkarService>();
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddSingleton<IFileStorageService, FileStorageService>();
        builder.Services.AddSingleton<IQuranDownloadService, QuranDownloadService>();
        builder.Services.AddSingleton<IAthkarNotificationService, AthkarNotificationService>();
        builder.Services.AddSingleton<IStreakService, StreakService>();
        builder.Services.AddSingleton<IPrayerService, PrayerService>();
        builder.Services.AddSingleton(AudioManager.Current);

        builder.Services.AddHttpClient<IQuranApiService, QuranApiService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromMinutes(5); // زيادة الوقت للمزامنة الكاملة
        });

        // تسجيل الصفحات
        builder.Services.AddSingleton<AthkarPage>();
        builder.Services.AddSingleton<QuranPage>();
        builder.Services.AddSingleton<MushafPage>();
        builder.Services.AddTransient<SurahDetailPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<TasbeehPage>();
        builder.Services.AddSingleton<PrayerPage>();

        return builder.Build();
    }
}