using System.Windows.Input;
using Eurekabank_Maui.Models;
using Eurekabank_Maui.Services;

namespace Eurekabank_Maui.ViewModels
{
    [QueryProperty(nameof(SucursalCodigo), nameof(SucursalCodigo))]
    public class SucursalFormViewModel : BaseViewModel
    {
        private readonly IEurekabankService _service;
        private string _sucursalCodigo;
        private bool _esEdicion;
        private string _codigo;
        private string _nombre;
        private string _ciudad;
        private string _direccion;
        private string _telefono;
        private string _email;
        private string _latitud;
        private string _longitud;
        private string _estado;

        public string SucursalCodigo
        {
            get => _sucursalCodigo;
            set
            {
                _sucursalCodigo = value;
                if (!string.IsNullOrEmpty(value))
                {
                    EsEdicion = true;
                    _ = CargarSucursal(value);
                }
            }
        }

        public bool EsEdicion
        {
            get => _esEdicion;
            set
            {
                SetProperty(ref _esEdicion, value);
                OnPropertyChanged(nameof(Titulo));
                OnPropertyChanged(nameof(TextoBotonGuardar));
            }
        }

        public string Titulo => EsEdicion ? "✏️ Editar Sucursal" : "➕ Nueva Sucursal";
        public string TextoBotonGuardar => EsEdicion ? "💾 Actualizar" : "💾 Guardar";

        public string Codigo
        {
            get => _codigo;
            set => SetProperty(ref _codigo, value);
        }

        public string Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }

        public string Ciudad
        {
            get => _ciudad;
            set => SetProperty(ref _ciudad, value);
        }

        public string Direccion
        {
            get => _direccion;
            set => SetProperty(ref _direccion, value);
        }

        public string Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Latitud
        {
            get => _latitud;
            set => SetProperty(ref _latitud, value);
        }

        public string Longitud
        {
            get => _longitud;
            set => SetProperty(ref _longitud, value);
        }

        public string Estado
        {
            get => _estado;
            set => SetProperty(ref _estado, value);
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }
        public ICommand ObtenerUbicacionCommand { get; }

        public SucursalFormViewModel(IEurekabankService service)
        {
            _service = service;
            Title = "Formulario Sucursal";

            // Valores por defecto
            Estado = "ACTIVO";
            Latitud = "0.0";
            Longitud = "0.0";

            GuardarCommand = new Command(async () => await Guardar());
            CancelarCommand = new Command(async () => await Cancelar());
            ObtenerUbicacionCommand = new Command(async () => await ObtenerUbicacion());
        }

        // Método para cargar directamente desde una sucursal (sin llamada al servidor)
        public void CargarDesde(Sucursal sucursal)
        {
            if (sucursal == null) return;

            System.Diagnostics.Debug.WriteLine($"📝 Cargando datos directamente desde sucursal: {sucursal.Nombre}");

            EsEdicion = true;
            Codigo = sucursal.Codigo;
            Nombre = sucursal.Nombre;
            Ciudad = sucursal.Ciudad;
            Direccion = sucursal.Direccion;
            Telefono = sucursal.Telefono;
            Email = sucursal.Email;
            // Usar InvariantCulture para que siempre use punto como separador decimal
            Latitud = sucursal.Latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
            Longitud = sucursal.Longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
            Estado = sucursal.Estado;

            System.Diagnostics.Debug.WriteLine($"✅ Datos cargados directamente: {Nombre}, {Ciudad}");
        }

        private async Task CargarSucursal(string codigo)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine($"📝 Cargando sucursal con código: {codigo}");

                var sucursal = await _service.ObtenerSucursalAsync(codigo);
                System.Diagnostics.Debug.WriteLine($"📝 Sucursal obtenida: {sucursal?.Nombre ?? "NULL"}");

