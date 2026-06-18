using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views
{
    public partial class SharedKhatmahDetailsPage : ContentPage
    {
        private readonly SharedKhatmahService _khatmahService;
        private SharedKhatmah _currentKhatmah;
        private string _deviceId;
        private IDisposable _firebaseListener;

        public ObservableCollection<KhatmahPart> Parts { get; set; } = new ObservableCollection<KhatmahPart>();

        public ICommand TakePartCommand { get; }
        public ICommand PartDetailsCommand { get; }

        public SharedKhatmahDetailsPage(SharedKhatmah khatmah, SharedKhatmahService khatmahService)
        {
            InitializeComponent();
            _currentKhatmah = khatmah;
            _khatmahService = khatmahService;

            // Simple device ID tracking for anonymous users
            _deviceId = Preferences.Default.Get("DeviceTrackerId", "");
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString("N");
                Preferences.Default.Set("DeviceTrackerId", _deviceId);
            }

            TakePartCommand = new Command<KhatmahPart>(async (part) => await OnTakePart(part));
            PartDetailsCommand = new Command<KhatmahPart>(async (part) => await OnPartDetails(part));

            PartsCollectionView.ItemsSource = Parts;

            UpdateUI(khatmah);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Start listening to realtime changes
            _firebaseListener = _khatmahService.ListenToKhatmahParts(_currentKhatmah.Id, OnPartUpdatedFromFirebase);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _firebaseListener?.Dispose();
        }

        private void UpdateUI(SharedKhatmah khatmah)
        {
            NameLabel.Text = khatmah.Name;
            IntentionLabel.Text = khatmah.Intention;
            CodeLabel.Text = khatmah.Id.Replace("K-", "");
            
            Parts.Clear();
            if (khatmah.Parts != null)
            {
                foreach (var p in khatmah.Parts.OrderBy(x => x.PartNumber))
                {
                    Parts.Add(p);
                }
            }
            UpdateProgress();
        }

        private void OnPartUpdatedFromFirebase(KhatmahPart updatedPart)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Parts.FirstOrDefault(p => p.PartNumber == updatedPart.PartNumber);
                if (existing != null)
                {
                    // Update properties manually to trigger INotifyPropertyChanged if it was implemented
                    // or just replace it
                    int index = Parts.IndexOf(existing);
                    Parts[index] = updatedPart;
                    UpdateProgress();
                }
            });
        }

        private void UpdateProgress()
        {
            int completed = Parts.Count(p => p.IsCompleted);
            double progress = (double)completed / 30.0;
            OverallProgressBar.Progress = progress;
            ProgressTextLabel.Text = $"{completed} / 30";
        }

        private async Task OnTakePart(KhatmahPart part)
        {
            string userName = await DisplayPromptAsync("حجز الجزء", "أدخل اسمك الكريم:", "حجز", "إلغاء");
            if (string.IsNullOrWhiteSpace(userName)) return;

            bool success = await _khatmahService.TakePartAsync(_currentKhatmah.Id, part.PartNumber, userName, _deviceId);
            if (!success)
            {
                await DisplayAlert("عذراً", "يبدو أن شخصاً آخر حجز هذا الجزء للتو.", "حسناً");
            }
        }

        private async Task OnPartDetails(KhatmahPart part)
        {
            if (part.TakenByDeviceId != _deviceId && !part.IsCompleted)
            {
                await DisplayAlert("تنبيه", $"هذا الجزء محجوز بواسطة {part.TakenBy}.", "حسناً");
                return;
            }

            if (part.IsCompleted)
            {
                await DisplayAlert("معلومات", $"تم إنجاز هذا الجزء بواسطة {part.TakenBy}.", "حسناً");
                return;
            }

            string action = await DisplayActionSheet($"خيارات الجزء {part.PartNumber}", "إلغاء", null, "قراءة الجزء 📖", "إتمام الجزء ✅", "إلغاء الحجز ❌");

            if (action == "إتمام الجزء ✅")
            {
                await _khatmahService.CompletePartAsync(_currentKhatmah.Id, part.PartNumber);
            }
            else if (action == "إلغاء الحجز ❌")
            {
                bool confirm = await DisplayAlert("تأكيد", "هل تريد إلغاء حجزك لهذا الجزء؟", "نعم", "لا");
                if (confirm)
                {
                    await _khatmahService.ReleasePartAsync(_currentKhatmah.Id, part.PartNumber, _deviceId);
                }
            }
            else if (action == "قراءة الجزء 📖")
            {
                // To do: Navigate to MushafPage and set the Surah/Ayah according to the Part
                // For now just navigate to Mushaf
                await Shell.Current.GoToAsync("//MushafPage");
            }
        }

        private async void OnShareCodeClicked(object sender, EventArgs e)
        {
            string shareText = $"أدعوك للمشاركة في ختمة قرآنية: {_currentKhatmah.Name}\n" +
                               (!string.IsNullOrEmpty(_currentKhatmah.Intention) ? $"النية: {_currentKhatmah.Intention}\n" : "") +
                               $"للانضمام، حمل تطبيق أذكار وأدخل الكود: {_currentKhatmah.Id.Replace("K-", "")}";
                               
            await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new Microsoft.Maui.ApplicationModel.DataTransfer.ShareTextRequest
            {
                Title = "مشاركة كود الختمة",
                Text = shareText
            });
        }
    }
}
