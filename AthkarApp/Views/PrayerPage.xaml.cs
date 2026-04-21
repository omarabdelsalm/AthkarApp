using AthkarApp.Models;
using AthkarApp.Services;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors; // ✅ بدلاً من Xamarin.Essentials

namespace AthkarApp.Views;

public partial class PrayerPage : ContentPage
{
    private readonly IPrayerService _prayerService;
    private IDispatcherTimer _timer;
    private PrayerData _currentData;
    private double _qiblaAngle = 0;
    private bool _isCompassInitialized = false;
    private bool _isARMode = false;

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
        await CheckAndEnableLocationServices();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopCompass();
        
        if (_isARMode)
        {
            _isARMode = false;
            ARModeBtn.Text = "📷 تشغيل الواقع المعزز (AR)";
            ARModeBtn.BackgroundColor = Color.FromArgb("#1A3A1A");
            CompassDialImg.IsVisible = true;
            CameraViewCtrl.IsVisible = false;
            MainThread.BeginInvokeOnMainThread(async () => await CameraViewCtrl.StopCameraAsync());
        }
    }

    private async Task CheckAndEnableLocationServices()
    {
        try
        {
            // ✅ استخدام Permissions من Microsoft.Maui
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (locationStatus != PermissionStatus.Granted)
            {
                var result = await DisplayAlert("صلاحية الموقع",
                    "نحتاج إلى صلاحية الموقع لحساب أوقات الصلاة بدقة وتحديد اتجاه القبلة.",
                    "طلب الصلاحية",
                    "إلغاء");

                if (result)
                {
                    locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (locationStatus != PermissionStatus.Granted)
                {
                    LocationLabel.Text = "الرجاء منح صلاحية الموقع لحساب المواقيت";
                    return;
                }
            }

            await CheckGpsIsEnabled();
        }
        catch (Exception ex)
        {
            LocationLabel.Text = "حدث خطأ في التحقق من صلاحية الموقع";
        }
    }

    private async Task CheckGpsIsEnabled()
    {
        try
        {
            // ✅ استخدام Geolocation من Microsoft.Maui.Devices.Sensors
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location == null)
            {
                var request = new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                location = await Geolocation.GetLocationAsync(request);
            }

            if (location != null)
            {
                LocationLabel.Text = "الموقع: تم تحديد الموقع بنجاح";
                await LoadPrayerData();
                StartCompass();
            }
            else
            {
                LocationLabel.Text = "الموقع: يرجى تشغيل GPS";

                var result = await DisplayAlert("خدمات الموقع",
                    "خدمات تحديد الموقع (GPS) مغلقة. هل تريد فتح الإعدادات لتشغيلها؟",
                    "الإعدادات",
                    "إلغاء");

                if (result)
                {
                    await OpenLocationSettings();
                }
            }
        }
        catch (FeatureNotSupportedException)
        {
            LocationLabel.Text = "الموقع: جهازك لا يدعم تحديد الموقع";
        }
        catch (FeatureNotEnabledException)
        {
            LocationLabel.Text = "الموقع: خدمات الموقع مغلقة";

            var result = await DisplayAlert("خدمات الموقع",
                "خدمات تحديد الموقع (GPS) مغلقة. هل تريد فتح الإعدادات لتشغيلها؟",
                "الإعدادات",
                "إلغاء");

            if (result)
            {
                await OpenLocationSettings();
            }
        }
        catch (Exception ex)
        {
            LocationLabel.Text = "الموقع: حدث خطأ في التحقق من GPS";
            await LoadPrayerData();
        }
    }

    private async Task OpenLocationSettings()
    {
        try
        {
            // ✅ فتح إعدادات الموقع بطريقة متوافقة مع .NET MAUI
            await Launcher.OpenAsync("app-settings:location");
        }
        catch (Exception ex)
        {
            await DisplayAlert("تنبيه", "الرجاء تشغيل خدمات الموقع يدوياً من إعدادات الجهاز.", "حسناً");
        }
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

                try
                {
                    _qiblaAngle = await _prayerService.GetQiblaAngleAsync();
                    QiblaAngleLabel.Text = $"الزاوية: {_qiblaAngle:F0}°";
                }
                catch (Exception ex)
                {
                    QiblaAngleLabel.Text = "الزاوية: غير متاحة";
                }

                await _prayerService.ScheduleAdhanNotificationsAsync(data);

                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    LocationLabel.Text = "⚡ تم تفعيل الحساب المحلي الدقيق (بدون إنترنت)";
                }
                else 
                {
                    LocationLabel.Text = "✅ تم تحديث المواقيت بناءً على موقعك";
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
                LocationLabel.Text = "خطأ في جلب البيانات";
            }
        }
    }

    private void UpdateUI(PrayerData data)
    {
        HijriDateLabel.Text = $"{data.Date.Hijri.Day} {data.Date.Hijri.Month.Arabic} {data.Date.Hijri.Year}";

        string islamicEvent = GetIslamicEvent(data.Date.Hijri.Day, data.Date.Hijri.Month.Number);
        if (!string.IsNullOrEmpty(islamicEvent))
        {
            IslamicEventLabel.Text = islamicEvent;
            IslamicEventLabel.IsVisible = true;
        }
        else
        {
            IslamicEventLabel.IsVisible = false;
        }

        if (LocationLabel.Text == "جاري جلب البيانات...")
        {
            LocationLabel.Text = "الموقع: تم تحديد الموقع بنجاح";
        }

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

        return new VerticalStackLayout
        {
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

        // ترتيب الأوقات لضمان المقارنة السليمة
        var sortedTimings = timings.OrderBy(t => t.Time).ToList();
        
        var next = sortedTimings.FirstOrDefault(t => t.Time > now);
        
        if (next.Name == null)
        {
            // إذا انتهت كل صلوات اليوم، فالصلاة القادمة هي فجر الغد
            next = sortedTimings.First();
            next.Time = next.Time.AddDays(1);
        }

        NextPrayerLabel.Text = $"الصلاة القادمة: {next.Ar}";
        var diff = next.Time - now;
        CountdownLabel.Text = diff.ToString(@"hh\:mm\:ss");

        // تحديث بيانات الودجت في الخلفية
        Preferences.Default.Set("Widget_NextPrayerName", next.Ar);
        Preferences.Default.Set("Widget_NextPrayerTime", next.Time.ToString("HH:mm"));
        Preferences.Default.Set("Widget_Countdown", $"متبقي: {diff.ToString(@"hh\:mm\:ss")}");

        if (now.Second == 0)
        {
            UpdateAndroidWidget();
        }
    }

    private void UpdateAndroidWidget()
    {
#if ANDROID
        var context = Android.App.Application.Context;
        var intent = new Android.Content.Intent(context, typeof(AthkarApp.Platforms.Android.PrayerWidgetProvider));
        intent.SetAction(Android.Appwidget.AppWidgetManager.ActionAppwidgetUpdate);
        
        var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
        var componentName = new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(AthkarApp.Platforms.Android.PrayerWidgetProvider)));
        int[] appWidgetIds = appWidgetManager.GetAppWidgetIds(componentName);
        
        intent.PutExtra(Android.Appwidget.AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
        context.SendBroadcast(intent);
#endif
    }

    private DateTime GetTime(string timeStr)
    {
        if (string.IsNullOrEmpty(timeStr)) return DateTime.Now;

        // تنظيف الوقت من أي لاحقة مثل (EEST) أو مسافات زائدة
        string cleanTime = timeStr.Split(' ')[0].Trim();

        if (DateTime.TryParseExact(cleanTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            return DateTime.Today.AddHours(time.Hour).AddMinutes(time.Minute);
        }
        
        System.Diagnostics.Debug.WriteLine($"Failed to parse time: {timeStr}");
        return DateTime.Now;
    }

    private void StartCompass()
    {
        try
        {
            if (!Compass.Default.IsSupported) return;
            if (Compass.Default.IsMonitoring) return;

            Compass.Default.ReadingChanged += OnCompassReadingChanged;
            Compass.Default.Start(SensorSpeed.UI);
            _isCompassInitialized = true;
        }
        catch (Exception ex)
        {
            QiblaAngleLabel.Text = "البوصلة: غير متاحة";
        }
    }

    private void StopCompass()
    {
        try
        {
            if (!Compass.Default.IsSupported) return;
            if (!Compass.Default.IsMonitoring) return;

            Compass.Default.ReadingChanged -= OnCompassReadingChanged;
            Compass.Default.Stop();
            _isCompassInitialized = false;
        }
        catch (Exception ex)
        {
        }
    }

    private void OnCompassReadingChanged(object sender, CompassChangedEventArgs e)
    {
        if (_qiblaAngle > 0 && e.Reading.HeadingMagneticNorth > 0)
        {
            double relativeAngle = _qiblaAngle - e.Reading.HeadingMagneticNorth;

            if (relativeAngle >= -180 && relativeAngle <= 180)
            {
                QiblaNeedle.RotateTo(relativeAngle, 100);
            }
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await CheckAndEnableLocationServices();
    }

    private async void OnARModeClicked(object sender, EventArgs e)
    {
        if (!_isARMode)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("تنبيه", "نحتاج إلى صلاحية الكاميرا لتشغيل الواقع المعزز.", "حسناً");
                return;
            }

            _isARMode = true;
            ARModeBtn.Text = "❌ إيقاف الواقع المعزز";
            ARModeBtn.BackgroundColor = Color.FromArgb("#8A2E2E");
            CompassDialImg.IsVisible = false;
            CameraViewCtrl.IsVisible = true;
            
            if (CameraViewCtrl.Cameras != null && CameraViewCtrl.Cameras.Count > 0)
            {
                CameraViewCtrl.Camera = CameraViewCtrl.Cameras.FirstOrDefault(c => c.Position == Camera.MAUI.CameraPosition.Back);
                MainThread.BeginInvokeOnMainThread(async () => await CameraViewCtrl.StartCameraAsync());
            }
        }
        else
        {
            _isARMode = false;
            ARModeBtn.Text = "📷 تشغيل الواقع المعزز (AR)";
            ARModeBtn.BackgroundColor = Color.FromArgb("#1A3A1A");
            CompassDialImg.IsVisible = true;
            CameraViewCtrl.IsVisible = false;
            MainThread.BeginInvokeOnMainThread(async () => await CameraViewCtrl.StopCameraAsync());
        }
    }

    private void OnCamerasLoaded(object sender, EventArgs e)
    {
        if (CameraViewCtrl.Cameras != null && CameraViewCtrl.Cameras.Count > 0)
        {
            CameraViewCtrl.Camera = CameraViewCtrl.Cameras.FirstOrDefault(c => c.Position == Camera.MAUI.CameraPosition.Back);
        }
    }

    private string GetIslamicEvent(string dayStr, int monthNum)
    {
        if (int.TryParse(dayStr, out int day))
        {
            // الأيام البيض
            if (day == 13 || day == 14 || day == 15)
            {
                // لا نذكر صيام الأيام البيض في شهر رمضان
                if (monthNum != 9 && monthNum != 12)
                    return $"🌟 اليوم هو {day} {GetMonthName(monthNum)} - من الأيام البيض (يُستحب صيامها)";
            }

            // مناسبات السنّة
            if (monthNum == 1 && day == 9) return "🌟 غداً يوم عاشوراء (يُستحب صيامه)";
            if (monthNum == 1 && day == 10) return "🌟 اليوم يوم عاشوراء (يُستحب صيامه)";
            if (monthNum == 9)
            {
                if (day <= 10) return "🌙 نحن في العشر الأوائل من رمضان";
                if (day > 10 && day <= 20) return "🌙 نحن في العشر الأواسط من رمضان";
                if (day > 20) return "🌙 نحن في العشر الأواخر من رمضان (عتق من النار)";
            }
            if (monthNum == 10 && day == 1) return "🎉 عيد الفطر المبارك - تقبل الله طاعتكم";
            if (monthNum == 12)
            {
                if (day < 9) return "🕋 نحن في العشر الأوائل من ذي الحجة (فضل العمل الصالح)";
                if (day == 9) return "🌟 أفضل الأيام: يوم عرفة (يُستحب صيامه لغير الحاج)";
                if (day == 10) return "🎉 عيد الأضحى المبارك تقبل الله منا ومنكم";
                if (day == 11 || day == 12 || day == 13) return "🕋 أيام التشريق";
            }
            if (monthNum == 8 && day == 15) return "✨ ليلة النصف من شعبان";
        }
        
        // التحقق من فضل صيام الإثنين والخميس وفضل الجمعة
        var today = DateTime.Now.DayOfWeek;
        
        bool isEidDay = (monthNum == 10 && day == 1) || (monthNum == 12 && (day >= 10 && day <= 13));

        if (!isEidDay)
        {
            if (today == DayOfWeek.Friday) return "✨ يوم الجمعة (لا تنسَ سورة الكهف والصلاة على النبي ﷺ)";
            if (monthNum != 9)
            {
                if (today == DayOfWeek.Monday) return "✨ يوم الإثنين (تُرفع فيه الأعمال ويُستحب صيامه)";
                if (today == DayOfWeek.Thursday) return "✨ يوم الخميس (تُرفع فيه الأعمال ويُستحب صيامه)";
            }
        }

        return string.Empty;
    }

    private string GetMonthName(int monthNum)
    {
        var months = new[] { "", "محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
        if (monthNum >= 1 && monthNum <= 12) return months[monthNum];
        return "";
    }
}