using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AthkarApp.Services;

namespace AthkarApp.Views
{
    [QueryProperty(nameof(SurahNumber), "SurahNumber")]
    public partial class QuranUthmaniPage : ContentPage
    {
        private readonly IQuranApiService _quranApiService;
        private int _currentPage = 1;
        private int _surahNumber = 1;
        private bool _isAutoScrolling = false;
        private int _scrollSpeed = 1;

        public int SurahNumber
        {
            get => _surahNumber;
            set
            {
                _surahNumber = value;
                LoadSurahInitialPage();
            }
        }

        public QuranUthmaniPage(IQuranApiService quranApiService)
        {
            InitializeComponent();
            _quranApiService = quranApiService;
        }

        private async void LoadSurahInitialPage()
        {
            try
            {
                var ayahs = await _quranApiService.GetAyahsAsync(_surahNumber);
                if (ayahs != null && ayahs.Any())
                {
                    _currentPage = ayahs.First().Page;
                }
                else
                {
                    _currentPage = 1;
                }
                await LoadQuranContentAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "حدث خطأ أثناء تحميل السورة: " + ex.Message, "حسناً");
            }
        }

        private async Task LoadQuranContentAsync()
        {
            try
            {
                if (_currentPage < 1) _currentPage = 1;
                if (_currentPage > 604) _currentPage = 604;

                var pageData = await _quranApiService.GetPageAsync(_currentPage);
                var ayahs = pageData.Ayahs;
                string htmlContent = GenerateQuranHtml(ayahs, _currentPage);
                
                var htmlSource = new HtmlWebViewSource
                {
                    Html = htmlContent
                };

#if ANDROID
                htmlSource.BaseUrl = "file:///android_asset/";
#endif

                QuranWebView.Source = htmlSource;
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "تعذر تحميل المصحف: " + ex.Message, "حسناً");
            }
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("mushaf://"))
            {
                e.Cancel = true;
                string action = e.Url.Replace("mushaf://", "");
                
                if (action == "next")
                {
                    _currentPage++;
                    _ = LoadQuranContentAsync();
                }
                else if (action == "prev")
                {
                    _currentPage--;
                    _ = LoadQuranContentAsync();
                }
            }
        }

        private string GenerateQuranHtml(System.Collections.Generic.List<AthkarApp.Models.Ayah> ayahsList, int pageNum)
        {
            string fontFaceCss = @"@font-face {
                font-family: 'KFGQPC Uthman Taha Naskh';
                src: url('UthmanicHafs.ttf') format('truetype');
            }";

            StringBuilder ayahsHtml = new StringBuilder();
            string currentSurahName = "";
            string surahNumberArabic = "";
            string revelationType = "";

            if (ayahsList != null && ayahsList.Any())
            {
                var firstAyah = ayahsList.First();
                if (firstAyah.Surah != null)
                {
                    string name = firstAyah.Surah.Name.Replace("سورة", "").Trim();
                    currentSurahName = "سُورَةُ " + name;
                    
                    revelationType = firstAyah.Surah.RevelationType;
                    if (revelationType == "Meccan" || revelationType == "مكية") revelationType = "مكية";
                    else if (revelationType == "Medinan" || revelationType == "مدنية") revelationType = "مدنية";
                    
                    surahNumberArabic = firstAyah.Surah.Number.ToString()
                        .Replace("0", "٠").Replace("1", "١").Replace("2", "٢")
                        .Replace("3", "٣").Replace("4", "٤").Replace("5", "٥")
                        .Replace("6", "٦").Replace("7", "٧").Replace("8", "٨").Replace("9", "٩");
                }
                else
                {
                    currentSurahName = "سُورَةُ " + firstAyah.SurahNumber;
                    surahNumberArabic = firstAyah.SurahNumber.ToString();
                    revelationType = "";
                }

                foreach (var ayah in ayahsList)
                {
                    string ayahText = ayah.Text;

                    if (ayah.NumberInSurah == 1 && ayah.SurahNumber != 1 && ayah.SurahNumber != 9)
                    {
                        // استخراج البسملة من بداية الآية الأولى لتجنب تكرارها وعرضها بشكل منسق في المنتصف
                        string bismillahPattern1 = "ٱلرَّحِيمِ";
                        string bismillahPattern2 = "الرَّحِيمِ";
                        
                        int idx1 = ayahText.IndexOf(bismillahPattern1);
                        int idx2 = ayahText.IndexOf(bismillahPattern2);
                        
                        int matchIdx = -1;
                        int patternLen = 0;
                        
                        if (idx1 > 0 && idx1 < 50) { matchIdx = idx1; patternLen = bismillahPattern1.Length; }
                        else if (idx2 > 0 && idx2 < 50) { matchIdx = idx2; patternLen = bismillahPattern2.Length; }
                        
                        if (matchIdx != -1)
                        {
                            string extractedBismillah = ayahText.Substring(0, matchIdx + patternLen).Trim();
                            ayahText = ayahText.Substring(matchIdx + patternLen).Trim();
                            
                            // إزالة أي رموز أو مسافات إضافية في بداية الآية
                            if (ayahText.StartsWith("بِسْمِ")) ayahText = ayahText.Replace("بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ", "").Trim();
                            
                            ayahsHtml.Append($"<div class='bismillah'>{extractedBismillah}</div>");
                        }
                        else 
                        {
                            // في حال لم يتمكن من العثور عليها بدقة، نضع البسملة الافتراضية
                            ayahsHtml.Append("<div class='bismillah'>بِسْمِ اللَّهِ الرَّحْمَٰنِ الرَّحِيمِ</div>");
                        }
                    }
                    
                    ayahsHtml.Append($"{ayahText} <span class='ayah-number'>﴿{ayah.NumberInSurah}﴾</span> ");
                }
            }
            else
            {
                ayahsHtml.Append("<div style='text-align:center;'>جاري تحميل الصفحة، تأكد من اتصالك بالإنترنت إذا لم تقم بمزامنة المصحف.</div>");
                currentSurahName = "المصحف";
            }

            return $@"
