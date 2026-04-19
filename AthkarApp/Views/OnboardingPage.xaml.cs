using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;

namespace AthkarApp.Views
{
    public partial class OnboardingPage : ContentPage
    {
        public ObservableCollection<OnboardingScreen> Screens { get; set; } = new();

        public OnboardingPage()
        {
            InitializeComponent();

            Screens.Add(new OnboardingScreen 
            { 
                Title = "أهلاً بك في رفيقك الإسلامي", 
                Description = "تطبيق متكامل يقدم لك المصحف الشريف والختمة والمواقيت والأذكار، صُنع بعناية ليناسب وتيرتك اليومية.",
                Image = "athkar_icon.jpg"
            });
            Screens.Add(new OnboardingScreen 
            { 
                Title = "المصحف الشريف والختمة", 
                Description = "تابع ختمتك بكل سهولة، واحفظ آخر صفحة قرأتها بميزة العلامات المرجعية التلقائية.",
                Image = "quran_icon.jpg"
            });
            Screens.Add(new OnboardingScreen 
            { 
                Title = "مواقيت دقيقة وتنبيهات الأذان", 
                Description = "مواقيت صلاة متزامنة مع موقعك، مع إشعارات للأذان في الخلفية حتى بدون إنترنت.",
                Image = "mosque_athkar.jpg"
            });
            Screens.Add(new OnboardingScreen 
            { 
                Title = "القبلة بالواقع المعزز (AR)", 
                Description = "استخدم الكاميرا كبوصلة متطورة لرؤية اتجاه القبلة بوضوح ودقة غير مسبوقة.",
                Image = "compass_dial_premium.jpg"
            });

            OnboardingCarousel.ItemsSource = Screens;
            OnboardingCarousel.PositionChanged += OnCarouselPositionChanged;
        }

        private void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (e.CurrentPosition == Screens.Count - 1)
            {
                NextStartBtn.Text = "ابدأ الآن 🚀";
                NextStartBtn.BackgroundColor = Color.FromArgb("#2C6E2C");
                NextStartBtn.TextColor = Colors.White;
            }
            else
            {
                NextStartBtn.Text = "التالي";
                NextStartBtn.BackgroundColor = Color.FromArgb("#D4AF37");
                NextStartBtn.TextColor = Color.FromArgb("#143214");
            }
        }

        private async void OnNextClicked(object sender, EventArgs e)
        {
            if (OnboardingCarousel.Position < Screens.Count - 1)
            {
                OnboardingCarousel.Position += 1;
            }
            else
            {
                // حفظ التفضيل أنه تم إنهاء مرحلة التهيئة
                Preferences.Default.Set("IsFirstLaunch", false);
                
                // الانتقال للشاشة الرئيسية بطريقة توافق .NET 9
                if (Application.Current != null && Application.Current.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
                else if (Application.Current != null)
                {
                    #pragma warning disable CS0618
                    Application.Current.MainPage = new AppShell();
                    #pragma warning restore CS0618
                }
            }
        }
    }

    public class OnboardingScreen
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
    }
}
