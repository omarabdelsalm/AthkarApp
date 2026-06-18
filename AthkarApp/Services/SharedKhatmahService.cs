using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using AthkarApp.Models;
using System.Collections.ObjectModel;

namespace AthkarApp.Services
{
    public class SharedKhatmahService
    {
        private readonly FirebaseClient _firebaseClient;
        private const string FirebaseUrl = "https://poweracademy-8f7e2-default-rtdb.firebaseio.com/";

        public SharedKhatmahService()
        {
            _firebaseClient = new FirebaseClient(FirebaseUrl);
        }

        // إنشاء ختمة جديدة
        public async Task<SharedKhatmah> CreateKhatmahAsync(string name, string intention)
        {
            // Generate a random 4-character code like "A8F2"
            var random = new Random();
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var code = new string(Enumerable.Repeat(chars, 4).Select(s => s[random.Next(s.Length)]).ToArray());

            var khatmah = new SharedKhatmah
            {
                Id = "K-" + code,
                Name = name,
                Intention = intention,
                CreatedAt = DateTime.UtcNow,
                Parts = new List<KhatmahPart>()
            };

            // Initialize 30 parts
            for (int i = 1; i <= 30; i++)
            {
                khatmah.Parts.Add(new KhatmahPart
                {
                    PartNumber = i,
                    TakenBy = "",
                    IsCompleted = false
                });
            }

            await _firebaseClient
                .Child("SharedKhatmahs")
                .Child(khatmah.Id)
                .PutAsync(khatmah);

            return khatmah;
        }

        // جلب ختمة باستخدام الكود
        public async Task<SharedKhatmah?> GetKhatmahAsync(string khatmahId)
        {
            var result = await _firebaseClient
                .Child("SharedKhatmahs")
                .Child(khatmahId)
                .OnceSingleAsync<SharedKhatmah>();

            return result;
        }

        // حجز جزء معين
        public async Task<bool> TakePartAsync(string khatmahId, int partNumber, string userName, string deviceId)
        {
            var khatmah = await GetKhatmahAsync(khatmahId);
            if (khatmah == null) return false;

            var part = khatmah.Parts.FirstOrDefault(p => p.PartNumber == partNumber);
            if (part != null && string.IsNullOrEmpty(part.TakenBy))
            {
                part.TakenBy = userName;
                part.TakenByDeviceId = deviceId;
                part.TakenAt = DateTime.UtcNow;

                await _firebaseClient
                    .Child("SharedKhatmahs")
                    .Child(khatmahId)
                    .Child("Parts")
                    .Child((partNumber - 1).ToString())
                    .PutAsync(part);
                    
                return true;
            }
            return false;
        }

        // وضع علامة "مكتمل" على الجزء
        public async Task<bool> CompletePartAsync(string khatmahId, int partNumber)
        {
            var khatmah = await GetKhatmahAsync(khatmahId);
            if (khatmah == null) return false;

            var part = khatmah.Parts.FirstOrDefault(p => p.PartNumber == partNumber);
            if (part != null)
            {
                part.IsCompleted = true;
                part.CompletedAt = DateTime.UtcNow;

                await _firebaseClient
                    .Child("SharedKhatmahs")
                    .Child(khatmahId)
                    .Child("Parts")
                    .Child((partNumber - 1).ToString())
                    .PutAsync(part);
                    
                return true;
            }
            return false;
        }

        // إلغاء حجز الجزء (إذا تراجع الشخص)
        public async Task<bool> ReleasePartAsync(string khatmahId, int partNumber, string deviceId)
        {
            var khatmah = await GetKhatmahAsync(khatmahId);
            if (khatmah == null) return false;

            var part = khatmah.Parts.FirstOrDefault(p => p.PartNumber == partNumber);
            if (part != null && part.TakenByDeviceId == deviceId && !part.IsCompleted)
            {
                part.TakenBy = "";
                part.TakenByDeviceId = "";
                part.TakenAt = null;

                await _firebaseClient
                    .Child("SharedKhatmahs")
                    .Child(khatmahId)
                    .Child("Parts")
                    .Child((partNumber - 1).ToString())
                    .PutAsync(part);
                    
                return true;
            }
            return false;
        }

        // الاستماع للتحديثات الحية للأجزاء
        public IDisposable ListenToKhatmahParts(string khatmahId, Action<KhatmahPart> onPartUpdated)
        {
            return _firebaseClient
                .Child("SharedKhatmahs")
                .Child(khatmahId)
                .Child("Parts")
                .AsObservable<KhatmahPart>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                    {
                        onPartUpdated(d.Object);
                    }
                });
        }
    }
}
