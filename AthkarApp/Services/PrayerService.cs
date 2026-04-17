using AthkarApp.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AthkarApp.Services;

public interface IPrayerService
{
    Task<PrayerData?> GetPrayerTimingsAsync(bool forceRefresh = false);
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

    public async Task<PrayerData?> GetPrayerTimingsAsync(bool forceRefresh = false)
    {
        try
        {
            // 1. التحقق من الكاش أولاً لتسريع الفتح
            var cached = await _fileStorage.LoadJsonAsync<PrayerApiResponse>(PrayerCacheKey);
            bool isCacheToday = cached != null && cached.Data.Date.Readable == DateTime.Today.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

            if (isCacheToday && !forceRefresh)
            {
                return cached.Data;
            }

            // 2. محاولة الحصول على الموقع الحالي
            Location location = null;
            try {
                location = await Geolocation.Default.GetLastKnownLocationAsync();
                if (location == null)
                {
                    location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"⚠️ Geolocation Error: {ex.Message}");
            }

            if (isCacheToday)
            {
                if (location != null)
                {
                    // نتحقق من المسافة إذا كان الموقع متاحاً
                    double distance = location.CalculateDistance(cached.Data.Meta.Latitude, cached.Data.Meta.Longitude, DistanceUnits.Kilometers);
                    if (distance < 10) return cached.Data;
                }
                else
                {
                    // إذا لم نتمكن من تحديد الموقع ولكن لدينا كاش لليوم، نستخدمه كحل أخير (Offline Mode)
                    return cached.Data;
                }
            }

            // 3. جلب من API (نحتاج للموقع هنا)
            if (location == null) return cached?.Data; // إذا فشل كل شيء، نرجع الكاش القديم حتى لو لم يكن لليوم

            int method = Preferences.Default.Get("SelectedCalculationMethod", 5);
            int school = Preferences.Default.Get("SelectedMadhab", 0);
            
            var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            var url = string.Format(CultureInfo.InvariantCulture, "https://api.aladhan.com/v1/timings/{0}?latitude={1}&longitude={2}&method={3}&school={4}", 
                timestamp, location.Latitude, location.Longitude, method, school);
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
            // محاولة أخيرة: إرجاع أي كاش متاح
            try {
                var fallback = await _fileStorage.LoadJsonAsync<PrayerApiResponse>(PrayerCacheKey);
                return fallback?.Data;
            } catch { }
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
            var url = string.Format(CultureInfo.InvariantCulture, "https://api.aladhan.com/v1/qibla/{0}/{1}", 
                location.Latitude, location.Longitude);
            var response = await _httpClient.GetFromJsonAsync<QiblaResponse>(url);
            return response?.Data?.Direction ?? 0;
        }
        catch { return 0; }
    }

    public async Task ScheduleAdhanNotificationsAsync(PrayerData data)
    {
        if (data == null) return;

        IAppNotificationService nativeService;
#if ANDROID
        nativeService = new AthkarApp.Platforms.Android.AndroidNotificationService();
#elif WINDOWS
        nativeService = new AthkarApp.Platforms.Windows.WindowsNotificationService();
#else
        return;
#endif

        // إلغاء أي تنبيهات صلاة قديمة (نستخدم IDs من 2000 إلى 2105 لخدمة الأذان وتنبيهات القرب)
        for (int i = 2000; i <= 2105; i++)
        {
            nativeService.CancelNotification(i);
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

                // 1. تنبيه بقرب الأذان (قبل دقيقتين)
                DateTime preNotifyTime = notifyTime.AddMinutes(-2);
                if (preNotifyTime > DateTime.Now)
                {
                    nativeService.ScheduleAdhanAlarm(p.Id + 100, $"قرب أذان {p.Ar}", "silent", preNotifyTime);
                }

                // 2. أذان الصلاة الفعلي
                nativeService.ScheduleAdhanAlarm(p.Id, p.Ar, AdhanSoundName, notifyTime);
            }
        }
        await Task.CompletedTask;
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
