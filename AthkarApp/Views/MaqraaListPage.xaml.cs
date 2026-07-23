using System;
using Microsoft.Maui.Controls;
using AthkarApp.Services;
using AthkarApp.Models;
using System.Threading.Tasks;

namespace AthkarApp.Views
{
    public partial class MaqraaListPage : ContentPage
    {
        private readonly MaqraaService _maqraaService;
        
        public System.Collections.ObjectModel.ObservableCollection<MaqraaSession> Sessions { get; set; } = new System.Collections.ObjectModel.ObservableCollection<MaqraaSession>();

        public MaqraaListPage(MaqraaService maqraaService)
        {
            InitializeComponent();
            _maqraaService = maqraaService;
            SessionsList.ItemsSource = Sessions;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSessions();
        }

        private async Task LoadSessions()
        {
            SessionsRefreshView.IsRefreshing = true;
            try
            {
                var sessions = await _maqraaService.GetActiveSessionsAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Sessions.Clear();
                    foreach (var s in sessions)
                    {
                        Sessions.Add(s);
                    }
                });
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "فشل في تحميل المقاريء: " + ex.Message, "حسناً");
            }
            finally
            {
                SessionsRefreshView.IsRefreshing = false;
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadSessions();
        }

        private void OnSessionTapped(object sender, TappedEventArgs e)
        {
            var session = e.Parameter as MaqraaSession;
            if (session != null)
            {
                HandleSessionJoin(session);
            }
        }

        private void OnJoinButtonClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var session = button?.CommandParameter as MaqraaSession;
            if (session != null)
            {
                HandleSessionJoin(session);
            }
        }

        private async void HandleSessionJoin(MaqraaSession session)
        {
            try
            {
                // Fetch the latest session state from the server to ensure HasStarted is accurate
                var latestSession = await _maqraaService.GetSessionAsync(session.SessionId);
                if (latestSession == null || !latestSession.IsActive)
                {
                    await DisplayAlert("تنبيه", "هذه الجلسة غير متاحة حالياً.", "حسناً");
                    await LoadSessions(); // Refresh list
                    return;
                }

                session = latestSession;

                string action = await DisplayActionSheet("كيف تود الدخول إلى هذه المقرأة؟", "إلغاء", null, "دخول كطالب", "أنا شيخ المقرأة");
                if (action == "إلغاء" || string.IsNullOrEmpty(action)) return;

                if (action == "أنا شيخ المقرأة")
                {
                    string enteredPasscode = await DisplayPromptAsync("تأكيد الصلاحية", "أدخل كلمة مرور المقرأة أو الرقم السري العام:", "موافق", "إلغاء", null, -1, Keyboard.Numeric, "");
                    if (string.IsNullOrEmpty(enteredPasscode)) return;

                    string correctPasscode = await _maqraaService.GetSheikhPasscodeAsync();
                    if (enteredPasscode == correctPasscode || enteredPasscode == session.Password)
                    {
                        if (!session.HasStarted)
                        {
                            var sheikhParticipant = await _maqraaService.StartSessionAsync(session.SessionId);
                            if (sheikhParticipant != null)
                            {
                                await Navigation.PushAsync(new MaqraaRoomPage(_maqraaService, session, sheikhParticipant));
                            }
                            else
                            {
                                await DisplayAlert("خطأ", "حدث خطأ أثناء تفعيل الجلسة.", "حسناً");
                            }
                        }
                        else
                        {
                            // Re-joining an already started session as Sheikh
                            var sheikhParticipant = await _maqraaService.JoinSessionAsSheikhAsync(session.SessionId, session.SheikhName);
                            if (sheikhParticipant != null)
                            {
                                await Navigation.PushAsync(new MaqraaRoomPage(_maqraaService, session, sheikhParticipant));
                            }
                            else
                            {
                                await DisplayAlert("خطأ", "حدث خطأ أثناء الدخول.", "حسناً");
                            }
                        }
                    }
                    else
                    {
                        await DisplayAlert("مرفوض", "كلمة المرور أو الرقم السري غير صحيح.", "حسناً");
                    }
                    return;
                }

                // If joining as student ("دخول كطالب")
                if (!session.HasStarted)
                {
                    await DisplayAlert("تنبيه", "الرجاء الانتظار حتى يفتح الشيخ الغرفة.", "حسناً");
                    return;
                }

                string name = await DisplayPromptAsync("دخول المقرأة", "أدخل اسمك الكريم للانضمام:", "دخول", "إلغاء");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    string deviceId = Guid.NewGuid().ToString().Substring(0, 8); // Simplification for device id
                    var participant = await _maqraaService.JoinSessionAsync(session.SessionId, name, deviceId);
                    
                    if (participant != null)
                    {
                        // Navigate to Room Page
                        await Navigation.PushAsync(new MaqraaRoomPage(_maqraaService, session, participant));
                    }
                    else
                    {
                        await DisplayAlert("عذراً", "الحد الأقصى للطلاب مكتمل أو الغرفة مغلقة.", "حسناً");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "حدث خطأ أثناء محاولة الانضمام: " + ex.Message, "حسناً");
            }
        }

        private async void OnCreateClicked(object sender, EventArgs e)
        {
            string enteredPasscode = await DisplayPromptAsync("تأكيد الصلاحية", "أدخل الرقم السري الخاص بالشيوخ المعتمدين:", "موافق", "إلغاء", null, -1, Keyboard.Numeric, "");
            
            if (string.IsNullOrEmpty(enteredPasscode)) return;

            string correctPasscode = await _maqraaService.GetSheikhPasscodeAsync();
            
            if (enteredPasscode == correctPasscode)
            {
                await Navigation.PushAsync(new CreateMaqraaPage(_maqraaService));
            }
            else
            {
                await DisplayAlert("مرفوض", "الرقم السري غير صحيح. هذه الخاصية للشيوخ المعتمدين فقط.", "حسناً");
            }
        }
    }
}
