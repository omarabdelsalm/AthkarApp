using Microsoft.Maui.Storage;

namespace AthkarApp.Services;

public interface IStreakService
{
    int GetCurrentStreak();
    int GetBestStreak();
    Task<(bool IsNewDay, int Count, string Message)> CheckAndUpdateStreakAsync();
}

public class StreakService : IStreakService
{
    private const string CurrentStreakKey = "streak_current_count";
    private const string BestStreakKey    = "streak_best_count";
    private const string LastActiveDateKey = "streak_last_date";

    public int GetCurrentStreak() => Preferences.Default.Get(CurrentStreakKey, 0);
    public int GetBestStreak()    => Preferences.Default.Get(BestStreakKey, 0);

    public async Task<(bool IsNewDay, int Count, string Message)> CheckAndUpdateStreakAsync()
    {
        DateTime today = DateTime.Today;
        string lastDateStr = Preferences.Default.Get(LastActiveDateKey, string.Empty);
        int currentCount = GetCurrentStreak();
        int bestCount = GetBestStreak();

        // 1. إذا كان المستخدم قد دخل اليوم بالفعل، لا نفعل شيئاً
        if (lastDateStr == today.ToString("yyyy-MM-dd"))
        {
            return (false, currentCount, string.Empty);
        }

        bool isNewDay = true;
        
        // 2. التحقق مما إذا كان النشاط متتالياً (أمس)
        if (!string.IsNullOrEmpty(lastDateStr))
        {
            DateTime lastDate = DateTime.Parse(lastDateStr);
            if (lastDate == today.AddDays(-1))
            {
                // السلسلة مستمرة
                currentCount++;
            }
            else
            {
                // السلسلة انقطعت، نبدأ من جديد
                currentCount = 1;
            }
        }
        else
        {
            // أول مرة يستخدم فيها التطبيق
            currentCount = 1;
        }

        // 3. تحديث أفضل سلسلة
        if (currentCount > bestCount)
        {
            Preferences.Default.Set(BestStreakKey, currentCount);
        }

        // 4. حفظ البيانات الجديدة
        Preferences.Default.Set(CurrentStreakKey, currentCount);
        Preferences.Default.Set(LastActiveDateKey, today.ToString("yyyy-MM-dd"));

        return (isNewDay, currentCount, GetEncouragingMessage(currentCount));
    }

    private string GetEncouragingMessage(int count)
    {
        if (count == 1) return "بداية مباركة! استمر في طريق الأذكار يومياً.";
        if (count % 7 == 0) return $"ما شاء الله! لقد أكملت {count / 7} أسابيع متتالية من الأذكار. أنت رائع!";
        if (count % 30 == 0) return $"إنجاز مذهل! شهر كامل من المحافظة على الأذكار. ثبتك الله.";
        
        return $"أحسنت! لقد حافظت على أذكارك لمدة {count} أيام متتالية. استمر!";
    }
}
