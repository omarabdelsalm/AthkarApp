using System;
using Microsoft.Maui.Controls;
using AthkarApp.Services;
using AthkarApp.Models;

namespace AthkarApp.Views
{
    public partial class CreateMaqraaPage : ContentPage
    {
        private readonly MaqraaService _maqraaService;

        public CreateMaqraaPage(MaqraaService maqraaService)
        {
            InitializeComponent();
            _maqraaService = maqraaService;
        }

        private async void OnStartClicked(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            btn.IsEnabled = false;

            try
            {
                if (string.IsNullOrWhiteSpace(SheikhNameEntry.Text) ||
                    string.IsNullOrWhiteSpace(MaxStudentsEntry.Text) ||
                    string.IsNullOrWhiteSpace(DurationEntry.Text) ||
                    string.IsNullOrWhiteSpace(PasswordEntry.Text))
                {
                    await DisplayAlert("تنبيه", "الرجاء تعبئة جميع الحقول", "حسناً");
                    return;
                }

            if (!int.TryParse(MaxStudentsEntry.Text, out int maxStudents) || 
                !int.TryParse(DurationEntry.Text, out int duration))
            {
                await DisplayAlert("تنبيه", "يجب أن تكون الأرقام صحيحة", "حسناً");
                return;
            }

            bool isScheduled = ScheduleSwitch.IsToggled;
                DateTime startTime = DateTime.UtcNow;
                bool hasStarted = true;

                if (isScheduled)
                {
                    DateTime selectedDate = SessionDatePicker.Date;
                    TimeSpan selectedTime = SessionTimePicker.Time;
                    startTime = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day, selectedTime.Hours, selectedTime.Minutes, 0, DateTimeKind.Local).ToUniversalTime();
                    hasStarted = false;

                    if (startTime <= DateTime.UtcNow)
                    {
                        await DisplayAlert("تنبيه", "تاريخ ووقت الجدولة يجب أن يكون في المستقبل", "حسناً");
                        return;
                    }
                }

                // Create Session in Firebase
                var session = await _maqraaService.CreateSessionAsync(
                    SheikhNameEntry.Text, 
                    PasswordEntry.Text, 
                    maxStudents, 
                    duration,
                    startTime,
                    hasStarted);

                // Find Sheikh Participant Object (the first one)
                MaqraaParticipant sheikhParticipant = null;
                foreach (var p in session.Participants.Values)
                {
                    if (p.IsSheikh)
                    {
                        sheikhParticipant = p;
                        break;
                    }
                }

                if (isScheduled)
                {
                    await DisplayAlert("نجاح", "تمت جدولة المقرأة بنجاح. ستظهر للطلاب ويمكنك تفعيلها في وقتها.", "حسناً");
                    await Navigation.PopAsync();
                }
                else if (sheikhParticipant != null)
                {
                    // Navigate to Room Page
                    await Navigation.PushAsync(new MaqraaRoomPage(_maqraaService, session, sheikhParticipant));
                    
                    // Remove current page from stack so back button doesn't come here
                    Navigation.RemovePage(this);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ", "لم نتمكن من إنشاء المقرأة: " + ex.Message, "حسناً");
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
    }
}
