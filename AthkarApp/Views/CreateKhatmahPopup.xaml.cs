using CommunityToolkit.Maui.Views;
using AthkarApp.Models;
using Microsoft.Maui.Graphics;

namespace AthkarApp.Views;

public partial class CreateKhatmahPopup : Popup
{
    private string _selectedColor = "#5EB670";

    // Dynamic control helpers to avoid XAML source generator lag
    private Entry? KhatmahNameEntryControl => this.FindByName<Entry>("KhatmahNameEntry");
    private Slider? ScopeSliderControl => this.FindByName<Slider>("ScopeSlider");
    private Label? DurationLabelControl => this.FindByName<Label>("DurationLabel");
    private Label? WirdLabelControl => this.FindByName<Label>("WirdLabel");
    private Label? ScopeValueLabelControl => this.FindByName<Label>("ScopeValueLabel");
    private Border? BlueBookmarkBorderControl => this.FindByName<Border>("BlueBookmarkBorder");
    private Border? OrangeBookmarkBorderControl => this.FindByName<Border>("OrangeBookmarkBorder");
    private Border? BrownBookmarkBorderControl => this.FindByName<Border>("BrownBookmarkBorder");
    private Border? RedBookmarkBorderControl => this.FindByName<Border>("RedBookmarkBorder");
    private Border? GreenBookmarkBorderControl => this.FindByName<Border>("GreenBookmarkBorder");
    private TimePicker? ReminderTimePickerControl => this.FindByName<TimePicker>("ReminderTimePicker");
    private Switch? ReminderSwitchControl => this.FindByName<Switch>("ReminderSwitch");

    public CreateKhatmahPopup()
    {
        InitializeComponent();

        // Dynamically set default Khatmah name with Arabic month name
        try
        {
            string currentMonthArabic = DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("ar-SA"));
            if (KhatmahNameEntryControl != null)
            {
                KhatmahNameEntryControl.Text = $"ختمتي - {DateTime.Now.Day} {currentMonthArabic}";
            }
        }
        catch
        {
            if (KhatmahNameEntryControl != null)
            {
                KhatmahNameEntryControl.Text = $"ختمتي - {DateTime.Now:dd-MM}";
            }
        }

        // Hook slider event
        var scopeSlider = ScopeSliderControl;
        if (scopeSlider != null)
        {
            scopeSlider.ValueChanged += OnScopeSliderValueChanged;
        }

