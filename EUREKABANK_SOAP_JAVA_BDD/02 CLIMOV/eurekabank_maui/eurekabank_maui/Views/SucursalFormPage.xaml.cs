using Eurekabank_Maui.ViewModels;

namespace Eurekabank_Maui.Views
{
    public partial class SucursalFormPage : ContentPage
    {
        public SucursalFormPage(SucursalFormViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
