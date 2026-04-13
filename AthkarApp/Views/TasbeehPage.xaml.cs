namespace AthkarApp.Views;

public partial class TasbeehPage : ContentPage
{
    private int _count = 0;
    private bool _isHapticEnabled = true;
    private const string GlobalTotalKey = "tasbeeh_global_total";
    private const string BestSessionKey = "tasbeeh_best_session";

    public TasbeehPage()
    {
        InitializeComponent();
        LoadStats();
    }

    private void LoadStats()
    {
        int globalTotal = Preferences.Default.Get(GlobalTotalKey, 0);
        int bestSession = Preferences.Default.Get(BestSessionKey, 0);

        TotalLabel.Text = globalTotal.ToString();
        BestLabel.Text = bestSession.ToString();
    }

    private async void OnCounterClicked(object sender, EventArgs e)
    {
        _count++;
        CountLabel.Text = _count.ToString();

        // تحديث الإجمالي الكلي
        int globalTotal = Preferences.Default.Get(GlobalTotalKey, 0) + 1;
        Preferences.Default.Set(GlobalTotalKey, globalTotal);
        TotalLabel.Text = globalTotal.ToString();

        // تحديث أفضل جلسة
        int bestSession = Preferences.Default.Get(BestSessionKey, 0);
        if (_count > bestSession)
        {
            Preferences.Default.Set(BestSessionKey, _count);
            BestLabel.Text = _count.ToString();
        }

        // تأثير الاهتزاز (Haptic Feedback)
        if (_isHapticEnabled)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch { /* قد لا يدعم الجهاز الاهتزاز */ }
        }

        // تأثير بصري بسيط (نبض)
        await CounterFrame.ScaleTo(0.95, 50);
        await CounterFrame.ScaleTo(1.0, 50);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("تأكيد", "هل تريد تصغير العداد الحالي إلى الصفر؟", "نعم", "إلغاء");
        if (confirm)
        {
            _count = 0;
            CountLabel.Text = "0";
            
            // اهتزاز تأكيدي مختلف
            if (_isHapticEnabled) HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
    }

    private void OnToggleHaptic(object sender, EventArgs e)
    {
        _isHapticEnabled = !_isHapticEnabled;
        HapticBtn.Text = _isHapticEnabled ? "📳 اهتزاز: مفعّل" : "📵 اهتزاز: معطل";
        HapticBtn.BackgroundColor = _isHapticEnabled ? Color.FromArgb("#2C6E2C") : Color.FromArgb("#999999");
    }
}
