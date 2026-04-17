using AthkarApp.Models;
using AthkarApp.Services;
using System.Globalization;

namespace AthkarApp.Views;

public partial class PrayerPage : ContentPage
{
    private readonly IPrayerService _prayerService;
    private IDispatcherTimer _timer;
    private PrayerData _currentData;
    private double _qiblaAngle = 0;

    public PrayerPage(IPrayerService prayerService)
    {
        InitializeComponent();
        _prayerService = prayerService;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) => UpdateCountdown();
        _timer.Start();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPrayerData();
        StartCompass();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCompass();
    }

    private async Task LoadPrayerData(bool forceRefresh = false)
    {
        try
        {
            if (_currentData == null)
            {
                LocationLabel.Text = "جاري جلب البيانات...";
            }

            var data = await _prayerService.GetPrayerTimingsAsync(forceRefresh);
            if (data != null)
            {
                _currentData = data;
                UpdateUI(data);
                _qiblaAngle = await _prayerService.GetQiblaAngleAsync();
                QiblaAngleLabel.Text = $"الزاوية: {_qiblaAngle:F0}°";
                
                // جدولة الإشعارات آلياً
                await _prayerService.ScheduleAdhanNotificationsAsync(data);

                // تنبيه المستخدم إذا كانت البيانات من الكاش (Offline)
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    LocationLabel.Text = "تعمل الآن في وضع عدم الاتصال";
                }
            }
            else if (_currentData == null)
            {
                LocationLabel.Text = "تعذر جلب البيانات. يرجى الاتصال بالإنترنت";
            }
        }
        catch (Exception ex)
        {
            if (_currentData == null)
            {
                await DisplayAlert("خطأ", "فشل جلب مواقيت الصلاة: " + ex.Message, "حسناً");
            }
        }
    }

    private void UpdateUI(PrayerData data)
    {
        HijriDateLabel.Text = $"{data.Date.Hijri.Day} {data.Date.Hijri.Month.Arabic} {data.Date.Hijri.Year}";
        LocationLabel.Text = "الموقع: إحداثيات GPS مفعلة";

        var prayers = new List<PrayerTimingViewModel>
        {
            new() { Name = "Fajr", ArabicName = "الفجر", Time = data.Timings.Fajr },
            new() { Name = "Sunrise", ArabicName = "الشروق", Time = data.Timings.Sunrise },
            new() { Name = "Dhuhr", ArabicName = "الظهر", Time = data.Timings.Dhuhr },
            new() { Name = "Asr", ArabicName = "العصر", Time = data.Timings.Asr },
            new() { Name = "Maghrib", ArabicName = "المغرب", Time = data.Timings.Maghrib },
            new() { Name = "Isha", ArabicName = "العشاء", Time = data.Timings.Isha }
        };

        TimingsList.Clear();
        foreach (var p in prayers)
        {
            TimingsList.Add(CreatePrayerRow(p));
        }
        
        UpdateCountdown();
    }

    private View CreatePrayerRow(PrayerTimingViewModel p)
    {
        bool isNext = NextPrayerLabel.Text.Contains(p.ArabicName);

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) },
            Padding = new Thickness(20, 15),
            BackgroundColor = isNext ? Color.FromArgb("#D4AF37").WithAlpha(0.1f) : Colors.Transparent
        };

        var nameLabel = new Label 
        { 
            Text = p.ArabicName, 
            VerticalOptions = LayoutOptions.Center, 
            FontSize = 18, 
            FontAttributes = isNext ? FontAttributes.Bold : FontAttributes.None,
            TextColor = isNext ? Color.FromArgb("#143214") : Color.FromArgb("#333333") 
        };

        var timeLabel = new Label 
        { 
            Text = p.Time, 
            VerticalOptions = LayoutOptions.Center, 
            FontSize = 18, 
            FontAttributes = FontAttributes.Bold, 
            TextColor = isNext ? Color.FromArgb("#143214") : Color.FromArgb("#2C6E2C") 
        };
        
        bool isEnabled = _prayerService.IsPrayerEnabled(p.Name);
        var bellBtn = new ImageButton 
        { 
            Source = isEnabled ? "athkar_icon.png" : "quran_icon.png",
            HeightRequest = 26, 
            WidthRequest = 26,
            Opacity = isEnabled ? 1 : 0.3,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(15, 0, 0, 0)
        };
        
        if (p.Name == "Sunrise") bellBtn.IsVisible = false;

        bellBtn.Clicked += (s, e) => {
            bool current = _prayerService.IsPrayerEnabled(p.Name);
            _prayerService.SetPrayerEnabled(p.Name, !current);
            bellBtn.Opacity = !current ? 1 : 0.3;
            _ = _prayerService.ScheduleAdhanNotificationsAsync(_currentData);
        };

        grid.Add(nameLabel, 0);
        grid.Add(timeLabel, 1);
        grid.Add(bellBtn, 2);

        return new VerticalStackLayout {
            Children = { grid, new BoxView { HeightRequest = 1, Color = Color.FromArgb("#DDDDDD"), Margin = new Thickness(15, 0) } }
        };
    }

    private void UpdateCountdown()
    {
        if (_currentData == null) return;

        var now = DateTime.Now;
        var timings = new List<(string Name, string Ar, DateTime Time)>
        {
            ("Fajr", "الفجر", GetTime(_currentData.Timings.Fajr)),
            ("Dhuhr", "الظهر", GetTime(_currentData.Timings.Dhuhr)),
            ("Asr", "العصر", GetTime(_currentData.Timings.Asr)),
            ("Maghrib", "المغرب", GetTime(_currentData.Timings.Maghrib)),
            ("Isha", "العشاء", GetTime(_currentData.Timings.Isha))
        };

        var next = timings.OrderBy(t => t.Time).FirstOrDefault(t => t.Time > now);
        if (next.Name == null) // All prayers passed today
        {
            next = timings.OrderBy(t => t.Time).First();
            next.Time = next.Time.AddDays(1);
        }

        NextPrayerLabel.Text = $"الصلاة القادمة: {next.Ar}";
        var diff = next.Time - now;
        CountdownLabel.Text = diff.ToString(@"hh\:mm\:ss");
    }

    private DateTime GetTime(string timeStr)
    {
        if (DateTime.TryParseExact(timeStr, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
        }
        return DateTime.Now;
    }

    private void StartCompass()
    {
        if (!Compass.Default.IsSupported) return;
        if (Compass.Default.IsMonitoring) return;

        Compass.Default.ReadingChanged += OnCompassReadingChanged;
        Compass.Default.Start(SensorSpeed.UI);
    }

    private void StopCompass()
    {
        if (!Compass.Default.IsSupported) return;
        if (!Compass.Default.IsMonitoring) return;

        Compass.Default.ReadingChanged -= OnCompassReadingChanged;
        Compass.Default.Stop();
    }

    private void OnCompassReadingChanged(object sender, CompassChangedEventArgs e)
    {
        // حساب الزاوية النسبية للقبلة
        double relativeAngle = _qiblaAngle - e.Reading.HeadingMagneticNorth;
        QiblaNeedle.RotateTo(relativeAngle, 100);
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadPrayerData(true);
    }
}
