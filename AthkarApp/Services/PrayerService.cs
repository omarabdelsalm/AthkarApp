using AthkarApp.Models;
using Microsoft.Maui.Devices.Sensors;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AthkarApp.Services;

public interface IPrayerService
{
    Task<PrayerData?> GetPrayerTimingsAsync();
    Task<double> GetQiblaAngleAsync();
    Task ScheduleAdhanNotificationsAsync(PrayerData timings);
    bool IsPrayerEnabled(string prayerName);
    void SetPrayerEnabled(string prayerName, bool enabled);
}

public class PrayerService : IPrayerService
{
    private readonly HttpClient _httpClient;
    private readonly IFileStorageService _fileStorage;
    private const string PrayerCacheKey = "prayer_timings_cache.json";

    public PrayerService(HttpClient httpClient, IFileStorageService fileStorage)
    {
        _httpClient = httpClient;
        _fileStorage = fileStorage;
    }

    private string AdhanSoundName => Preferences.Default.Get("SelectedAdhanSound", "adhan");

    public async Task<PrayerData?> GetPrayerTimingsAsync()
    {
        try
        {
            // 1. الحصول على الموقع الحالي
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null)
            {
                location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            }

            if (location == null) return null;

            // 2. التحقق من الكاش (إذا كان لنفس اليوم ونفس الإحداثيات تقريباً)
            var cached = await _fileStorage.LoadJsonAsync<PrayerApiResponse>(PrayerCacheKey);
            if (cached != null && cached.Data.Date.Readable == DateTime.Today.ToString("dd MMM yyyy", CultureInfo.InvariantCulture))
            {
                // نتحقق من المسافة (إذا تغير الموقع بأكثر من 10 كم نحدث)
                double distance = location.CalculateDistance(cached.Data.Meta.Latitude, cached.Data.Meta.Longitude, DistanceUnits.Kilometers);
                if (distance < 10) return cached.Data;
            }

            // 3. جلب من API
            int method = Preferences.Default.Get("SelectedCalculationMethod", 5);
            int school = Preferences.Default.Get("SelectedMadhab", 0);
            
            var url = $"https://api.aladhan.com/v1/timings/{DateTimeOffset.Now.ToUnixTimeSeconds()}?latitude={location.Latitude}&longitude={location.Longitude}&method={method}&school={school}";
            var response = await _httpClient.GetFromJsonAsync<PrayerApiResponse>(url);

            if (response != null && response.Code == 200)
            {
                await _fileStorage.SaveJsonAsync(PrayerCacheKey, response);
                return response.Data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ GetPrayerTimingsAsync Error: {ex.Message}");
        }

        return null;
    }

    public async Task<double> GetQiblaAngleAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null) return 0;

            // رابط مباشر لجلب زاوية القبلة من الإحداثيات
            var url = $"https://api.aladhan.com/v1/qibla/{location.Latitude}/{location.Longitude}";
            var response = await _httpClient.GetFromJsonAsync<QiblaResponse>(url);
            return response?.Data?.Direction ?? 0;
        }
        catch { return 0; }
    }

    public async Task ScheduleAdhanNotificationsAsync(PrayerData data)
    {
        if (data == null) return;

        // إلغاء أي تنبيهات صلاة قديمة (نستخدم IDs من 2000 إلى 2005)
        for (int i = 2000; i <= 2005; i++)
        {
            LocalNotificationCenter.Current.Cancel(i);
        }

        var prayers = new[]
        {
            (Name: "Fajr", Ar: "الفجر", Time: data.Timings.Fajr, Id: 2000),
            (Name: "Dhuhr", Ar: "الظهر", Time: data.Timings.Dhuhr, Id: 2001),
            (Name: "Asr", Ar: "العصر", Time: data.Timings.Asr, Id: 2002),
            (Name: "Maghrib", Ar: "المغرب", Time: data.Timings.Maghrib, Id: 2003),
            (Name: "Isha", Ar: "العشاء", Time: data.Timings.Isha, Id: 2004)
        };

        foreach (var p in prayers)
        {
            if (!IsPrayerEnabled(p.Name)) continue;

            if (DateTime.TryParseExact(p.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                DateTime notifyTime = DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
                if (notifyTime < DateTime.Now) notifyTime = notifyTime.AddDays(1);

                var request = new NotificationRequest
                {
                    NotificationId = p.Id,
                    Title = $"حان الآن موعد صلاة {p.Ar}",
                    Description = $"حي على الصلاة.. حي على الفلاح",
                    Sound = AdhanSoundName,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = notifyTime,
                        RepeatType = NotificationRepeat.Daily
                    },
#if ANDROID

                    Android = new AndroidOptions
                    {
                        ChannelId = "adhan_channel",
                        Priority = AndroidPriority.High,
                        AutoCancel = true,
                        // لا تستخدم LaunchApp.Location - هذه الخاصية غير موجودة
                        // بدلاً من ذلك، استخدم Intent بديل كما هو موضح أدناه
                    }

                    //Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions
                    //{
                    //    ChannelId = "adhan_channel",
                    //    Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.High,
                    //    LaunchApp = { Location = "PrayerPage" }
                    //}
#endif
                };

                await LocalNotificationCenter.Current.Show(request);
            }
        }
    }

    public bool IsPrayerEnabled(string prayerName) => Preferences.Default.Get($"Prayer_{prayerName}_Enabled", true);
    public void SetPrayerEnabled(string prayerName, bool enabled) => Preferences.Default.Set($"Prayer_{prayerName}_Enabled", enabled);
}

public class QiblaResponse
{
    [JsonPropertyName("data")]
    public QiblaData Data { get; set; } = new();
}

public class QiblaData
{
    [JsonPropertyName("direction")]
    public double Direction { get; set; }
}
