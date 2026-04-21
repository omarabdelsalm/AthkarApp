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
            });

#if DEBUG
        builder.Logging.AddDebug();
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
        builder.Services.AddTransient<SurahDetailPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<TasbeehPage>();
        builder.Services.AddSingleton<PrayerPage>();
        builder.Services.AddSingleton<KhatmahPage>();
        builder.Services.AddSingleton<SiraPage>();
        builder.Services.AddSingleton<FiqhPage>();
        builder.Services.AddSingleton<ProphetsPage>();
        builder.Services.AddSingleton<MushafTeacherPage>();
        builder.Services.AddTransient<ProphetDetailPage>();

        return builder.Build();
    }
}