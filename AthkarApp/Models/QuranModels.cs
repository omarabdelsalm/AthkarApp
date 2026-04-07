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
}