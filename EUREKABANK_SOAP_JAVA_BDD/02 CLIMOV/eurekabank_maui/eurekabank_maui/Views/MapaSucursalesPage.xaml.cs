using Eurekabank_Maui.ViewModels;
using Eurekabank_Maui.Services;

namespace Eurekabank_Maui.Views
{
    public partial class MapaSucursalesPage : ContentPage
    {
        private readonly MapaSucursalesViewModel _viewModel;

        public MapaSucursalesPage(IEurekabankService service)
        {
            InitializeComponent();

            _viewModel = new MapaSucursalesViewModel(mapa, service);
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InicializarAsync();
        }
    }
}