<!DOCTYPE html>
<html lang='ar' dir='rtl'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <style>
        {fontFaceCss}
        :root {{
            --primary-color: #000000;
            --bg-color: #FFF8E7;
            --border-color: #A97C50;
        }}
        body {{
            background-color: var(--bg-color);
            margin: 0;
            padding: 10px;
            display: flex;
            justify-content: center;
            overflow-x: hidden;
            touch-action: pan-y;
        }}
        .mushaf-container {{
            max-width: 700px;
            width: 100%;
            border: 3px double var(--border-color);
            padding: 15px 20px;
            box-sizing: border-box;
            background-color: var(--bg-color);
            box-shadow: inset 0 0 10px rgba(169, 124, 80, 0.1);
            position: relative;
            min-height: 90vh;
        }}
        
        /* إطار السورة المزخرف عبر CSS */
        .surah-header-container {{
            position: relative;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border: 2px solid var(--border-color);
            margin-bottom: 25px;
            margin-top: 10px;
            background-color: var(--bg-color);
            height: 50px;
            box-shadow: inset 0 0 0 2px var(--bg-color), inset 0 0 0 3px var(--border-color);
        }}
        
        .surah-header-container::before, .surah-header-container::after {{
            content: '';
            position: absolute;
            top: 50%;
            transform: translateY(-50%);
            width: 44px;
            height: 44px;
            border: 2px solid var(--border-color);
            border-radius: 50%;
            background-color: var(--bg-color);
            z-index: 1;
            box-shadow: inset 0 0 0 2px var(--bg-color), inset 0 0 0 3px var(--border-color);
        }}
        .surah-header-container::before {{
            right: 15px; /* الدائرة اليمنى لرقم السورة (RTL) */
        }}
        .surah-header-container::after {{
            left: 15px; /* الدائرة اليسرى لمكية/مدنية (RTL) */
        }}
        
        .surah-header-right, .surah-header-left {{
            width: 75px;
            text-align: center;
            font-size: 14px;
            font-family: 'KFGQPC Uthman Taha Naskh', 'Noto Naskh Arabic', serif;
            color: var(--border-color);
            z-index: 2;
            position: relative;
            font-weight: bold;
        }}
        
        .surah-header-center {{
            flex-grow: 1;
            text-align: center;
            font-size: 26px;
            font-family: 'KFGQPC Uthman Taha Naskh', 'Noto Naskh Arabic', serif;
            color: var(--primary-color);
            z-index: 2;
        }}

        .bismillah {{
            text-align: center;
            font-family: 'KFGQPC Uthman Taha Naskh', 'Noto Naskh Arabic', serif;
            font-size: 26px;
            margin-bottom: 25px;
            color: var(--primary-color);
        }}
        .ayahs-content {{
            font-family: 'KFGQPC Uthman Taha Naskh', 'Noto Naskh Arabic', serif;
            font-size: 30px;
            line-height: 2.2;
            text-align: justify;
            text-justify: kashida;
            color: var(--primary-color);
        }}
        .ayah-number {{
            color: var(--border-color);
            font-size: 24px;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            font-size: 16px;
            color: #555;
            font-family: 'Arial', sans-serif;
        }}
    </style>
