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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _notificationService.RequestPermissionsAsync();
                await _notificationService.ScheduleHourlyNotificationAsync();
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}