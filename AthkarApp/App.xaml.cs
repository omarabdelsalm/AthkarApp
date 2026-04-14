namespace AthkarApp
{
    public partial class App : Application
    {
        private readonly Services.IAthkarNotificationService _notificationService;
        private readonly Services.IPrayerService _prayerService;

        public App(Services.IAthkarNotificationService notificationService, Services.IPrayerService prayerService)
        {
            InitializeComponent();
            _notificationService = notificationService;
            _prayerService = prayerService;
        }

        protected override void OnStart()
        {
            base.OnStart();

            // جدولة الإشعارات مرة واحدة يومياً فقط
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _notificationService.RequestPermissionsAsync();
                await _notificationService.RequestBatteryOptimizationAsync();
                await _notificationService.EnsureNotificationsScheduledAsync();
                
                // جلب مواقيت الصلاة وجدولتها
                var timings = await _prayerService.GetPrayerTimingsAsync();
                if (timings != null)
                {
                    await _prayerService.ScheduleAdhanNotificationsAsync(timings);
                }
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}