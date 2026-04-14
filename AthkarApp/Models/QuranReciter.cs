namespace AthkarApp.Models;

public class QuranReciter
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AudioFolderName { get; set; } = string.Empty;

    public static List<QuranReciter> GetPopularReciters()
    {
        return new List<QuranReciter>
        {
            new QuranReciter { Id = "ar.rifat", Name = "محمد رفعت", AudioFolderName = "muhammad_rifat" },
            new QuranReciter { Id = "ar.abdulbasitmurattal", Name = "عبد الباسط عبد الصمد", AudioFolderName = "abdul_basit_murattal" },
            new QuranReciter { Id = "ar.alafasy", Name = "مشاري راشد العفاسي", AudioFolderName = "mishaari_raashid_al_3afaasee" },
            new QuranReciter { Id = "ar.minshawi", Name = "محمد صديق المنشاوي", AudioFolderName = "muhammad_siddiq_al-minshawi" },
            new QuranReciter { Id = "ar.husary", Name = "محمود خليل الحصري", AudioFolderName = "mahmood_khaleel_al-husaree" },
            new QuranReciter { Id = "ar.tablawi", Name = "محمد محمود الطبلاوي", AudioFolderName = "mohammad_al_tablaway" },
            new QuranReciter { Id = "ar.saoodshuraym", Name = "سعود الشريم", AudioFolderName = "sa3ood_al-shuraym" },
            new QuranReciter { Id = "ar.abdurrahmansudais", Name = "عبد الرحمن السديس", AudioFolderName = "abdurrahmaan_as-sudays" }
        };
    }
}
