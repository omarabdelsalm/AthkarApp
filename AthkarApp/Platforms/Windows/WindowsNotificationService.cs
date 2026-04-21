#if WINDOWS
using AthkarApp.Services;
using Microsoft.Toolkit.Uwp.Notifications; // Ensure you have the Microsoft.Toolkit.Uwp.Notifications NuGet package installed
using Windows.UI.Notifications;

namespace AthkarApp.Platforms.Windows;

public class WindowsNotificationService : IAppNotificationService
{
    public void ScheduleAthkarAlarm(int id, string text, string soundName, DateTime notifyTime)
    {
        try
        {
            new ToastContentBuilder()
                .AddText("⭐ ذكر الله")
                .AddText(text)
                .AddAudio(new Uri($"ms-appx:///Assets/{soundName}.wav"))
                .Schedule(notifyTime, toast =>
                {
                    toast.Tag = id.ToString();
                    toast.Group = "Athkar";
                });
        }
        catch (Exception)
        {
        }
    }

    public void ScheduleAdhanAlarm(int id, string prayerName, string soundName, DateTime notifyTime)
    {
        try
        {
             new ToastContentBuilder()
                .AddText($"حان الآن موعد صلاة {prayerName}")
                .AddText("حي على الصلاة.. حي على الفلاح")
                .AddAudio(new Uri($"ms-appx:///Assets/om.wav")) // نستخدم الصوت المتاح حالياً لويندوز
                .Schedule(notifyTime, toast =>
                {
                    toast.Tag = id.ToString();
                    toast.Group = "Adhan";
                });
        }
        catch (Exception)
        {
        }
    }

    public void CancelNotification(int id)
    {
        ToastNotificationManager.History.Remove(id.ToString(), "Athkar");
        ToastNotificationManager.History.Remove(id.ToString(), "Adhan");
    }

    public void CancelAll()
    {
        ToastNotificationManager.History.Clear();
    }
}
#endif
