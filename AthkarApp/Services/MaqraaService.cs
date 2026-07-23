using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using AthkarApp.Models;

namespace AthkarApp.Services
{
    public class MaqraaService
    {
        private readonly FirebaseClient _firebaseClient;
        private const string FirebaseUrl = "https://poweracademy-8f7e2-default-rtdb.firebaseio.com/";

        public MaqraaService()
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl);
        }

        // إنشاء مقرأة جديدة (يستخدمها الشيخ)
        public async Task<MaqraaSession> CreateSessionAsync(string sheikhName, string password, int maxStudents, int durationMinutes, DateTime startTime, bool hasStarted)
        {
            // Generate a random 6-character room code
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());

            var session = new MaqraaSession
            {
                SessionId = "MQ-" + code,
                SheikhName = sheikhName,
                Password = password,
                MaxStudents = maxStudents,
                CurrentStudentsCount = 0,
                StartTime = startTime,
                EndTime = startTime.AddMinutes(durationMinutes),
                IsActive = true,
                HasStarted = hasStarted,
                Participants = new Dictionary<string, MaqraaParticipant>()
            };

            // Add Sheikh as the first participant
            var sheikhParticipant = new MaqraaParticipant
            {
                ParticipantId = "Sheikh-" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = sheikhName,
                IsSheikh = true,
                IsMuted = false, // Sheikh is unmuted by default
                IsSpeaking = true,
                JoinedAt = DateTime.UtcNow
            };

            session.Participants.Add(sheikhParticipant.ParticipantId, sheikhParticipant);

            await _firebaseClient
                .Child("MaqraaSessions")
                .Child(session.SessionId)
                .PutAsync(session);

            return session;
        }

        // جلب المقاريء النشطة للطلاب
        public async Task<List<MaqraaSession>> GetActiveSessionsAsync()
        {
            var result = await _firebaseClient
                .Child("MaqraaSessions")
                .OnceAsync<MaqraaSession>();

            var activeSessions = new List<MaqraaSession>();
            foreach (var item in result)
            {
                var session = item.Object;
                
                // حذف الجلسة من قاعدة البيانات إذا انتهى وقتها
                if (session.EndTime.ToUniversalTime() < DateTime.UtcNow)
                {
                    try
                    {
                        await _firebaseClient.Child("MaqraaSessions").Child(session.SessionId).DeleteAsync();
                    }
                    catch { }
                    continue;
                }

                if (session.IsActive)
                {
                    // Calculate current students
                    int studentsCount = session.Participants?.Values.Count(p => !p.IsSheikh) ?? 0;
                    session.CurrentStudentsCount = studentsCount;
                    activeSessions.Add(session);
                }
            }
            return activeSessions;
        }

        public async Task<MaqraaSession?> GetSessionAsync(string sessionId)
        {
            return await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .OnceSingleAsync<MaqraaSession>();
        }

        // انضمام طالب للمقرأة
        public async Task<MaqraaParticipant?> JoinSessionAsync(string sessionId, string studentName, string deviceId)
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null || !session.IsActive) return null;

            if (!session.HasStarted) return null; // Room not started yet

            int studentsCount = session.Participants?.Values.Count(p => !p.IsSheikh) ?? 0;
            if (studentsCount >= session.MaxStudents) return null; // Room full

            var participant = new MaqraaParticipant
            {
                ParticipantId = deviceId,
                Name = studentName,
                IsSheikh = false,
                IsMuted = true, // الطالب يدخل صوته مكتوم
                IsSpeaking = false,
                JoinedAt = DateTime.UtcNow
            };

            await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .Child(deviceId)
                .PutAsync(participant);

            return participant;
        }

        // انضمام الشيخ للمقرأة (لإعادة الدخول)
        public async Task<MaqraaParticipant?> JoinSessionAsSheikhAsync(string sessionId, string sheikhName)
        {
            var session = await GetSessionAsync(sessionId);
            if (session == null || !session.IsActive) return null;

            // Check if sheikh already in room
            var existingSheikh = session.Participants?.Values.FirstOrDefault(p => p.IsSheikh);
            if (existingSheikh != null) return existingSheikh; // Reuse existing participant

            var sheikhParticipant = new MaqraaParticipant
            {
                ParticipantId = "Sheikh-" + Guid.NewGuid().ToString().Substring(0, 8),
                Name = sheikhName,
                IsSheikh = true,
                IsMuted = false,
                IsSpeaking = true,
                JoinedAt = DateTime.UtcNow
            };

            await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .Child(sheikhParticipant.ParticipantId)
                .PutAsync(sheikhParticipant);

            return sheikhParticipant;
        }

        // مغادرة المقرأة
        public async Task LeaveSessionAsync(string sessionId, string participantId)
        {
            await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .Child(participantId)
                .DeleteAsync();
        }

        // كتم أو تفعيل مايك الطالب (يستخدمها الشيخ)
        public async Task ToggleStudentMuteAsync(string sessionId, string participantId, bool isMuted)
        {
            var participant = await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .Child(participantId)
                .OnceSingleAsync<MaqraaParticipant>();

            if (participant != null)
            {
                participant.IsMuted = isMuted;
                participant.IsSpeaking = !isMuted; // If unmuted, he is speaking
                
                // If unmuted, usually the hand should be lowered
                if (!isMuted) participant.IsHandRaised = false;

                await _firebaseClient
                    .Child("MaqraaSessions")
                    .Child(sessionId)
                    .Child("Participants")
                    .Child(participantId)
                    .PutAsync(participant);
            }
        }

        // رفع أو خفض يد الطالب
        public async Task ToggleHandRaisedAsync(string sessionId, string participantId, bool isRaised)
        {
            var participant = await _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .Child(participantId)
                .OnceSingleAsync<MaqraaParticipant>();

            if (participant != null)
            {
                participant.IsHandRaised = isRaised;

                await _firebaseClient
                    .Child("MaqraaSessions")
                    .Child(sessionId)
                    .Child("Participants")
                    .Child(participantId)
                    .PutAsync(participant);
            }
        }

        // إنهاء الجلسة تماماً (يستخدمها الشيخ)
        public async Task EndSessionAsync(string sessionId)
        {
            var session = await GetSessionAsync(sessionId);
            if (session != null)
            {
                session.IsActive = false;
                await _firebaseClient
                    .Child("MaqraaSessions")
                    .Child(sessionId)
                    .PutAsync(session);
            }
        }

        // بدء الجلسة المجدولة (يستخدمها الشيخ)
        public async Task<MaqraaParticipant?> StartSessionAsync(string sessionId)
        {
            var session = await GetSessionAsync(sessionId);
            if (session != null && !session.HasStarted)
            {
                session.HasStarted = true;
                // Update EndTime based on actual start time and original duration
                var duration = session.EndTime - session.StartTime;
                session.StartTime = DateTime.UtcNow;
                session.EndTime = DateTime.UtcNow.Add(duration);

                await _firebaseClient
                    .Child("MaqraaSessions")
                    .Child(sessionId)
                    .PutAsync(session);
                    
                return session.Participants?.Values.FirstOrDefault(p => p.IsSheikh);
            }
            return session?.Participants?.Values.FirstOrDefault(p => p.IsSheikh);
        }

        // الاستماع للتحديثات الحية لغرفة معينة
        public IDisposable ListenToSessionUpdates(string sessionId, Action<MaqraaSession> onSessionUpdated)
        {
            return _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .AsObservable<MaqraaSession>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                    {
                        onSessionUpdated(d.Object);
                    }
                });
        }
        
        // الاستماع لتحديثات الأعضاء بشكل منفصل
        public IDisposable ListenToParticipantsUpdates(string sessionId, Action<MaqraaParticipant, Firebase.Database.Streaming.FirebaseEventType> onParticipantUpdated)
        {
            return _firebaseClient
                .Child("MaqraaSessions")
                .Child(sessionId)
                .Child("Participants")
                .AsObservable<MaqraaParticipant>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                    {
                        onParticipantUpdated(d.Object, d.EventType);
                    }
                });
        }

        // جلب الرقم السري الموحد للشيوخ (للسماح لهم بإنشاء مقرأة)
        public async Task<string> GetSheikhPasscodeAsync()
        {
            try
            {
                var passcode = await _firebaseClient
                    .Child("AdminSettings")
                    .Child("SheikhPasscode")
                    .OnceSingleAsync<string>();
                    
                // Default passcode if not set in Firebase yet
                return string.IsNullOrEmpty(passcode) ? "12345" : passcode;
            }
            catch
            {
                return "12345"; // Fallback default
            }
        }
    }
}
