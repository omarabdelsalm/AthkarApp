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

            // تنفيذ طلبات الصلاحيات على الـ UI Thread
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _notificationService.RequestPermissionsAsync();
                
                bool batteryAsked = Preferences.Default.Get("battery_optimization_asked", false);
                if (!batteryAsked)
                {
                    await _notificationService.RequestBatteryOptimizationAsync();
                    Preferences.Default.Set("battery_optimization_asked", true);
                }

                // نقل العمليات الثقيلة (جدولة + جلب مواقع + حفظ) إلى مسار خلفي
                // لتجنب تعليق الواجهة وخروج التطبيق (Skipped 264 frames/Crash)
                _ = Task.Run(async () =>
                {
                    try 
                    {
                        await _notificationService.EnsureNotificationsScheduledAsync();
                        
                        // جلب مواقيت الصلاة وجدولتها
                        var timings = await _prayerService.GetPrayerTimingsAsync();
                        if (timings != null)
                        {
                            await _prayerService.ScheduleAdhanNotificationsAsync(timings);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Background Init Error: {ex.Message}");
                    }
                });
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}