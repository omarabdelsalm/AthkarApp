using AthkarApp.Services;
using AthkarApp.Models;
using System.Collections.ObjectModel;

namespace AthkarApp.Views
{
    public partial class SiraPage : ContentPage
    {
        private readonly ISiraService _siraService;
        public ObservableCollection<SiraSection> Sections { get; set; } = new();

        public SiraPage(ISiraService siraService)
        {
            InitializeComponent();
            _siraService = siraService;
            BindingContext = this;
            LoadSira();
        }

        protected override bool OnBackButtonPressed()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                return base.OnBackButtonPressed();
            }

            Shell.Current.GoToAsync("//AthkarPage");
            return true;
        }

        private void LoadSira()
        {
            var data = _siraService.GetAllSections();
            foreach (var section in data)
            {
                Sections.Add(section);
            }
        }
    }
}
