using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using AthkarApp.Services;
using AthkarApp.Models;
using AgoraIO.Media;

namespace AthkarApp.Views
{
    public partial class MaqraaRoomPage : ContentPage
    {
        private readonly MaqraaService _maqraaService;
        private MaqraaSession _session;
        private MaqraaParticipant _currentUser;
        private IDisposable _participantsListener;

        public ObservableCollection<MaqraaParticipant> Participants { get; set; } = new ObservableCollection<MaqraaParticipant>();
        public bool IsCurrentUserSheikh => _currentUser?.IsSheikh ?? false;
        public bool IsCurrentUserNotSheikh => !IsCurrentUserSheikh;
        public Command<MaqraaParticipant> ToggleMuteCommand { get; }
        public Command ToggleMyMuteCommand { get; }
        public Command ToggleMyHandCommand { get; }

        public MaqraaRoomPage(MaqraaService maqraaService, MaqraaSession session, MaqraaParticipant currentUser)
        {
            InitializeComponent();
            _maqraaService = maqraaService;
            _session = session;
            _currentUser = currentUser;

            RoomTitleLabel.Text = $"مقرأة الشيخ: {session.SheikhName}";
            
            ToggleMuteCommand = new Command<MaqraaParticipant>(async (p) => await OnToggleMute(p));
            ToggleMyMuteCommand = new Command(async () => await OnToggleMyMute());
            ToggleMyHandCommand = new Command(async () => await OnToggleMyHand());

            BindingContext = this;
            ParticipantsList.ItemsSource = Participants;

            // Load Agora HTML
            var htmlSource = new HtmlWebViewSource
            {
                Html = LoadLocalHtmlFile()
            };
#if ANDROID
            htmlSource.BaseUrl = "https://localhost/";
#endif
            AgoraWebView.Source = htmlSource;
            AgoraWebView.Navigated += AgoraWebView_Navigated;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Request Microphone permission for Android
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Microphone>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("تنبيه", "يجب السماح بصلاحية المايكروفون لتعمل المقرأة بشكل صحيح", "حسناً");
                }
            }
            
            // Populate initial participants
            if (_session.Participants != null && !Participants.Any())
            {
                foreach (var p in _session.Participants.Values)
                {
                    Participants.Add(p);
                }
            }

            if (_participantsListener == null)
            {
                // Setup Firebase Listener for changes in Participants
                _participantsListener = _maqraaService.ListenToParticipantsUpdates(_session.SessionId, (participant, eventType) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if (eventType == Firebase.Database.Streaming.FirebaseEventType.Delete)
                        {
                            var toRemove = Participants.FirstOrDefault(p => p.ParticipantId == participant.ParticipantId);
                            if (toRemove != null) Participants.Remove(toRemove);
                        }
                        else
                        {
                            var existing = Participants.FirstOrDefault(p => p.ParticipantId == participant.ParticipantId);
                            if (existing != null)
                            {
                                int index = Participants.IndexOf(existing);
                                Participants[index] = participant; // Update state
                            }
                            else
                            {
                                Participants.Add(participant);
                            }

                            // If it's me, handle local mute/unmute
                            if (participant.ParticipantId == _currentUser.ParticipantId)
                            {
                                _currentUser = participant;
                                string isMutedStr = participant.IsMuted ? "true" : "false";
                                try { await AgoraWebView.EvaluateJavaScriptAsync($"setLocalMute({isMutedStr})"); } catch { }
                            }
                        }
                    });
                });
            }
        }

        private string LoadLocalHtmlFile()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <script src='https://download.agora.io/sdk/release/AgoraRTC_N.js'></script>
