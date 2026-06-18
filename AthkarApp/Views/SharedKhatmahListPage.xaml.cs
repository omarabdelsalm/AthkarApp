using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AthkarApp.Models;
using AthkarApp.Services;

namespace AthkarApp.Views
{
    public partial class SharedKhatmahListPage : ContentPage
    {
        private readonly SharedKhatmahService _khatmahService;
        public ObservableCollection<SharedKhatmah> Khatmahs { get; set; } = new ObservableCollection<SharedKhatmah>();

        public SharedKhatmahListPage(SharedKhatmahService khatmahService)
        {
            InitializeComponent();
            _khatmahService = khatmahService;
            KhatmahsCollectionView.ItemsSource = Khatmahs;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSavedKhatmahsAsync();
        }

        private async Task LoadSavedKhatmahsAsync()
        {
            KhatmahRefreshView.IsRefreshing = true;
            try
            {
                var savedIdsStr = Preferences.Default.Get("SavedKhatmahIds", "");
                if (string.IsNullOrEmpty(savedIdsStr))
                {
                    Khatmahs.Clear();
                    return;
                }

                var ids = savedIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var loadedKhatmahs = new System.Collections.Generic.List<SharedKhatmah>();

                foreach (var id in ids)
                {
                    var khatmah = await _khatmahService.GetKhatmahAsync(id);
                    if (khatmah != null)
                    {
                        loadedKhatmahs.Add(khatmah);
                    }
                }

                Khatmahs.Clear();
                foreach (var k in loadedKhatmahs.OrderByDescending(k => k.CreatedAt))
                {
                    Khatmahs.Add(k);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "فشل في تحميل الختمات المشتركة: " + ex.Message, "حسناً");
            }
            finally
            {
                KhatmahRefreshView.IsRefreshing = false;
            }
        }

        private async void OnRefreshKhatmahs(object sender, EventArgs e)
        {
            await LoadSavedKhatmahsAsync();
        }

        private async void OnCreateKhatmahClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("إنشاء ختمة جديدة", "أدخل اسم الختمة (مثال: ختمة رمضان للعائلة):", "إنشاء", "إلغاء");
            if (string.IsNullOrWhiteSpace(name)) return;

            string intention = await DisplayPromptAsync("نية الختمة (اختياري)", "أدخل نية الختمة (مثال: عن روح فلان):", "متابعة", "إلغاء");
            if (intention == null) intention = ""; // Canceled vs empty

            try
            {
                // Show loading indicator in a real app, here we just await
                var newKhatmah = await _khatmahService.CreateKhatmahAsync(name, intention);
                
                // Save to local preferences
                SaveKhatmahIdLocal(newKhatmah.Id);
                
                Khatmahs.Insert(0, newKhatmah);
                
                await DisplayAlert("نجاح", $"تم إنشاء الختمة بنجاح.\nكود الختمة هو: {newKhatmah.Id.Replace("K-", "")}\nقم بمشاركته مع الآخرين لينضموا للختمة.", "حسناً");
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "حدث خطأ أثناء الإنشاء: " + ex.Message, "حسناً");
            }
        }

        private async void OnJoinKhatmahClicked(object sender, EventArgs e)
        {
            string code = await DisplayPromptAsync("الانضمام لختمة", "أدخل كود الختمة الذي شاركه معك صديقك:", "انضمام", "إلغاء");
            if (string.IsNullOrWhiteSpace(code)) return;

            string fullId = code.StartsWith("K-") ? code.Trim().ToUpper() : "K-" + code.Trim().ToUpper();

            // Check if already joined
            var savedIdsStr = Preferences.Default.Get("SavedKhatmahIds", "");
            var ids = savedIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ids.Contains(fullId))
            {
                await DisplayAlert("تنبيه", "أنت منضم لهذه الختمة بالفعل.", "حسناً");
                return;
            }

            try
            {
                var khatmah = await _khatmahService.GetKhatmahAsync(fullId);
                if (khatmah == null)
                {
                    await DisplayAlert("خطأ", "كود الختمة غير صحيح أو الختمة غير موجودة.", "حسناً");
                    return;
                }

                SaveKhatmahIdLocal(fullId);
                Khatmahs.Insert(0, khatmah);
                await DisplayAlert("نجاح", $"تم الانضمام لختمة '{khatmah.Name}' بنجاح.", "حسناً");
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "حدث خطأ أثناء الانضمام: " + ex.Message, "حسناً");
            }
        }

        private void SaveKhatmahIdLocal(string id)
        {
            var savedIdsStr = Preferences.Default.Get("SavedKhatmahIds", "");
            var ids = savedIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!ids.Contains(id))
            {
                ids.Add(id);
                Preferences.Default.Set("SavedKhatmahIds", string.Join(",", ids));
            }
        }

        private async void OnKhatmahSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is SharedKhatmah selected)
            {
                KhatmahsCollectionView.SelectedItem = null;
                // Navigate to details page
                await Navigation.PushAsync(new SharedKhatmahDetailsPage(selected, _khatmahService));
            }
        }
    }
}
