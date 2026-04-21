using AthkarApp.Models;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.Networking;

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
    private readonly IFileStorageService _fileStorage;
    private readonly HttpClient _httpClient;
    private const string PrayerCacheKey = "prayer_timings_cache.json";

    public PrayerService(IFileStorageService fileStorage, HttpClient httpClient)
    {
        _fileStorage = fileStorage;
        _httpClient = httpClient;
    }

    private string AdhanSoundName => Preferences.Default.Get("SelectedAdhanSound", "adhan");

    public async Task<PrayerData?> GetPrayerTimingsAsync(bool forceRefresh = false)
    {
        try
        {
            // 1. محاولة الحصول على الموقع الحالي
            Location? location = null;
            try {
                location = await Geolocation.Default.GetLastKnownLocationAsync();
                if (location == null)
                {
                    location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
                }
            } catch (Exception) { }

            // 2. إذا لم يتوفر الموقع ولا يوجد إنترنت، نستعيد الكاش
            if (location == null && Connectivity.Current.NetworkAccess != NetworkAccess.Internet && !forceRefresh)
            {
                var cached = await _fileStorage.LoadJsonAsync<PrayerData>(PrayerCacheKey);
                if (cached != null) ApplyManualDstIfEnabled(cached.Timings);
                return cached;
            }

            // 3. الجلب من Aladhan API
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    int method = Preferences.Default.Get("SelectedCalculationMethod", 5);
                    string url;
                    
                    if (location != null)
                    {
                        url = $"https://api.aladhan.com/v1/timings?latitude={location.Latitude}&longitude={location.Longitude}&method={method}";
                    }
                    else
                    {
                        url = $"https://api.aladhan.com/v1/timingsByCity?city=Cairo&country=Egypt&method={method}";
                    }

                    var response = await _httpClient.GetStringAsync(url);
                    var apiResponse = JsonSerializer.Deserialize<PrayerApiResponse>(response);
                    
                    if (apiResponse != null && apiResponse.Code == 200)
                    {
                        var data = apiResponse.Data;
                        await _fileStorage.SaveJsonAsync(PrayerCacheKey, data);
                        ApplyManualDstIfEnabled(data.Timings);
                        return data;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                }
            }

            // 4. Fallback to cache
            var finalCached = await _fileStorage.LoadJsonAsync<PrayerData>(PrayerCacheKey);
            if (finalCached != null) ApplyManualDstIfEnabled(finalCached.Timings);
            return finalCached;
        }
        catch (Exception)
        {
            return await _fileStorage.LoadJsonAsync<PrayerData>(PrayerCacheKey);
        }
    }

    private void ApplyManualDstIfEnabled(PrayerTimings timings)
    {
        if (Preferences.Default.Get("ManualDstEnabled", false))
        {
            timings.Fajr = AddHour(timings.Fajr);
            timings.Sunrise = AddHour(timings.Sunrise);
            timings.Dhuhr = AddHour(timings.Dhuhr);
            timings.Asr = AddHour(timings.Asr);
            timings.Maghrib = AddHour(timings.Maghrib);
            timings.Isha = AddHour(timings.Isha);
        }
    }

    private string AddHour(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return timeStr;
        // تنظيف الوقت من أي لاحقة مثل (EEST)
        string cleanTime = timeStr.Split(' ')[0];
        if (DateTime.TryParseExact(cleanTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return time.AddHours(1).ToString("HH:mm");
        }
        return timeStr;
    }

    public async Task<double> GetQiblaAngleAsync()
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null) return 0;
            
            double m_lat = 21.4225 * Math.PI / 180.0;
            double m_lng = 39.8262 * Math.PI / 180.0;
            double u_lat = location.Latitude * Math.PI / 180.0;
            double u_lng = location.Longitude * Math.PI / 180.0;

            double angle = Math.Atan2(Math.Sin(m_lng - u_lng), 
                                      Math.Cos(u_lat) * Math.Tan(m_lat) - Math.Sin(u_lat) * Math.Cos(m_lng - u_lng));
            
            return (angle * 180.0 / Math.PI + 360) % 360;
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

            string cleanTime = p.Time.Split(' ')[0];
            if (DateTime.TryParseExact(cleanTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            {
                DateTime notifyTime = DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
                if (notifyTime < DateTime.Now) notifyTime = notifyTime.AddDays(1);

                // 1. تنبيه بقرب الأذان (قبل أربع دقائق)
                DateTime preNotifyTime = notifyTime.AddMinutes(-4);
                if (preNotifyTime > DateTime.Now)
                {
                    nativeService.ScheduleAdhanAlarm(p.Id + 100, p.Ar, "silent", preNotifyTime);
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
