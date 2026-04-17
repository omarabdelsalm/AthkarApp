namespace AthkarApp.Services;

public interface IAppNotificationService
{
    void ScheduleAthkarAlarm(int id, string text, string soundName, DateTime notifyTime);
    void ScheduleAdhanAlarm(int id, string prayerName, string soundName, DateTime notifyTime);
    void CancelNotification(int id);
    void CancelAll();
}