                if (sucursal != null)
                {
                    Codigo = sucursal.Codigo;
                    Nombre = sucursal.Nombre;
                    Ciudad = sucursal.Ciudad;
                    Direccion = sucursal.Direccion;
                    Telefono = sucursal.Telefono;
                    Email = sucursal.Email;
                    // Usar InvariantCulture para que siempre use punto como separador decimal
                    Latitud = sucursal.Latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                    Longitud = sucursal.Longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                    Estado = sucursal.Estado;

                    System.Diagnostics.Debug.WriteLine($"✅ Datos cargados: {Nombre}, {Ciudad}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ La sucursal retornó NULL");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error al cargar sucursal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"No se pudo cargar la sucursal: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Guardar()
        {
            if (IsBusy) return;

            // Validaciones
            if (string.IsNullOrWhiteSpace(Codigo) ||
                string.IsNullOrWhiteSpace(Nombre) ||
                string.IsNullOrWhiteSpace(Ciudad) ||
                string.IsNullOrWhiteSpace(Direccion) ||
                string.IsNullOrWhiteSpace(Telefono) ||
                string.IsNullOrWhiteSpace(Email))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Campos requeridos",
                    "Por favor complete todos los campos obligatorios (*)",
                    "OK"
                );
                return;
            }

            // Normalizar coordenadas: reemplazar coma por punto para aceptar ambos formatos
            string latitudNormalizada = Latitud?.Replace(",", ".") ?? "0";
            string longitudNormalizada = Longitud?.Replace(",", ".") ?? "0";

            System.Diagnostics.Debug.WriteLine($"📍 Latitud original: '{Latitud}' → normalizada: '{latitudNormalizada}'");
            System.Diagnostics.Debug.WriteLine($"📍 Longitud original: '{Longitud}' → normalizada: '{longitudNormalizada}'");

            // Validar coordenadas con cultura invariante
            if (!double.TryParse(latitudNormalizada, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(longitudNormalizada, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Coordenadas inválidas",
                    $"Las coordenadas deben ser valores numéricos válidos.\n\nPuedes usar punto (.) o coma (,) como separador decimal.\nEjemplo: -0.178881 o -0,178881",
                    "OK"
                );
                return;
            }

            // Validar rangos de coordenadas
            if (lat < -90 || lat > 90)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Latitud inválida",
                    $"La latitud debe estar entre -90 y 90.\nValor ingresado: {lat}",
                    "OK"
                );
                return;
            }

            if (lng < -180 || lng > 180)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Longitud inválida",
                    $"La longitud debe estar entre -180 y 180.\nValor ingresado: {lng}",
                    "OK"
                );
                return;
            }

            System.Diagnostics.Debug.WriteLine($"📍 Coordenadas validadas: Lat={lat:F8}, Lng={lng:F8}");

            try
            {
                IsBusy = true;

                var sucursal = new Sucursal
                {
                    Codigo = Codigo.Trim(),
                    Nombre = Nombre.Trim(),
                    Ciudad = Ciudad.Trim(),
                    Direccion = Direccion.Trim(),
                    Telefono = Telefono.Trim(),
                    Email = Email.Trim(),
                    Latitud = lat,
                    Longitud = lng,
                    Estado = Estado,
                    ContadorCuentas = 0
                };

                bool exito;
                string mensaje;

                if (EsEdicion)
                {
                    exito = await _service.ActualizarSucursalAsync(sucursal);
                    mensaje = exito ? "Sucursal actualizada exitosamente" : "No se pudo actualizar la sucursal";
                }
                else
                {
                    exito = await _service.CrearSucursalAsync(sucursal);
                    mensaje = exito ? "Sucursal creada exitosamente" : "No se pudo crear la sucursal";
                }

                if (exito)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Éxito",
                        mensaje,
                        "OK"
                    );
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Error",
                        mensaje,
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    $"Error al guardar: {ex.Message}",
                    "OK"
                );
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task Cancelar()
        {
            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Cancelar",
                "¿Está seguro que desea cancelar? Los cambios no se guardarán",
                "Sí",
                "No"
            );

            if (confirmar)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
        }

        private async Task ObtenerUbicacion()
        {
            try
            {
                IsBusy = true;

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
                    // Usar InvariantCulture para que siempre use punto como separador decimal
                    Latitud = location.Latitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
                    Longitud = location.Longitude.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);

                    System.Diagnostics.Debug.WriteLine($"📍 Ubicación obtenida: Lat={Latitud}, Lng={Longitud}");

                    await Application.Current.MainPage.DisplayAlert(
                        "Ubicación obtenida",
                        $"Lat: {location.Latitude:F6}\nLng: {location.Longitude:F6}",
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
            finally
            {
                IsBusy = false;
            }
        }
    }
}
