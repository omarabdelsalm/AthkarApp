using AthkarApp.Models;

namespace AthkarApp.Services;

public class AthkarService
{
    private readonly AthkarDatabase _database;
    private List<AthkarCategory> _cachedCategories = new();

    public AthkarService(AthkarDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        var initialData = GetHardcodedData();
        await _database.SeedInitialDataAsync(initialData);
        _cachedCategories = await _database.GetCategoriesAsync();
    }

    public async Task<List<AthkarCategory>> GetAllCategoriesAsync()
    {
        if (!_cachedCategories.Any())
        {
            await InitializeAsync();
        }
        return _cachedCategories;
    }

    public async Task<AthkarCategory?> GetCategoryByNameAsync(string name)
    {
        if (!_cachedCategories.Any())
        {
            await InitializeAsync();
        }
        return _cachedCategories.FirstOrDefault(c => c.Name == name);
    }

    private List<AthkarCategory> GetHardcodedData()
    {
        return new List<AthkarCategory>
        {
            new AthkarCategory
            {
                Name = "أذكار الصباح",
                CardColor = "#E8F5E9",
                ImagePath = "morning_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "أَصْبَحْنَا وَأَصْبَحَ الْمُلْكُ لِلَّهِ، وَالْحَمْدُ لِلَّهِ لاَ إِلَهَ إِلاَّ اللَّهُ وَحْدَهُ لاَ شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ", Reference = "مسلم 4/2088", Count = 1, HadithSource = "صحيح مسلم" },
                    new() { Text = "اللَّهُمَّ بِكَ أَصْبَحْنَا، وَبِكَ أَمْسَيْنَا، وَبِكَ نَحْيَا وَبِكَ نَمُوتُ، وَإِلَيْكَ النُّشُورُ", Reference = "الترمذي 5/466", Count = 1, HadithSource = "جامع الترمذي" },
                    new() { Text = "اللَّهُمَّ أَنْتَ رَبِّي لاَ إِلَهَ إِلاَّ أَنْتَ، خَلَقْتَنِي وَأَنَا عَبْدُكَ، وَأَنَا عَلَى عَهْدِكَ وَوَعْدِكَ مَا اسْتَطَعْتُ، أَعُوذُ بكَ مِنْ شَرِّ مَا صَنَعْتُ، أَبُوءُ لَكَ بِنِعْمَتِكَ عَلَيَّ، وَأَبُوءُ لَكَ بِذَنْبِي فَاغْفِرْ لِي فَإِنَّهُ لاَ يَغْفِرُ الذُّنُوبَ إِلاَّ أَنْتَ", Reference = "البخاري 7/150", Count = 1, HadithSource = "صحيح البخاري" },
                    new() { Text = "يَا حَيُّ يَا قَيُّومُ بِرَحْمَتِكَ أَسْتَغيثُ أَصْلِحْ لِي شَأْنِي كُلَّهُ وَلَا تَكِلْنِي إِلَى نَفْسِي طَرْفَةَ عَيْنٍ", Reference = "الحاكم 1/545", Count = 1, HadithSource = "المستدرك" },
                    new() { Text = "أَسْتَغْفِرُ اللَّهَ وَأَتُوبُ إِلَيْهِ", Reference = "البخاري مع الفتح 11/101", Count = 100, HadithSource = "صحيح البخاري" }
                }
            },
            new AthkarCategory
            {
                Name = "أذكار المساء",
                CardColor = "#E3F2FD",
                ImagePath = "evening_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "أَمْسَيْنَا وَأَمْسَى الْمُلْكُ لِلَّهِ، وَالْحَمْدُ لِلَّهِ لاَ إِلَهَ إِلاَّ اللَّهُ وَحْدَهُ لاَ شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ", Reference = "مسلم 4/2088", Count = 1, HadithSource = "صحيح مسلم" },
                    new() { Text = "اللَّهُمَّ بِكَ أَمْسَيْنَا، وَبِكَ أَصْبَحْنَا، وَبِكَ نَحْيَا وَبِكَ نَمُوتُ، وَإِليْكَ الْمَصِيرُ", Reference = "الترمذي 5/466", Count = 1, HadithSource = "جامع الترمذي" },
                    new() { Text = "بِسْمِ اللَّهِ الَّذِي لَا يَضُرُّ مَعَ اسْمِهِ شَيْءٌ فِي الْأَرْضِ وَلَا فِي السَّمَاءِ وَهُوَ السَّمِيعُ الْعَلِيمُ", Reference = "الترمذي 5/465", Count = 3, HadithSource = "جامع الترمذي" },
                    new() { Text = "أَعُوذُ بِكَلِمَاتِ اللَّهِ التَّامَّاتِ مِنْ شَرِّ مَا خَلَقَ", Reference = "أحمد 2/290", Count = 3, HadithSource = "مسند أحمد" }
                }
            },
            new AthkarCategory
            {
                Name = "أذكار الاستيقاظ",
                CardColor = "#FFF3E0",
                ImagePath = "sleep_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "الْحَمْدُ لِلَّهِ الَّذِي أَحْيَانَا بَعْدَ مَا أَمَاتَنَا وَإِلَيْهِ النُّشُورُ", Reference = "البخاري", Count = 1, HadithSource = "صحيح البخاري" },
                    new() { Text = "الْحَمْدُ لِلَّهِ الَّذِي عَافَانِي فِي جَسَدِي، وَرَدَّ عَلَيَّ رُوحِي، وَأَذِنَ لِي بِذِكْرِهِ", Reference = "الترمذي", Count = 1, HadithSource = "جامع الترمذي" }
                }
            },
            new AthkarCategory
            {
                Name = "أذكار المسجد",
                CardColor = "#E1F5FE",
                ImagePath = "mosque_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "اللَّهُمَّ اجْعَلْ فِي قَلْبِي نُوراً، وَفِي لِسَانِي نُوراً، وَاجْعَلْ فِي سَمْعِي نُوراً، وَاجْعَلْ فِي بَصَرِي نُوراً", Reference = "مسلم", Count = 1, HadithSource = "صحيح مسلم" },
                    new() { Text = "بِسْمِ اللهِ، وَالصَّلَاةُ وَالسَّلَامُ عَلَى رَسُولِ اللهِ، اللَّهُمَّ اغْفِرْ لِي ذُنُوبِي، وَافْتَحْ لِي أَبْوَابَ رَحْمَتِكَ", Reference = "ابن ماجه", Count = 1, HadithSource = "سنن ابن ماجه" }
                }
            },
            new AthkarCategory
            {
                Name = "بعد السلام من الصلاة",
                CardColor = "#F3E5F5",
                ImagePath = "tasbeeh_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "أَسْتَغْفِرُ اللهَ (ثلاثاً) اللَّهُمَّ أَنْتَ السَّلَامُ وَمِنْكَ السَّلَامُ، تَبَارَكْتَ يَا ذَا الْجَلَالِ وَالْإِكْرَامِ", Reference = "مسلم", Count = 1, HadithSource = "صحيح مسلم" },
                    new() { Text = "اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ، وَشُكْرِكَ، وَحُسْنِ عِبَادَتِكَ", Reference = "أبو داود", Count = 1, HadithSource = "سنن أبي داود" },
                    new() { Text = "سُبْحَانَ اللهِ (33)، الْحَمْدُ للهِ (33)، اللهُ أَكْبَرُ (33)", Reference = "مسلم", Count = 33, HadithSource = "صحيح مسلم" }
                }
            },
            new AthkarCategory
            {
                Name = "أذكار السفر",
                CardColor = "#FAFAFA",
                ImagePath = "travel_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "اللهُ أَكْبَرُ، اللهُ أَكْبَرُ، اللهُ أَكْبَرُ، سُبْحَانَ الَّذِي سَخَّرَ لَنَا هَذَا وَمَا كُنَّا لَهُ مُقْرِنِينَ وَإِنَّا إِلَى رَبِّنَا لَمُنْقَلِبُونَ", Reference = "مسلم", Count = 1, HadithSource = "صحيح مسلم" },
                    new() { Text = "اللَّهُمَّ إِنَّا نَسْأَلُكَ فِي سَفَرِنَا هَذَا الْبِرَّ وَالتَّقْوَى، وَمِنَ الْعَمَلِ مَا تَرْضَى", Reference = "مسلم", Count = 1, HadithSource = "صحيح مسلم" }
                }
            },
            new AthkarCategory
            {
                Name = "أدعية الهم والحزن",
                CardColor = "#FFEBEE",
                ImagePath = "sadness_athkar.jpg",
                AthkarList = new List<ThikrItem>
                {
                    new() { Text = "اللَّهُمَّ إِنِّي أَعُوذُ بِكَ مِنَ الْهَمِّ وَالْحَزَنِ، وَالْعَجْزِ وَالْكَسَلِ، وَالْبُخْلِ وَالْجُبْنِ، وَضَلَعِ الدَّيْنِ، وَغَلَبَةِ الرِّجَالِ", Reference = "البخاري", Count = 1, HadithSource = "صحيح البخاري" },
                    new() { Text = "لَا إِلَهَ إِلَّا اللهُ الْعَظِيمُ الْحَلِيمُ، لَا إِلَهَ إِلَّا اللهُ رَبُّ الْعَرْشِ الْعَظِيمِ", Reference = "البخاري", Count = 1, HadithSource = "صحيح البخاري" }
                }
            }
        };
    }
}