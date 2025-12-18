using System.Collections.ObjectModel;
using System.Windows.Input;
using Eurekabank_Maui.Models;
using Eurekabank_Maui.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MapControl = Microsoft.Maui.Controls.Maps.Map;

namespace Eurekabank_Maui.ViewModels
{
    public class MapaSucursalesViewModel : BaseViewModel
    {
        private readonly MapControl _mapa;
        private readonly IEurekabankService _service;
        private Sucursal _sucursalSeleccionada;
        private int _cantidadSucursales;
        private bool _estaCargando;
        private double _miLatitud;
        private double _miLongitud;
        private bool _tengoUbicacion;

        public Sucursal SucursalSeleccionada
        {
            get => _sucursalSeleccionada;
            set => SetProperty(ref _sucursalSeleccionada, value);
        }

        public int CantidadSucursales
        {
            get => _cantidadSucursales;
            set => SetProperty(ref _cantidadSucursales, value);
        }

        public bool EstaCargando
        {
            get => _estaCargando;
            set => SetProperty(ref _estaCargando, value);
        }

        public double MiLatitud
        {
            get => _miLatitud;
            set
            {
                SetProperty(ref _miLatitud, value);
                TengoUbicacion = _miLatitud != 0 || _miLongitud != 0;
            }
        }

        public double MiLongitud
        {
            get => _miLongitud;
            set
            {
                SetProperty(ref _miLongitud, value);
                TengoUbicacion = _miLatitud != 0 || _miLongitud != 0;
            }
        }

        public bool TengoUbicacion
        {
            get => _tengoUbicacion;
            set => SetProperty(ref _tengoUbicacion, value);
        }

        public ICommand CentrarEnMiUbicacionCommand { get; }
        public ICommand RecargarSucursalesCommand { get; }
        public ICommand AbrirNavegacionCommand { get; }

        public MapaSucursalesViewModel(MapControl mapa, IEurekabankService service)
        {
            _mapa = mapa;
            _service = service;

            Title = "Mapa de Sucursales";

            // Inicializar comandos
            CentrarEnMiUbicacionCommand = new Command(async () => await CentrarEnMiUbicacion());
            RecargarSucursalesCommand = new Command(async () => await CargarSucursales());
            AbrirNavegacionCommand = new Command(async () => await AbrirNavegacion());

            // Configurar evento de clic en pins
            ConfigurarEventosPins();
        }

        public async Task InicializarAsync()
        {
            try
            {
                EstaCargando = true;

                // Obtener ubicación del usuario
                await ObtenerUbicacionUsuario();

                // Cargar sucursales y agregarlas al mapa
                await CargarSucursales();

                // Centrar el mapa en la ubicación del usuario
                if (_miLatitud != 0 && _miLongitud != 0)
                {
                    await CentrarEnMiUbicacion();
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al inicializar el mapa: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                EstaCargando = false;
            }
        }

        private async Task ObtenerUbicacionUsuario()
        {
            try
            {
                var location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(30)
                });

                if (location != null)
                {
                    MiLatitud = location.Latitude;
                    MiLongitud = location.Longitude;
                    System.Diagnostics.Debug.WriteLine($"📍 Mi ubicación: Lat={MiLatitud:F6}, Lng={MiLongitud:F6}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error obteniendo ubicación: {ex.Message}");

                // Ubicación por defecto (Quito, Ecuador)
                MiLatitud = -0.1807;
                MiLongitud = -78.4678;
                System.Diagnostics.Debug.WriteLine($"📍 Usando ubicación por defecto: Quito");
            }
        }

        private async Task CargarSucursales()
        {
            try
            {
                EstaCargando = true;
                System.Diagnostics.Debug.WriteLine($"🗺️ MapaSucursalesViewModel - Iniciando carga de sucursales...");

                // Limpiar pins existentes
                _mapa.Pins.Clear();
                System.Diagnostics.Debug.WriteLine($"🗺️ Pins limpiados");

                // Obtener sucursales del servidor SOAP
                System.Diagnostics.Debug.WriteLine($"🗺️ Llamando a ListarSucursalesAsync...");
                var sucursales = await _service.ListarSucursalesAsync();
                System.Diagnostics.Debug.WriteLine($"🗺️ Sucursales recibidas: {sucursales?.Count ?? 0}");

                if (sucursales == null || sucursales.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ No se encontraron sucursales");
                    await Application.Current.MainPage.DisplayAlert(
                        "Información",
                        "No se encontraron sucursales",
                        "OK"
                    );
                    return;
                }

                CantidadSucursales = sucursales.Count;
                System.Diagnostics.Debug.WriteLine($"✅ Total de sucursales: {CantidadSucursales}");

                // Agregar un pin por cada sucursal
                int pinesAgregados = 0;
                int pinesIgnorados = 0;
                foreach (var sucursal in sucursales)
                {
                    System.Diagnostics.Debug.WriteLine($"🗺️ Procesando sucursal: {sucursal.Nombre} - Lat: {sucursal.Latitud}, Lng: {sucursal.Longitud}");

                    if (sucursal.Latitud == 0 || sucursal.Longitud == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Sucursal sin coordenadas válidas, ignorando...");
                        pinesIgnorados++;
                        continue;
                    }

                    var pin = new Pin
                    {
                        Label = sucursal.Nombre,
                        Address = $"{sucursal.Direccion}, {sucursal.Ciudad}",
                        Type = PinType.Place,
                        Location = new Location(sucursal.Latitud, sucursal.Longitud)
                    };

                    // Guardar la sucursal en el pin para acceso posterior
                    pin.MarkerClicked += (s, e) =>
                    {
                        SucursalSeleccionada = sucursal;
                        e.HideInfoWindow = false;
                    };

                    _mapa.Pins.Add(pin);
                    pinesAgregados++;
                    System.Diagnostics.Debug.WriteLine($"✅ Pin agregado para: {sucursal.Nombre}");
                }

                System.Diagnostics.Debug.WriteLine($"✅ Total: {pinesAgregados} pines agregados, {pinesIgnorados} ignorados");
                System.Diagnostics.Debug.WriteLine($"✅ Pins en el mapa: {_mapa.Pins.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en CargarSucursales: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al cargar sucursales: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                EstaCargando = false;
                System.Diagnostics.Debug.WriteLine($"🗺️ CargarSucursales finalizado");
            }
        }

        private async Task CentrarEnMiUbicacion()
        {
            try
            {
                if (MiLatitud == 0 && MiLongitud == 0)
                {
                    await ObtenerUbicacionUsuario();
                }

                if (MiLatitud != 0 || MiLongitud != 0)
                {
                    var location = new Location(MiLatitud, MiLongitud);
                    var distance = Distance.FromKilometers(10); // Radio de 10km

                    _mapa.MoveToRegion(MapSpan.FromCenterAndRadius(location, distance));
                    System.Diagnostics.Debug.WriteLine($"🗺️ Mapa centrado en mi ubicación");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Error centrando mapa: {ex.Message}");
            }
        }

        private async Task AbrirNavegacion()
        {
            if (SucursalSeleccionada == null) return;

            try
            {
                var location = new Location(SucursalSeleccionada.Latitud, SucursalSeleccionada.Longitud);
                var options = new MapLaunchOptions
                {
                    Name = SucursalSeleccionada.Nombre,
                    NavigationMode = NavigationMode.Driving
                };

                await Microsoft.Maui.ApplicationModel.Map.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"No se pudo abrir el navegador: {ex.Message}",
                    "OK"
                );
            }
        }

        private void ConfigurarEventosPins()
        {
            // Los eventos se configuran directamente en cada pin durante la creación
        }
    }
}
