using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using AthkarApp.Services;

namespace AthkarApp.Views
{
    public partial class AchievementsPage : ContentPage
    {
        private readonly AchievementsService _achievementsService;

        public AchievementsPage(AchievementsService achievementsService)
        {
            InitializeComponent();
            _achievementsService = achievementsService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadData();
        }

        private void LoadData()
        {
            TotalQuranLabel.Text = _achievementsService.GetTotalQuranPages().ToString();
            TotalTasbeehLabel.Text = _achievementsService.GetTotalTasbeeh().ToString();

            var last7Days = _achievementsService.GetLast7Days();
            // Reverse so left to right is oldest to newest, but in RTL layout it goes right to left!
            // RTL means index 0 is on the right. We want the newest day (today) on the left.
            // So we want index 0 (rightmost) to be 6 days ago, and index 6 (leftmost) to be today.
            last7Days.Reverse(); // Now index 0 is oldest, index 6 is today.
            
            DrawQuranChart(last7Days);
            DrawTasbeehChart(last7Days);
            DrawAthkarChart(last7Days);
        }

        private void DrawQuranChart(List<Models.DailyDeeds> days)
        {
            QuranChartGrid.Children.Clear();
            QuranChartGrid.ColumnDefinitions.Clear();

            int maxPages = days.Max(d => d.QuranPagesRead);
            if (maxPages < 10) maxPages = 10; // Minimum scale

            for (int i = 0; i < 7; i++)
            {
                QuranChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var day = days[i];
                double heightRatio = (double)day.QuranPagesRead / maxPages;
                if (heightRatio < 0.05 && day.QuranPagesRead > 0) heightRatio = 0.05;

                var stack = new VerticalStackLayout { VerticalOptions = LayoutOptions.End, Spacing = 2 };

                // The bar
                var bar = new BoxView
                {
                    Color = Color.FromArgb("#D4AF37"),
                    HeightRequest = heightRatio * 90,
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    VerticalOptions = LayoutOptions.End
                };
                
                // Label above bar
                var valLabel = new Label
                {
                    Text = day.QuranPagesRead > 0 ? day.QuranPagesRead.ToString() : "",
                    FontSize = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray
                };

                // Day name
                string dayName = GetShortDayName(day.Date);
                var dayLabel = new Label
                {
                    Text = dayName,
                    FontSize = 10,
                    FontAttributes = i == 6 ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = i == 6 ? Color.FromArgb("#143214") : Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                };

                stack.Children.Add(valLabel);
                stack.Children.Add(bar);
                stack.Children.Add(dayLabel);

                Grid.SetColumn(stack, i);
                QuranChartGrid.Children.Add(stack);
            }
        }

        private void DrawTasbeehChart(List<Models.DailyDeeds> days)
        {
            TasbeehChartGrid.Children.Clear();
            TasbeehChartGrid.ColumnDefinitions.Clear();

            int maxTasbeeh = days.Max(d => d.TasbeehCount);
            if (maxTasbeeh < 100) maxTasbeeh = 100;

            for (int i = 0; i < 7; i++)
            {
                TasbeehChartGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

                var day = days[i];
                double heightRatio = (double)day.TasbeehCount / maxTasbeeh;
                if (heightRatio < 0.05 && day.TasbeehCount > 0) heightRatio = 0.05;

                var stack = new VerticalStackLayout { VerticalOptions = LayoutOptions.End, Spacing = 2 };

                var bar = new BoxView
                {
                    Color = Color.FromArgb("#2C6E2C"),
                    HeightRequest = heightRatio * 90,
                    CornerRadius = new CornerRadius(5, 5, 0, 0),
                    VerticalOptions = LayoutOptions.End
                };
                
                var valLabel = new Label
                {
                    Text = day.TasbeehCount > 0 ? FormatNumber(day.TasbeehCount) : "",
                    FontSize = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray
                };

                string dayName = GetShortDayName(day.Date);
                var dayLabel = new Label
                {
                    Text = dayName,
                    FontSize = 10,
                    FontAttributes = i == 6 ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = i == 6 ? Color.FromArgb("#143214") : Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                };

                stack.Children.Add(valLabel);
                stack.Children.Add(bar);
                stack.Children.Add(dayLabel);

                Grid.SetColumn(stack, i);
                TasbeehChartGrid.Children.Add(stack);
            }
        }

        private void DrawAthkarChart(List<Models.DailyDeeds> days)
        {
            AthkarGrid.Children.Clear();
            AthkarGrid.ColumnDefinitions.Clear();
            AthkarGrid.RowDefinitions.Clear();
            
            AthkarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Morning
            AthkarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Evening
            AthkarGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Labels

            // Add an extra column at the beginning for the title (Morning/Evening)
            AthkarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var morningLabel = new Label { Text = "الصباح", VerticalOptions = LayoutOptions.Center, FontSize = 12 };
            var eveningLabel = new Label { Text = "المساء", VerticalOptions = LayoutOptions.Center, FontSize = 12 };
            
            Grid.SetRow(morningLabel, 0); Grid.SetColumn(morningLabel, 0);
            Grid.SetRow(eveningLabel, 1); Grid.SetColumn(eveningLabel, 0);
            
            AthkarGrid.Children.Add(morningLabel);
            AthkarGrid.Children.Add(eveningLabel);

            for (int i = 0; i < 7; i++)
            {
                AthkarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                var day = days[i];

                var morningIcon = new Label
                {
                    Text = day.MorningAthkarCompleted ? "☀️" : "☁️",
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    Opacity = day.MorningAthkarCompleted ? 1.0 : 0.3
                };

                var eveningIcon = new Label
                {
                    Text = day.EveningAthkarCompleted ? "🌙" : "☁️",
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.Center,
                    Opacity = day.EveningAthkarCompleted ? 1.0 : 0.3
                };

                string dayName = GetShortDayName(day.Date);
                var dayLabel = new Label
                {
                    Text = dayName,
                    FontSize = 10,
                    FontAttributes = i == 6 ? FontAttributes.Bold : FontAttributes.None,
                    TextColor = i == 6 ? Color.FromArgb("#143214") : Colors.Gray,
                    HorizontalOptions = LayoutOptions.Center
                };

                Grid.SetRow(morningIcon, 0); Grid.SetColumn(morningIcon, i + 1);
                Grid.SetRow(eveningIcon, 1); Grid.SetColumn(eveningIcon, i + 1);
                Grid.SetRow(dayLabel, 2); Grid.SetColumn(dayLabel, i + 1);

                AthkarGrid.Children.Add(morningIcon);
                AthkarGrid.Children.Add(eveningIcon);
                AthkarGrid.Children.Add(dayLabel);
            }
        }

        private string GetShortDayName(DateTime date)
        {
            if (date.Date == DateTime.Today) return "اليوم";
            return date.ToString("ddd", new System.Globalization.CultureInfo("ar-SA"));
        }

        private string FormatNumber(int num)
        {
            if (num >= 1000)
                return (num / 1000.0).ToString("0.#") + "k";
            return num.ToString();
        }
    }
}
