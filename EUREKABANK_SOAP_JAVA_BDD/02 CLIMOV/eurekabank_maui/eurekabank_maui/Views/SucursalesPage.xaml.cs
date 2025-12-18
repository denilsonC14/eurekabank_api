using Eurekabank_Maui.ViewModels;
using Eurekabank_Maui.Services;
using Eurekabank_Maui.Models;

namespace Eurekabank_Maui.Views
{
    public partial class SucursalesPage : ContentPage
    {
        private readonly SucursalesViewModel _viewModel;

        public SucursalesPage(IEurekabankService service)
        {
            InitializeComponent();
            _viewModel = new SucursalesViewModel(service);
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Recargar la lista cada vez que se vuelve a esta página
            // (por ejemplo, después de crear o editar una sucursal)
            System.Diagnostics.Debug.WriteLine("🔄 SucursalesPage.OnAppearing - Recargando lista");
            _viewModel.CargarSucursalesCommand.Execute(null);
        }

        // Evento cuando selecciona una sucursal de la lista TODAS
        private async void OnSucursalSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Sucursal sucursal)
            {
                // Deseleccionar para permitir volver a tocar la misma
                ((CollectionView)sender).SelectedItem = null;

                await MostrarDetalleSucursal(sucursal);
            }
        }

        // Evento cuando selecciona una sucursal de la lista CERCANAS
        private async void OnSucursalCercanaSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is SucursalConDistancia sucursalConDistancia)
            {
                // Deseleccionar para permitir volver a tocar la misma
                ((CollectionView)sender).SelectedItem = null;

                await MostrarDetalleSucursal(sucursalConDistancia.Sucursal, sucursalConDistancia.DistanciaKm);
            }
        }

        // Método para mostrar los detalles de una sucursal
        private async Task MostrarDetalleSucursal(Sucursal sucursal, double? distancia = null)
        {
            var distanciaTexto = distancia.HasValue
                ? $"📏 Distancia: {distancia.Value:F2} km\n⏱️ Tiempo estimado: ~{(int)(distancia.Value / 0.8)} min\n\n"
                : "";

            var mensaje = $@"{distanciaTexto}📋 Código: {sucursal.Codigo}
🏢 Nombre: {sucursal.Nombre}
🌆 Ciudad: {sucursal.Ciudad}
📍 Dirección: {sucursal.Direccion}
📞 Teléfono: {sucursal.Telefono}
✉️ Email: {sucursal.Email}
🗺️ Coordenadas: {sucursal.Coordenadas}
📊 Estado: {sucursal.Estado}";

            var abrir = await DisplayAlert(
                sucursal.Nombre,
                mensaje,
                "🗺️ Abrir en Mapa",
                "Cerrar"
            );

            if (abrir)
            {
                await AbrirEnMapa(sucursal);
            }
        }

        // Método para abrir Google Maps
        private async Task AbrirEnMapa(Sucursal sucursal)
        {
            try
            {
                var location = new Location(sucursal.Latitud, sucursal.Longitud);
                var options = new MapLaunchOptions
                {
                    Name = sucursal.Nombre,
                    NavigationMode = NavigationMode.Driving
                };

                await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Error",
                    $"No se pudo abrir el mapa: {ex.Message}\n\nIntenta instalar Google Maps.",
                    "OK"
                );
            }
        }

        // Método para abrir la página del mapa embebido
        private async void OnVerMapaClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MapaSucursalesPage(_viewModel.Service));
        }
    }
}