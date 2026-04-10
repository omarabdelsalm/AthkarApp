namespace AthkarApp
{
    public partial class App : Application
    {
        private readonly Services.IAthkarNotificationService _notificationService;

        public App(Services.IAthkarNotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
        }

        protected override void OnStart()
        {
            base.OnStart();

            // جدولة الإشعارات مرة واحدة يومياً فقط
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _notificationService.RequestPermissionsAsync();
                await _notificationService.EnsureScheduledTodayAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}