        // Initialize UI values
        UpdateWirdAmount();
        UpdateColorSelectionUI();
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close(null);
    }

    private void OnScopeSliderValueChanged(object? sender, ValueChangedEventArgs e)
    {
        int selectedJuz = (int)Math.Round(e.NewValue);
        var scopeValueLabel = ScopeValueLabelControl;
        if (scopeValueLabel != null)
        {
            if (selectedJuz == 30)
            {
                scopeValueLabel.Text = "كامل المصحف (30 جزء)";
            }
            else
            {
                scopeValueLabel.Text = $"من الجزء 1 إلى {selectedJuz}";
            }
        }
        UpdateWirdAmount();
    }

    private void OnIncreaseDays(object sender, EventArgs e)
    {
        var durationLabel = DurationLabelControl;
        if (durationLabel != null && int.TryParse(durationLabel.Text, out int days))
        {
            days++;
            durationLabel.Text = days.ToString();
            UpdateWirdAmount();
        }
    }

    private void OnDecreaseDays(object sender, EventArgs e)
    {
        var durationLabel = DurationLabelControl;
        if (durationLabel != null && int.TryParse(durationLabel.Text, out int days) && days > 1)
        {
            days--;
            durationLabel.Text = days.ToString();
            UpdateWirdAmount();
        }
    }

    private void UpdateWirdAmount()
    {
        var wirdLabel = WirdLabelControl;
        var durationLabel = DurationLabelControl;
        var scopeSlider = ScopeSliderControl;

        if (wirdLabel == null || durationLabel == null || scopeSlider == null)
            return;

        if (!int.TryParse(durationLabel.Text, out int days) || days < 1)
            days = 30;

        int selectedJuz = (int)Math.Round(scopeSlider.Value);
        
        // Calculate total pages based on selected Juz range (Juz 1 to selectedJuz)
        int startPage = 1;
        int endPage = selectedJuz == 30 ? 604 : (selectedJuz * 20 + 1);
        int totalPages = endPage - startPage + 1;

        double pagesPerDay = (double)totalPages / days;
        double juzPerDay = (double)selectedJuz / days;

        if (juzPerDay >= 0.95)
        {
            double roundedJuz = Math.Round(juzPerDay, 1);
            wirdLabel.Text = $"{roundedJuz} جزء";
        }
        else
        {
            int roundedPages = (int)Math.Round(pagesPerDay);
            if (roundedPages < 1) roundedPages = 1;
            wirdLabel.Text = $"{roundedPages} صفحة";
        }
    }

    private void OnColorTapped(object sender, EventArgs e)
    {
        if (sender is Border clickedBorder && e is TappedEventArgs tappedEventArgs && tappedEventArgs.Parameter is string colorHex)
        {
            _selectedColor = colorHex;
            UpdateColorSelectionUI();
        }
    }

    private void UpdateColorSelectionUI()
    {
        var blueBookmark = BlueBookmarkBorderControl;
        var orangeBookmark = OrangeBookmarkBorderControl;
        var brownBookmark = BrownBookmarkBorderControl;
        var redBookmark = RedBookmarkBorderControl;
        var greenBookmark = GreenBookmarkBorderControl;

        if (blueBookmark == null || orangeBookmark == null || 
            brownBookmark == null || redBookmark == null || 
            greenBookmark == null)
            return;

        // Reset backgrounds and stroke thicknesses
        blueBookmark.BackgroundColor = Colors.Transparent;
        blueBookmark.StrokeThickness = 2;

        orangeBookmark.BackgroundColor = Colors.Transparent;
        orangeBookmark.StrokeThickness = 2;

        brownBookmark.BackgroundColor = Colors.Transparent;
        brownBookmark.StrokeThickness = 2;

        redBookmark.BackgroundColor = Colors.Transparent;
        redBookmark.StrokeThickness = 2;

        greenBookmark.BackgroundColor = Colors.Transparent;
        greenBookmark.StrokeThickness = 2;

        // Highlight the selected one with a light background shade and thicker border
        if (_selectedColor == "#3498db")
        {
            blueBookmark.BackgroundColor = Color.FromArgb("#E3F2FD");
            blueBookmark.StrokeThickness = 4;
        }
        else if (_selectedColor == "#e67e22")
        {
            orangeBookmark.BackgroundColor = Color.FromArgb("#FFF3E0");
            orangeBookmark.StrokeThickness = 4;
        }
        else if (_selectedColor == "#8d6e63")
        {
            brownBookmark.BackgroundColor = Color.FromArgb("#EFEBE9");
            brownBookmark.StrokeThickness = 4;
        }
        else if (_selectedColor == "#e74c3c")
        {
            redBookmark.BackgroundColor = Color.FromArgb("#FFEBEE");
            redBookmark.StrokeThickness = 4;
        }
        else if (_selectedColor == "#5EB670")
        {
            greenBookmark.BackgroundColor = Color.FromArgb("#E0F2E0");
            greenBookmark.StrokeThickness = 4;
        }
    }

    private void OnCreateClicked(object sender, EventArgs e)
    {
        var durationLabel = DurationLabelControl;
        var scopeSlider = ScopeSliderControl;
        var khatmahNameEntry = KhatmahNameEntryControl;
        var reminderSwitch = ReminderSwitchControl;
        var reminderTimePicker = ReminderTimePickerControl;

        if (!int.TryParse(durationLabel?.Text, out int days) || days < 1)
            days = 30;

        int selectedJuz = scopeSlider != null ? (int)Math.Round(scopeSlider.Value) : 30;
        int startPage = 1;
        int endPage = selectedJuz == 30 ? 604 : (selectedJuz * 20 + 1);

        var config = new KhatmahPlanConfig
        {
            Name = string.IsNullOrWhiteSpace(khatmahNameEntry?.Text) ? "ختمتي" : khatmahNameEntry.Text,
            DurationDays = days,
            StartPage = startPage,
            EndPage = endPage,
            ColorHex = _selectedColor,
            ReminderEnabled = reminderSwitch?.IsToggled ?? true,
            ReminderTime = reminderTimePicker?.Time ?? TimeSpan.FromHours(21)
        };

        Close(config);
    }
}

