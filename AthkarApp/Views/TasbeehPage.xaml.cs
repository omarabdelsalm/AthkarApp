using AthkarApp.Services;

namespace AthkarApp.Views;

public partial class TasbeehPage : ContentPage
{
    private int _count = 0;
    private bool _isHapticEnabled = true;
    private bool _isSoundEnabled = true;
    private readonly ISoundService _soundService;

    private const string GlobalTotalKey = "tasbeeh_global_total";
    private const string BestSessionKey = "tasbeeh_best_session";
    private const string HapticEnabledKey = "tasbeeh_haptic_enabled";
    private const string SoundEnabledKey = "tasbeeh_sound_enabled";

    public TasbeehPage(ISoundService soundService)
    {
        InitializeComponent();
        _soundService = soundService;
        LoadStats();
    }

    private void LoadStats()
    {
        int globalTotal = Preferences.Default.Get(GlobalTotalKey, 0);
        int bestSession = Preferences.Default.Get(BestSessionKey, 0);
        _isHapticEnabled = Preferences.Default.Get(HapticEnabledKey, true);
        _isSoundEnabled = Preferences.Default.Get(SoundEnabledKey, true);

        TotalLabel.Text = globalTotal.ToString();
        BestLabel.Text = bestSession.ToString();

        UpdateHapticBtnUI();
        UpdateSoundBtnUI();
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

        // تحديد ما إذا كان قد تم الوصول لعدد 33 أو 100
        bool isTargetReached = (_count % 33 == 0 || _count % 100 == 0);

        // تأثير الاهتزاز (Haptic Feedback)
        if (_isHapticEnabled)
        {
            try
            {
                if (isTargetReached)
                {
                    // اهتزاز طويل ومميز عند اكتمال العداد
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                }
            }
            catch { /* قد لا يدعم الجهاز الاهتزاز */ }
        }

        // تأثير الصوت
        if (_isSoundEnabled)
        {
            try
            {
                if (isTargetReached)
                {
                    await _soundService.PlaySoundAsync("tasbeeh_success");
                }
                else
                {
                    await _soundService.PlaySoundAsync("tasbeeh_click");
                }
            }
            catch { }
        }

        // تأثير بصري بسيط (نبض)
        await CounterFrame.ScaleTo(0.95, 50);
        await CounterFrame.ScaleTo(1.0, 50);
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("تأكيد", "هل تريد تصفير العداد الحالي إلى الصفر؟", "نعم", "إلغاء");
        if (confirm)
        {
            _count = 0;
            CountLabel.Text = "0";
            
            // اهتزاز تأكيدي مختلف
            if (_isHapticEnabled)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(150));
                }
                catch { }
            }
        }
    }

    private void OnToggleHaptic(object sender, EventArgs e)
    {
        _isHapticEnabled = !_isHapticEnabled;
        Preferences.Default.Set(HapticEnabledKey, _isHapticEnabled);
        UpdateHapticBtnUI();
    }

    private void OnToggleSound(object sender, EventArgs e)
    {
        _isSoundEnabled = !_isSoundEnabled;
        Preferences.Default.Set(SoundEnabledKey, _isSoundEnabled);
        UpdateSoundBtnUI();
    }

    private void UpdateHapticBtnUI()
    {
        HapticBtn.Text = _isHapticEnabled ? "📳 اهتزاز: مفعّل" : "📵 اهتزاز: معطل";
        HapticBtn.BackgroundColor = _isHapticEnabled ? Color.FromArgb("#2C6E2C") : Color.FromArgb("#999999");
    }

    private void UpdateSoundBtnUI()
    {
        SoundBtn.Text = _isSoundEnabled ? "🔊 صوت: مفعّل" : "🔇 صوت: معطل";
        SoundBtn.BackgroundColor = _isSoundEnabled ? Color.FromArgb("#2C6E2C") : Color.FromArgb("#999999");
    }
}