</head>
<body>
    <script>
        var client;
        var localAudioTrack;
        var isJoined = false;

        async function initAgora(appId, token, channel, uid) {
            try {
                let retries = 0;
                while (typeof AgoraRTC === 'undefined' && retries < 50) {
                    await new Promise(r => setTimeout(r, 100));
                    retries++;
                }

                if (typeof AgoraRTC === 'undefined') {
                    alert('خطأ: لم يتم تحميل مكتبة الصوت، تأكد من اتصال الإنترنت.');
                    return 'ERROR: Agora SDK failed to load.';
                }

                client = AgoraRTC.createClient({ mode: 'rtc', codec: 'vp8' });
                client.on('user-published', async (user, mediaType) => {
                    await client.subscribe(user, mediaType);
                    if (mediaType === 'audio') user.audioTrack.play();
                });
                await client.join(appId, channel, token || null, uid);
                localAudioTrack = await AgoraRTC.createMicrophoneAudioTrack();
                await client.publish([localAudioTrack]);
                isJoined = true;
                alert('تم الاتصال بالصوت بنجاح!');
                return 'SUCCESS';
            } catch (e) { 
                alert('خطأ في الاتصال: ' + e.toString()); 
                return 'ERROR: ' + e.toString(); 
            }
        }

        async function setLocalMute(isMuted) {
            if (localAudioTrack) {
                await localAudioTrack.setMuted(isMuted);
            }
        }

        async function leaveChannel() {
            try {
                if (localAudioTrack) {
                    localAudioTrack.stop();
                    localAudioTrack.close();
                }
                if (client) {
                    await client.leave();
                }
            } catch (e) {}
        }
    </script>
</body>
</html>";
        }

        private async void AgoraWebView_Navigated(object sender, WebNavigatedEventArgs e)
        {
            try
            {
                // Join Agora Channel
                string appId = "a7a563745f7e4c8bac350c814708cab8"; // Agora App ID (AthkarTest)
                string appCert = "37c3a8bd02ae483ca3be61187d43a20a";
                string channel = _session.SessionId;
                string uid = _currentUser.ParticipantId; 
                
                // توليد التوكن برمجياً بصلاحية 24 ساعة
                uint privilegeTs = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 86400);
                string token = RtcTokenBuilder.buildTokenWithUserAccount(appId, appCert, channel, uid, RtcTokenBuilder.Role.RolePublisher, privilegeTs);
                
                // Note: This relies on the WebView executing JS.
                string result = await AgoraWebView.EvaluateJavaScriptAsync($"initAgora('{appId}', '{token}', '{channel}', '{uid}')");

                if (!string.IsNullOrEmpty(result) && result.Contains("ERROR"))
                {
                    System.Diagnostics.Debug.WriteLine($"Agora Initialization Error: {result}");
                    MainThread.BeginInvokeOnMainThread(async () => {
                        await DisplayAlert("خطأ في الاتصال", "فشل تشغيل المايكروفون: " + result, "حسناً");
                    });
                }

                // If I am not sheikh, I join muted
                if (!_currentUser.IsSheikh)
                {
                    await AgoraWebView.EvaluateJavaScriptAsync("setLocalMute(true)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Agora initialization exception: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(async () => {
                    await DisplayAlert("خطأ برمجي", "حدث انهيار داخلي: " + ex.Message, "حسناً");
                });
            }
        }

        private async Task OnToggleMute(MaqraaParticipant participant)
        {
            if (!IsCurrentUserSheikh) return; // Only Sheikh can mute/unmute
            
            bool newMuteState = !participant.IsMuted;
            await _maqraaService.ToggleStudentMuteAsync(_session.SessionId, participant.ParticipantId, newMuteState);
        }

        private async Task OnToggleMyMute()
        {
            if (_currentUser != null)
            {
                bool newMuteState = !_currentUser.IsMuted;
                await _maqraaService.ToggleStudentMuteAsync(_session.SessionId, _currentUser.ParticipantId, newMuteState);
            }
        }

        private async Task OnToggleMyHand()
        {
            if (_currentUser != null && !_currentUser.IsSheikh)
            {
                bool newHandState = !_currentUser.IsHandRaised;
                await _maqraaService.ToggleHandRaisedAsync(_session.SessionId, _currentUser.ParticipantId, newHandState);
            }
        }

        private async void OnLeaveClicked(object sender, EventArgs e)
        {
            if (_participantsListener != null) _participantsListener.Dispose();
            
            try { await AgoraWebView.EvaluateJavaScriptAsync("leaveChannel()"); } catch { }

            if (IsCurrentUserSheikh)
            {
                await _maqraaService.EndSessionAsync(_session.SessionId);
            }
            else
            {
                await _maqraaService.LeaveSessionAsync(_session.SessionId, _currentUser.ParticipantId);
            }

            await Navigation.PopAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_participantsListener != null) _participantsListener.Dispose();
            try { AgoraWebView.EvaluateJavaScriptAsync("leaveChannel()"); } catch { }
        }

        protected override bool OnBackButtonPressed()
        {
            OnLeaveClicked(null, null);
            return true;
        }
    }
}
