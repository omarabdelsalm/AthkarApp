using System;
using System.Collections.Generic;

namespace AthkarApp.Models
{
    public class MaqraaSession
    {
        public string SessionId { get; set; }
        public string SheikhName { get; set; }
        public string Password { get; set; } // Hashed or plain for simple auth
        public int MaxStudents { get; set; }
        public int CurrentStudentsCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsActive { get; set; }
        public bool HasStarted { get; set; }
        
        // Dictionary of participants: Key is ParticipantId, Value is MaqraaParticipant
        public Dictionary<string, MaqraaParticipant> Participants { get; set; } = new Dictionary<string, MaqraaParticipant>();
    }

    public class MaqraaParticipant
    {
        public string ParticipantId { get; set; } // Unique ID (could be device ID or generated)
        public string Name { get; set; }
        public bool IsSheikh { get; set; }
        public bool IsMuted { get; set; } = true; // By default everyone is muted
        public bool IsSpeaking { get; set; } // Currently reading
        public bool IsHandRaised { get; set; } 
        public DateTime JoinedAt { get; set; }
    }
}
