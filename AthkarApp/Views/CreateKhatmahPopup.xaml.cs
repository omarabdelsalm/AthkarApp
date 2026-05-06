using CommunityToolkit.Maui.Views;

namespace AthkarApp.Views;

public partial class CreateKhatmahPopup : Popup
{
    public CreateKhatmahPopup()
    {
        InitializeComponent();
        ScopeSlider.ValueChanged += (s, e) => {
            // Update something if needed
        };
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }

    private void OnIncreaseDays(object sender, EventArgs e)
    {
        if (int.TryParse(DurationLabel.Text, out int days))
        {
            days++;
            DurationLabel.Text = days.ToString();
            UpdateWirdAmount(days);
        }
    }

    private void OnDecreaseDays(object sender, EventArgs e)
    {
        if (int.TryParse(DurationLabel.Text, out int days) && days > 1)
        {
            days--;
            DurationLabel.Text = days.ToString();
            UpdateWirdAmount(days);
        }
    }

    private void UpdateWirdAmount(int days)
    {
        double juzPerDay = 30.0 / days;
        if (juzPerDay >= 1)
        {
            WirdLabel.Text = $"{(int)Math.Round(juzPerDay)} جزء";
        }
        else
        {
            double pagesPerDay = 604.0 / days;
            WirdLabel.Text = $"{(int)Math.Round(pagesPerDay)} صفحة";
        }
    }

    private void OnCreateClicked(object sender, EventArgs e)
    {
        if (int.TryParse(DurationLabel.Text, out int days))
        {
            Close(days);
        }
        else
        {
            Close(30);
        }
    }
}
