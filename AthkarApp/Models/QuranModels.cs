using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SQLite;

namespace AthkarApp.Models;

public class SurahResponse
{
    [JsonPropertyName("data")]
    public List<Surah> Data { get; set; }
}

public class Surah
{
    [PrimaryKey]
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("englishName")]
    public string EnglishName { get; set; }

    [JsonPropertyName("englishNameTranslation")]
    public string EnglishNameTranslation { get; set; }

    [JsonPropertyName("numberOfAyahs")]
    public int NumberOfAyahs { get; set; }

    [JsonPropertyName("revelationType")]
    public string RevelationType { get; set; }

    [JsonIgnore]
    [Ignore]
    public bool IsDownloaded { get; set; }

    [JsonIgnore]
    [Ignore]
    public double DownloadProgress { get; set; }

    [JsonIgnore]
    [Ignore]
    public bool IsDownloading { get; set; }
}

public class AyahResponse
{
    [JsonPropertyName("data")]
    public AyahData Data { get; set; }
}

public class AyahData
{
    [JsonPropertyName("ayahs")]
    public List<Ayah> Ayahs { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("englishName")]
    public string EnglishName { get; set; }
}

public class Ayah
{
    [PrimaryKey]
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("numberInSurah")]
    public int NumberInSurah { get; set; }

    [JsonPropertyName("audio")]
    public string Audio { get; set; }

    [JsonPropertyName("surah")]
    [Ignore]
    public Surah? Surah { get; set; }

    [JsonPropertyName("page")]
    [Indexed]
    public int Page { get; set; }

    [Indexed]
    public int SurahNumber { get; set; }

    public string? TafsirText { get; set; }

    [JsonIgnore]
    [Ignore]
    public bool IsSelected { get; set; }
}

public class PageResponse
{
    [JsonPropertyName("data")]
    public PageData Data { get; set; }
}

public class PageData : INotifyPropertyChanged
{
    private int _number;
    private List<Ayah> _ayahs;
    private bool _hasBismillah;

    [JsonPropertyName("number")]
    public int Number
    {
        get => _number;
        set { _number = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("ayahs")]
    public List<Ayah> Ayahs
    {
        get => _ayahs;
        set { _ayahs = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public bool HasBismillah
    {
        get => _hasBismillah;
        set { _hasBismillah = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TafsirResponse
{
    [JsonPropertyName("data")]
    public TafsirData Data { get; set; }
}

public class TafsirData
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }
}