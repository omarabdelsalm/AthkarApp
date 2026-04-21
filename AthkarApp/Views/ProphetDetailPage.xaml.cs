using AthkarApp.Models;

namespace AthkarApp.Views
{
    public partial class ProphetDetailPage : ContentPage
    {
        public ProphetDetailPage(ProphetStory story)
        {
            InitializeComponent();
            BindingContext = story;
        }
    }
}
