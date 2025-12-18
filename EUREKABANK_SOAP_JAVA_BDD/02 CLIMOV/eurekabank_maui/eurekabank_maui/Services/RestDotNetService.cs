using Eurekabank_Maui.Models;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Eurekabank_Maui.Services
{
    public class RestDotNetService : IEurekabankService
    {
        private readonly HttpClient _httpClient;
        private readonly ServidorConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl;

        public RestDotNetService(HttpClient httpClient)
        {
            _config = ServidorConfig.ObtenerServidores()
                .First(s => s.Tipo == TipoServidor.RestDotNet);
            
            // Asegurar que la URL base termine con /
            _baseUrl = _config.Url.TrimEnd('/') + "/";
            
            // Crear nuevo HttpClient con BaseAddress configurado
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            
            // Bypass SSL validation for development
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public ServidorConfig GetServidorInfo() => _config;

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                
                System.Diagnostics.Debug.WriteLine($"🔍 REST .NET - Health check: {_baseUrl}Health");
                
                var response = await _httpClient.GetAsync("Health", cts.Token);
                
                System.Diagnostics.Debug.WriteLine($"✅ REST .NET Health: {response.StatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Health - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                username = username.Trim();
                password = password.Trim();
                
                var loginRequest = new { username, password };
                var content = new StringContent(
                    JsonSerializer.Serialize(loginRequest),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"🔐 REST .NET - Login: {username}");
                
                var response = await _httpClient.PostAsync("Auth/login", content);

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET Login: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Authenticated: {result?.Data?.Authenticated}");
                    return result?.Data?.Authenticated ?? false;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Login - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Movimiento>> ObtenerMovimientosAsync(string cuenta)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📋 REST .NET - Movimientos: {cuenta}");
                
                var response = await _httpClient.GetAsync($"Movimientos/cuenta/{cuenta}");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET Movimientos: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<Movimiento>>>();
                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Movimientos: {result?.Data?.Count ?? 0}");
                    return result?.Data ?? new List<Movimiento>();
                }
                return new List<Movimiento>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Movimientos - Error: {ex.Message}");
                return new List<Movimiento>();
            }
        }

        public async Task<bool> RegistrarDepositoAsync(string cuenta, double importe)
        {
            try
            {
                var request = new { cuenta, importe };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"💵 REST .NET - Deposito: {cuenta}, ${importe}");
                
                var response = await _httpClient.PostAsync("Movimientos/deposito", content);
                
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET Deposito: {response.StatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Deposito - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarRetiroAsync(string cuenta, double importe)
        {
            try
            {
                var request = new { cuenta, importe };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"💸 REST .NET - Retiro: {cuenta}, ${importe}");
                
                var response = await _httpClient.PostAsync("Movimientos/retiro", content);
                
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET Retiro: {response.StatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Retiro - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarTransferenciaAsync(string cuentaOrigen, string cuentaDestino, double importe)
        {
            try
            {
                var request = new { cuentaOrigen, cuentaDestino, importe };
                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"🔄 REST .NET - Transferencia: {cuentaOrigen} → {cuentaDestino}, ${importe}");
                
                var response = await _httpClient.PostAsync("Movimientos/transferencia", content);
                
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET Transferencia: {response.StatusCode}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET Transferencia - Error: {ex.Message}");
                return false;
            }
        }

        // Clases de respuesta para deserialización
        private class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public T Data { get; set; }
        }

        private class LoginResponse
        {
            public bool Authenticated { get; set; }
            public string Username { get; set; }
        }
        #region Métodos de Sucursales

        /// <summary>
        /// Listar todas las sucursales
        /// </summary>
        public async Task<List<Sucursal>> ListarSucursalesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📋 REST .NET - Listando sucursales");

                var response = await _httpClient.GetAsync("api/sucursales");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET ListarSucursales: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📨 Response content: {content}");

                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<Sucursal>>>(_jsonOptions);

                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursales encontradas: {result?.Data?.Count ?? 0}");

                    return result?.Data ?? new List<Sucursal>();
                }

                System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al listar sucursales: {response.StatusCode}");
                return new List<Sucursal>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET ListarSucursales - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return new List<Sucursal>();
            }
        }

        /// <summary>
        /// Obtener sucursal por código
        /// </summary>
        public async Task<Sucursal> ObtenerSucursalAsync(string codigo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 REST .NET - Obteniendo sucursal: {codigo}");

                var response = await _httpClient.GetAsync($"api/sucursales/{codigo}");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET ObtenerSucursal: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📨 Response content: {content}");

                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<Sucursal>>(_jsonOptions);

                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursal encontrada: {result?.Data?.Nombre}");

                    return result?.Data;
                }

                System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Sucursal no encontrada: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET ObtenerSucursal - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Crear nueva sucursal
        /// </summary>
        public async Task<bool> CrearSucursalAsync(Sucursal sucursal)
        {
            try
            {
                var request = new
                {
                    codigo = sucursal.Codigo,
                    nombre = sucursal.Nombre,
                    ciudad = sucursal.Ciudad,
                    direccion = sucursal.Direccion,
                    contadorCuentas = sucursal.ContadorCuentas,
                    latitud = sucursal.Latitud,
                    longitud = sucursal.Longitud,
                    telefono = sucursal.Telefono,
                    email = sucursal.Email,
                    estado = sucursal.Estado
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"➕ REST .NET - Creando sucursal: {sucursal.Codigo}");
                System.Diagnostics.Debug.WriteLine($"📤 Request: {JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PostAsync("api/sucursales", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET CrearSucursal: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📨 Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursal creada exitosamente");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al crear sucursal: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET CrearSucursal - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Actualizar sucursal existente
        /// </summary>
        public async Task<bool> ActualizarSucursalAsync(Sucursal sucursal)
        {
            try
            {
                var request = new
                {
                    codigo = sucursal.Codigo,
                    nombre = sucursal.Nombre,
                    ciudad = sucursal.Ciudad,
                    direccion = sucursal.Direccion,
                    contadorCuentas = sucursal.ContadorCuentas,
                    latitud = sucursal.Latitud,
                    longitud = sucursal.Longitud,
                    telefono = sucursal.Telefono,
                    email = sucursal.Email,
                    estado = sucursal.Estado
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                System.Diagnostics.Debug.WriteLine($"✏️ REST .NET - Actualizando sucursal: {sucursal.Codigo}");
                System.Diagnostics.Debug.WriteLine($"📤 Request: {JsonSerializer.Serialize(request)}");

                var response = await _httpClient.PutAsync($"api/sucursales/{sucursal.Codigo}", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET ActualizarSucursal: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📨 Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursal actualizada exitosamente");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al actualizar sucursal: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET ActualizarSucursal - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Eliminar sucursal (soft delete - cambiar estado a INACTIVO)
        /// </summary>
        public async Task<bool> EliminarSucursalAsync(string codigo)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🗑️ REST .NET - Eliminando sucursal: {codigo}");

                var response = await _httpClient.DeleteAsync($"api/sucursales/{codigo}");

                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"📨 REST .NET EliminarSucursal: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"📨 Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursal eliminada exitosamente");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al eliminar sucursal: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET EliminarSucursal - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Calcular distancia entre dos sucursales
        /// </summary>
        public async Task<double> CalcularDistanciaEntreSucursalesAsync(string codigo1, string codigo2)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📏 REST .NET - Calculando distancia: {codigo1} → {codigo2}");

                var response = await _httpClient.GetAsync($"api/sucursales/distancia/{codigo1}/{codigo2}");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET CalcularDistancia: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<DistanciaResponse>>(_jsonOptions);

                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Distancia calculada: {result?.Data?.DistanciaKm ?? 0} km");

                    return result?.Data?.DistanciaKm ?? 0.0;
                }

                System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al calcular distancia: {response.StatusCode}");
                return -1.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET CalcularDistancia - Error: {ex.Message}");
                return -1.0;
            }
        }

        /// <summary>
        /// Encontrar sucursal más cercana
        /// </summary>
        public async Task<Sucursal> EncontrarSucursalMasCercanaAsync(double latitud, double longitud)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎯 REST .NET - Buscando sucursal más cercana: {latitud:F6}, {longitud:F6}");

                var response = await _httpClient.GetAsync($"api/sucursales/mas-cercana?latitud={latitud}&longitud={longitud}");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET SucursalMasCercana: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<SucursalConDistanciaResponse>>(_jsonOptions);

                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursal más cercana: {result?.Data?.Sucursal?.Nombre} a {result?.Data?.DistanciaKm} km");

                    return result?.Data?.Sucursal;
                }

                System.Diagnostics.Debug.WriteLine($"❌ REST .NET - No se encontró sucursal cercana: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET SucursalMasCercana - Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtener sucursales con distancias desde una posición
        /// </summary>
        public async Task<List<SucursalConDistancia>> ObtenerSucursalesConDistanciasAsync(double latitud, double longitud)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🎯 REST .NET - Obteniendo sucursales con distancias: {latitud:F6}, {longitud:F6}");

                var response = await _httpClient.GetAsync($"api/sucursales/con-distancias?latitud={latitud}&longitud={longitud}&limite=50");

                System.Diagnostics.Debug.WriteLine($"📨 REST .NET SucursalesConDistancias: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"📨 Response content: {content}");

                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<SucursalConDistanciaResponse>>>(_jsonOptions);

                    // Convertir de SucursalConDistanciaResponse a SucursalConDistancia
                    var sucursalesConDistancia = result?.Data?.Select(s => new SucursalConDistancia
                    {
                        Sucursal = s.Sucursal,
                        DistanciaKm = s.DistanciaKm
                    }).ToList() ?? new List<SucursalConDistancia>();

                    System.Diagnostics.Debug.WriteLine($"✅ REST .NET - Sucursales con distancias: {sucursalesConDistancia.Count}");

                    return sucursalesConDistancia;
                }

                System.Diagnostics.Debug.WriteLine($"❌ REST .NET - Error al obtener sucursales con distancias: {response.StatusCode}");
                return new List<SucursalConDistancia>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ REST .NET SucursalesConDistancias - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return new List<SucursalConDistancia>();
            }
        }

        #endregion

        #region Clases de Response para Sucursales

        private class DistanciaResponse
        {
            public double DistanciaKm { get; set; }
            public string SucursalOrigen { get; set; }
            public string SucursalDestino { get; set; }
            public double? TiempoEstimadoMinutos { get; set; }
        }

        private class SucursalConDistanciaResponse
        {
            public Sucursal Sucursal { get; set; }
            public double DistanciaKm { get; set; }
            public double? TiempoEstimadoMinutos { get; set; }
        }

        #endregion
    }
}
