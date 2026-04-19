using Microsoft.Maui.Controls;
using System;

namespace AthkarApp.Views
{
    public partial class PrivacyPolicyPage : ContentPage
    {
        public PrivacyPolicyPage()
        {
            InitializeComponent();
        }

        private async void OnOkClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
