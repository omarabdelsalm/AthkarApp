using System.Text.Json.Serialization;

namespace AthkarApp.Models;

public class PrayerApiResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PrayerData Data { get; set; } = new();
}

public class PrayerData
{
    [JsonPropertyName("timings")]
    public PrayerTimings Timings { get; set; } = new();

    [JsonPropertyName("date")]
    public PrayerDate Date { get; set; } = new();

    [JsonPropertyName("meta")]
    public PrayerMeta Meta { get; set; } = new();
}

public class PrayerTimings
{
    [JsonPropertyName("Fajr")]
    public string Fajr { get; set; } = string.Empty;

    [JsonPropertyName("Sunrise")]
    public string Sunrise { get; set; } = string.Empty;

    [JsonPropertyName("Dhuhr")]
    public string Dhuhr { get; set; } = string.Empty;

    [JsonPropertyName("Asr")]
    public string Asr { get; set; } = string.Empty;

    [JsonPropertyName("Maghrib")]
    public string Maghrib { get; set; } = string.Empty;

    [JsonPropertyName("Isha")]
    public string Isha { get; set; } = string.Empty;
}

public class PrayerDate
{
    [JsonPropertyName("readable")]
    public string Readable { get; set; } = string.Empty;

    [JsonPropertyName("hijri")]
    public HijriDate Hijri { get; set; } = new();
}

public class HijriDate
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("day")]
    public string Day { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public HijriMonth Month { get; set; } = new();

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;
}

public class HijriMonth
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("en")]
    public string English { get; set; } = string.Empty;

    [JsonPropertyName("ar")]
    public string Arabic { get; set; } = string.Empty;
}

public class PrayerMeta
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;
}

public class PrayerTimingViewModel
{
    public string Name { get; set; } = string.Empty;
    public string ArabicName { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public bool IsNext { get; set; }
    public bool IsNotificationEnabled { get; set; }
}
