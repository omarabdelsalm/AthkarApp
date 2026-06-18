using System;
using System.Collections.Generic;

namespace AthkarApp.Models
{
    public class SharedKhatmah
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Intention { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<KhatmahPart> Parts { get; set; } = new List<KhatmahPart>();

        // For local tracking (not sent to Firebase directly, but useful for UI bindings)
        [Newtonsoft.Json.JsonIgnore]
        public double ProgressPercentage 
        {
            get
            {
                if (Parts == null || Parts.Count == 0) return 0;
                int completed = Parts.Count(p => p.IsCompleted);
                return (double)completed / Parts.Count;
            }
        }
        
        [Newtonsoft.Json.JsonIgnore]
        public string ProgressText => $"{(Parts?.Count(p => p.IsCompleted) ?? 0)} / 30 جزء";
    }

    public class KhatmahPart
    {
        public int PartNumber { get; set; }
        public string TakenBy { get; set; }
        public string TakenByDeviceId { get; set; } // To identify if the current user owns it
        public bool IsCompleted { get; set; }
        public DateTime? TakenAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        [Newtonsoft.Json.JsonIgnore]
        public bool IsAvailable => string.IsNullOrEmpty(TakenBy);
    }

    public class InvertedBoolConverter : Microsoft.Maui.Controls.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : Microsoft.Maui.Controls.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isCompleted && isCompleted)
                return Microsoft.Maui.Graphics.Colors.Green;
            return Microsoft.Maui.Graphics.Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AvailableToBgConverter : Microsoft.Maui.Controls.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isAvailable && isAvailable)
                return Microsoft.Maui.Graphics.Colors.White;
            return Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
