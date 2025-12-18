using System.Collections.ObjectModel;
using System.Windows.Input;
using Eurekabank_Maui.Models;
using Eurekabank_Maui.Services;

namespace Eurekabank_Maui.ViewModels
{
    public class SucursalesViewModel : BaseViewModel
    {
        private readonly IEurekabankService _service;
        private ObservableCollection<Sucursal> _sucursales;

        // Exponer el servicio para que pueda ser usado por otras páginas
        public IEurekabankService Service => _service;
        private ObservableCollection<SucursalConDistancia> _sucursalesCercanas;
        private Sucursal _sucursalSeleccionada;
        private SucursalConDistancia _sucursalConDistanciaSeleccionada;
        private bool _mostrarMapa;
        private double _miLatitud;
        private double _miLongitud;
        private string _textoBotonVista;

        public ObservableCollection<Sucursal> Sucursales
        {
            get => _sucursales;
            set => SetProperty(ref _sucursales, value);
        }

        public ObservableCollection<SucursalConDistancia> SucursalesCercanas
        {
            get => _sucursalesCercanas;
            set => SetProperty(ref _sucursalesCercanas, value);
        }

        public Sucursal SucursalSeleccionada
        {
            get => _sucursalSeleccionada;
            set
            {
                SetProperty(ref _sucursalSeleccionada, value);
                if (value != null)
                {
                    AbrirDetalleSucursal();
                }
            }
        }

        public SucursalConDistancia SucursalConDistanciaSeleccionada
        {
            get => _sucursalConDistanciaSeleccionada;
            set
            {
                SetProperty(ref _sucursalConDistanciaSeleccionada, value);
                if (value != null)
                {
                    AbrirDetalleSucursalConDistancia();
                }
            }
        }

        public bool MostrarMapa
        {
            get => _mostrarMapa;
            set
            {
                SetProperty(ref _mostrarMapa, value);
                TextoBotonVista = value ? "📋 Todas" : "🎯 Cercanas";
            }
        }

        public string TextoBotonVista
        {
            get => _textoBotonVista;
            set => SetProperty(ref _textoBotonVista, value);
        }

        public double MiLatitud
        {
            get => _miLatitud;
            set => SetProperty(ref _miLatitud, value);
        }

        public double MiLongitud
        {
            get => _miLongitud;
            set => SetProperty(ref _miLongitud, value);
        }

        public ICommand CargarSucursalesCommand { get; }
        public ICommand BuscarCercanasCommand { get; }
        public ICommand ObtenerUbicacionCommand { get; }
        public ICommand AbrirMapaCommand { get; }
        public ICommand AlternarVistaCommand { get; }
        public ICommand NuevaSucursalCommand { get; }
        public ICommand EditarSucursalCommand { get; }
        public ICommand EliminarSucursalCommand { get; }

        public SucursalesViewModel(IEurekabankService service)
        {
            _service = service;
            Title = "🏢 Sucursales";

            Sucursales = new ObservableCollection<Sucursal>();
            SucursalesCercanas = new ObservableCollection<SucursalConDistancia>();

            CargarSucursalesCommand = new Command(async () => await CargarSucursales());
            BuscarCercanasCommand = new Command(async () => await BuscarSucursalesCercanas());
            ObtenerUbicacionCommand = new Command(async () => await ObtenerMiUbicacion());
            AbrirMapaCommand = new Command<Sucursal>(async (s) => await AbrirEnMapa(s));
            AlternarVistaCommand = new Command(AlternarVista);
            NuevaSucursalCommand = new Command(async () => await NuevaSucursal());
            EditarSucursalCommand = new Command<Sucursal>(async (s) => await EditarSucursal(s));
            EliminarSucursalCommand = new Command<Sucursal>(async (s) => await EliminarSucursal(s));

            // Inicializar texto del botón
            TextoBotonVista = "🎯 Cercanas";

            // Cargar sucursales al iniciar
            Task.Run(async () => await CargarSucursales());
        }

        private async Task CargarSucursales()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var sucursales = await _service.ListarSucursalesAsync();

                Sucursales.Clear();
                foreach (var sucursal in sucursales)
                {
                    Sucursales.Add(sucursal);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"No se pudieron cargar las sucursales: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task BuscarSucursalesCercanas()
        {
            if (IsBusy) return;

            if (MiLatitud == 0 && MiLongitud == 0)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Ubicación requerida",
                    "Primero obtenga su ubicación actual",
                    "OK"
                );
                return;
            }

            try
            {
                IsBusy = true;
                var sucursales = await _service.ObtenerSucursalesConDistanciasAsync(MiLatitud, MiLongitud);

                SucursalesCercanas.Clear();
                foreach (var sucursal in sucursales.OrderBy(s => s.DistanciaKm))
                {
                    SucursalesCercanas.Add(sucursal);
                }

                MostrarMapa = true;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al buscar sucursales cercanas: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void AlternarVista()
        {
            if (!MostrarMapa)
            {
                // Cambiar a vista de cercanas
                await BuscarSucursalesCercanas();
            }
            else
            {
                // Volver a vista de todas
                MostrarMapa = false;
            }
        }

        private async Task ObtenerMiUbicacion()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest
                    {
                        DesiredAccuracy = GeolocationAccuracy.Medium,
                        Timeout = TimeSpan.FromSeconds(30)
                    });
                }