</head>
<body>
    <div class='mushaf-container' id='swipeZone'>
        <div class='surah-header-container'>
            <div class='surah-header-right'>{surahNumberArabic}</div>
            <div class='surah-header-center' style='white-space: nowrap;'>{currentSurahName}</div>
            <div class='surah-header-left'>{revelationType}</div>
        </div>
        <div class='ayahs-content'>
            {ayahsHtml.ToString()}
        </div>
        <div class='footer'>- {pageNum} -</div>
    </div>

    <script>
        // 1. التنقل عبر السحب (Swipe) - تم تحسين الحساسية
        let touchstartX = 0;
        let touchendX = 0;
        let touchstartY = 0;
        let touchendY = 0;
        
        const zone = document.getElementById('swipeZone');

        zone.addEventListener('touchstart', function(event) {{
            touchstartX = event.changedTouches[0].screenX;
            touchstartY = event.changedTouches[0].screenY;
        }}, false);

        zone.addEventListener('touchend', function(event) {{
            touchendX = event.changedTouches[0].screenX;
            touchendY = event.changedTouches[0].screenY;
            handleSwipe();
        }}, false); 

        function handleSwipe() {{
            const threshold = 40; // تقليل الحد الأدنى لتسهيل السحب
            const yThreshold = 80; // السماح بميلان عمودي أكبر قليلاً
            
            let swipeDistX = touchendX - touchstartX;
            let swipeDistY = Math.abs(touchendY - touchstartY);
            
            if (swipeDistY > yThreshold) return; 
            
            if (swipeDistX > threshold) {{
                window.location.href = 'mushaf://prev';
            }} else if (swipeDistX < -threshold) {{
                window.location.href = 'mushaf://next';
            }}
        }}

        // 2. التنقل عبر اللمس (Tap Zones) - الأسهل والأكثر شيوعاً في تطبيقات المصحف
        zone.addEventListener('click', function(event) {{
            const screenWidth = window.innerWidth;
            const clickX = event.clientX;
            
            // إذا تم الضغط على الثلث الأيسر من الشاشة (الصفحة التالية)
            if (clickX < screenWidth * 0.3) {{
                window.location.href = 'mushaf://next';
            }} 
            // إذا تم الضغط على الثلث الأيمن من الشاشة (الصفحة السابقة)
            else if (clickX > screenWidth * 0.7) {{
                window.location.href = 'mushaf://prev';
            }}
            // الضغط في المنتصف لا يفعل شيئاً (يمكن تخصيصه مستقبلاً لإظهار/إخفاء القوائم)
        }}, false);
        // 3. التحرك التلقائي (Auto-Scroll) بحركة فائقة السلاسة (requestAnimationFrame)
        let autoScrollReqId = null;
        let lastTime = 0;
        let scrollAccumulator = 0;
        
        function startAutoScroll(speed) {{
            stopAutoScroll();
            
            // تحديد السرعة: بكسل في الثانية
            let pixelsPerSecond = 20; // 1x
            if(speed == 2) pixelsPerSecond = 45; // 2x
            if(speed == 3) pixelsPerSecond = 80; // 3x
            
            lastTime = performance.now();
            
            function step(time) {{
                const dt = time - lastTime;
                lastTime = time;
                
                // تجميع الأجزاء العشرية للبكسلات لضمان دقة الحركة
                scrollAccumulator += (pixelsPerSecond * dt) / 1000;
                
                if (scrollAccumulator >= 1) {{
                    let pixelsToScroll = Math.floor(scrollAccumulator);
                    window.scrollBy(0, pixelsToScroll);
                    scrollAccumulator -= pixelsToScroll;
                }}
                
                // الاستمرار بالتحرك إذا لم نصل لنهاية الصفحة
                if ((window.innerHeight + window.scrollY) < document.body.offsetHeight) {{
                    autoScrollReqId = requestAnimationFrame(step);
                }} else {{
                    stopAutoScroll();
                }}
            }}
            
            autoScrollReqId = requestAnimationFrame(step);
        }}
        
        function stopAutoScroll() {{
            if(autoScrollReqId) {{
                cancelAnimationFrame(autoScrollReqId);
                autoScrollReqId = null;
            }}
        }}
    </script>
</body>
</html>";
        }
        // ===================== التحرك التلقائي (Auto-Scroll) =====================
        private void OnAutoScrollClicked(object sender, EventArgs e)
        {
            if (_isAutoScrolling) StopAutoScroll();
            else StartAutoScroll();
        }

        private void StartAutoScroll()
        {
            _isAutoScrolling = true;
            AutoScrollBtn.Text = "⏹️";
            SpeedControls.IsVisible = true;
            _ = QuranWebView.EvaluateJavaScriptAsync($"startAutoScroll({_scrollSpeed});");
        }

        private void StopAutoScroll()
        {
            _isAutoScrolling = false;
            AutoScrollBtn.Text = "▶️";
            SpeedControls.IsVisible = false;
            _ = QuranWebView.EvaluateJavaScriptAsync("stopAutoScroll();");
        }

        private void OnSpeedClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.CommandParameter?.ToString(), out int speed))
            {
                _scrollSpeed = speed;
                foreach (var child in SpeedControls.Children)
                {
                    if (child is Button b)
                        b.BackgroundColor = (b.CommandParameter?.ToString() == speed.ToString()) 
                            ? Color.FromArgb("#88FFFFFF") : Color.FromArgb("#44FFFFFF");
                }
                
                if (_isAutoScrolling)
                {
                    _ = QuranWebView.EvaluateJavaScriptAsync($"startAutoScroll({_scrollSpeed});");
                }
            }
        }
    }
}
