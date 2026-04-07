using System.Collections.ObjectModel;
using AthkarApp.Models;

namespace AthkarApp.Services;

public class AthkarService
{
    private readonly List<AthkarCategory> _categories;

    public AthkarService()
    {
        _categories = new List<AthkarCategory>
        {
            new AthkarCategory
            {
                Name = "أذكار الصباح",
                AthkarList = new List<string>
                {
                    "اللَّهُمَّ بِكَ أَصْبَحْنَا، وَبِكَ أَمْسَيْنَا، وَبِكَ نَحْيَا وَبِكَ نَمُوتُ، وَإِلَيْكَ النُّشُورُ",
                    "أَصْبَحْنَا عَلَى فِطْرَةِ الإِسْلاَمِ، وَعَلَى كَلِمَةِ الإِخْلاَصِ، وَعَلَى دِينِ نَبِيِّنَا مُحَمَّدٍ صَلَّى اللَّهُ عَلَيْهِ وَسَلَّمَ",
                    "اللَّهُمَّ إِنِّي أَصْبَحْتُ أُشْهِدُكَ وَأُشْهِدُ حَمَلَةَ عَرْشِكَ وَمَلاَئِكَتَكَ وَجَمِيعَ خَلْقِكَ أَنَّكَ أَنْتَ اللَّهُ لاَ إِلَهَ إِلاَّ أَنْتَ وَحْدَكَ لاَ شَرِيكَ لَكَ"
                }
            },
            new AthkarCategory
            {
                Name = "أذكار المساء",
                AthkarList = new List<string>
                {
                    "اللَّهُمَّ بِكَ أَمْسَيْنَا، وَبِكَ أَصْبَحْنَا، وَبِكَ نَحْيَا وَبِكَ نَمُوتُ، وَإِلَيْكَ الْمَصِيرُ",
                    "أَمْسَيْنَا عَلَى فِطْرَةِ الإِسْلاَمِ، وَعَلَى كَلِمَةِ الإِخْلاَصِ، وَعَلَى دِينِ نَبِيِّنَا مُحَمَّدٍ صَلَّى اللَّهُ عَلَيْهِ وَسَلَّمَ",
                    "اللَّهُمَّ مَا أَمْسَى بِي مِنْ نِعْمَةٍ أَوْ بِأَحَدٍ مِنْ خَلْقِكَ فَمِنْكَ وَحْدَكَ لاَ شَرِيكَ لَكَ"
                }
            },
            new AthkarCategory
            {
                Name = "أذكار النوم",
                AthkarList = new List<string>
                {
                    "بِاسْمِكَ اللَّهُمَّ أَمُوتُ وَأَحْيَا",
                    "اللَّهُمَّ قِنِي عَذَابَكَ يَوْمَ تَبْعَثُ عِبَادَكَ",
                    "الْحَمْدُ لِلَّهِ الَّذِي أَطْعَمَنَا وَسَقَانَا وَكَفَانَا وَآوَانَا"
                }
            }
        };
    }

    public List<AthkarCategory> GetAllCategories() => _categories;

    public AthkarCategory GetCategoryByName(string name) =>
        _categories.FirstOrDefault(c => c.Name == name);
}