                if (location != null)
                {
                    MiLatitud = location.Latitude;
                    MiLongitud = location.Longitude;

                    await Application.Current.MainPage.DisplayAlert(
                        "Ubicación obtenida",
                        $"Lat: {MiLatitud:F6}, Lng: {MiLongitud:F6}",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"No se pudo obtener la ubicación: {ex.Message}",
                    "OK"
                );
            }
        }

        private async Task AbrirEnMapa(Sucursal sucursal)
        {
            if (sucursal == null) return;

            try
            {
                var location = new Location(sucursal.Latitud, sucursal.Longitud);
                var options = new MapLaunchOptions
                {
                    Name = sucursal.Nombre,
                    NavigationMode = NavigationMode.Driving // 🚗 Activa navegación con ruta
                };

                await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"No se pudo abrir el mapa: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void AbrirDetalleSucursal()
        {
            if (SucursalSeleccionada == null) return;

            var mensaje = $@"
📋 Código: {SucursalSeleccionada.Codigo}
🏢 Nombre: {SucursalSeleccionada.Nombre}
🌆 Ciudad: {SucursalSeleccionada.Ciudad}
📍 Dirección: {SucursalSeleccionada.Direccion}
📞 Teléfono: {SucursalSeleccionada.Telefono}
✉️ Email: {SucursalSeleccionada.Email}
🗺️ Coordenadas: {SucursalSeleccionada.Coordenadas}
📊 Estado: {SucursalSeleccionada.Estado}";

            var abrir = await Application.Current.MainPage.DisplayAlert(
                SucursalSeleccionada.Nombre,
                mensaje,
                "Abrir en Mapa",
                "Cerrar"
            );

            if (abrir)
            {
                await AbrirEnMapa(SucursalSeleccionada);
            }

            // Limpiar selección
            SucursalSeleccionada = null;
        }

        private async void AbrirDetalleSucursalConDistancia()
        {
            if (SucursalConDistanciaSeleccionada == null) return;

            var sucursal = SucursalConDistanciaSeleccionada.Sucursal;
            var distancia = SucursalConDistanciaSeleccionada.DistanciaKm;
            var tiempoEstimado = (int)(distancia / 0.75); // ~45 km/h promedio = 0.75 km/min

            var mensaje = $@"
📋 Código: {sucursal.Codigo}
🏢 Nombre: {sucursal.Nombre}
🌆 Ciudad: {sucursal.Ciudad}
📍 Dirección: {sucursal.Direccion}
📞 Teléfono: {sucursal.Telefono}
✉️ Email: {sucursal.Email}
🗺️ Coordenadas: {sucursal.Coordenadas}
📊 Estado: {sucursal.Estado}

📏 Distancia: {distancia:F2} km
⏱️ Tiempo estimado: ~{tiempoEstimado} min";

            var abrir = await Application.Current.MainPage.DisplayAlert(
                sucursal.Nombre,
                mensaje,
                "🗺️ Abrir en Mapa",
                "Cerrar"
            );

            if (abrir)
            {
                await AbrirEnMapa(sucursal);
            }

            // Limpiar selección
            SucursalConDistanciaSeleccionada = null;
        }

        private async Task NuevaSucursal()
        {
            var formViewModel = new SucursalFormViewModel(_service);
            var formPage = new Views.SucursalFormPage(formViewModel);
            await Application.Current.MainPage.Navigation.PushAsync(formPage);
        }

        private async Task EditarSucursal(Sucursal sucursal)
        {
            if (sucursal == null) return;

            System.Diagnostics.Debug.WriteLine($"✏️ Editando sucursal: {sucursal.Nombre}");

            var formViewModel = new SucursalFormViewModel(_service);
            formViewModel.CargarDesde(sucursal); // Cargar directamente desde la sucursal

            var formPage = new Views.SucursalFormPage(formViewModel);
            await Application.Current.MainPage.Navigation.PushAsync(formPage);
        }

        private async Task EliminarSucursal(Sucursal sucursal)
        {
            if (sucursal == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Confirmar eliminación",
                $"¿Está seguro que desea eliminar la sucursal '{sucursal.Nombre}'?\n\nEsta acción cambiará su estado a INACTIVO.",
                "Sí, eliminar",
                "Cancelar"
            );

            if (!confirmar) return;

            try
            {
                IsBusy = true;
                bool exito = await _service.EliminarSucursalAsync(sucursal.Codigo);

                if (exito)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Éxito",
                        "Sucursal eliminada exitosamente",
                        "OK"
                    );

                    // Recargar lista
                    await CargarSucursales();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        "No se pudo eliminar la sucursal",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al eliminar: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}