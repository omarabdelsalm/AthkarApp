using System.Text.Json.Serialization;

namespace AthkarApp.Models;

public class SurahResponse
{
    [JsonPropertyName("data")]
    public List<Surah> Data { get; set; }
}

public class Surah
{
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
    public bool IsDownloaded { get; set; }

    [JsonIgnore]
    public double DownloadProgress { get; set; }

    [JsonIgnore]
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
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("numberInSurah")]
    public int NumberInSurah { get; set; }

    [JsonPropertyName("audio")]
    public string Audio { get; set; }

    [JsonPropertyName("surah")]
    public Surah? Surah { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonIgnore]
    public bool IsSelected { get; set; }
}

public class PageResponse
{
    [JsonPropertyName("data")]
    public PageData Data { get; set; }
}

public class PageData
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("ayahs")]
    public List<Ayah> Ayahs { get; set; }

    [JsonIgnore]
    public bool HasBismillah { get; set; }
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