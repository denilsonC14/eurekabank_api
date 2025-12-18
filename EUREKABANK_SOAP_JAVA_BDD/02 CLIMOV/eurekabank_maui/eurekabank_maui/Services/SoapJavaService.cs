using Eurekabank_Maui.Helpers;
using Eurekabank_Maui.Models;
using System.Xml.Linq;

namespace Eurekabank_Maui.Services
{
    public class SoapJavaService : IEurekabankService
    {
        private readonly SoapHelper _soapHelper;
        private readonly ServidorConfig _config;
        private const string SOAP_NAMESPACE = "http://ws.monster.edu.ec/";

        public SoapJavaService(HttpClient httpClient)
        {
            if (httpClient == null) throw new ArgumentNullException(nameof(httpClient));
            _soapHelper = new SoapHelper(httpClient);
            _config = ServidorConfig.ObtenerServidores()
                .First(s => s.Tipo == TipoServidor.SoapJava);
        }

        public ServidorConfig GetServidorInfo() => _config;

        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                
                var envelope = BuildJavaSoapEnvelope("health", "");
                
                System.Diagnostics.Debug.WriteLine($"🔍 SOAP Java - Health check");
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope, cts.Token);
                
                var status = SoapHelper.ExtractValue(response, "status");
                System.Diagnostics.Debug.WriteLine($"✅ SOAP Java Health: {status}");
                
                return !string.IsNullOrEmpty(status);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Health - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                username = username.Trim();
                password = password.Trim();
                
                // Intentar primero SIN el prefijo ws: en los parámetros
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:login>
            <username>{username}</username>
            <password>{password}</password>
        </ws:login>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"🔐 SOAP Java - Login: {username}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope (sin ws: en params):");
                System.Diagnostics.Debug.WriteLine(envelope);
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope);
                
                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());
                
                var result = SoapHelper.ExtractValue(response, "return");
                System.Diagnostics.Debug.WriteLine($"📊 SOAP Java - Login result: {result}");
                
                return result.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Login - Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<Movimiento>> ObtenerMovimientosAsync(string cuenta)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:traerMovimientos>
            <cuenta>{cuenta}</cuenta>
        </ws:traerMovimientos>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"📋 SOAP Java - Movimientos: {cuenta}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope);
                
                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());
                
                var movimientos = ParseMovimientos(response);
                
                System.Diagnostics.Debug.WriteLine($"✅ SOAP Java - Movimientos: {movimientos.Count}");
                
                return movimientos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Movimientos - Error: {ex.Message}");
                return new List<Movimiento>();
            }
        }

        public async Task<bool> RegistrarDepositoAsync(string cuenta, double importe)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:regDeposito>
            <cuenta>{cuenta}</cuenta>
            <importe>{importe.ToString(System.Globalization.CultureInfo.InvariantCulture)}</importe>
        </ws:regDeposito>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"💰 SOAP Java Depósito: {cuenta} - ${importe}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope);
                
                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());
                
                var estado = SoapHelper.ExtractValue(response, "estado");
                System.Diagnostics.Debug.WriteLine($"📊 SOAP Java - Estado depósito: {estado}");
                
                return estado == "1";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Depósito - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarRetiroAsync(string cuenta, double importe)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:regRetiro>
            <cuenta>{cuenta}</cuenta>
            <importe>{importe.ToString(System.Globalization.CultureInfo.InvariantCulture)}</importe>
        </ws:regRetiro>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"🏧 SOAP Java Retiro: {cuenta} - ${importe}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope);
                
                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());
                
                var estado = SoapHelper.ExtractValue(response, "estado");
                System.Diagnostics.Debug.WriteLine($"📊 SOAP Java - Estado retiro: {estado}");
                
                return estado == "1";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Retiro - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegistrarTransferenciaAsync(string cuentaOrigen, string cuentaDestino, double importe)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:regTransferencia>
            <cuentaOrigen>{cuentaOrigen}</cuentaOrigen>
            <cuentaDestino>{cuentaDestino}</cuentaDestino>
            <importe>{importe.ToString(System.Globalization.CultureInfo.InvariantCulture)}</importe>
        </ws:regTransferencia>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"🔄 SOAP Java Transferencia: {cuentaOrigen} → {cuentaDestino} - ${importe}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);
                
                var response = await _soapHelper.CallSoapServiceAsync(_config.Url, "", envelope);
                
                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());
                
                var estado = SoapHelper.ExtractValue(response, "estado");
                System.Diagnostics.Debug.WriteLine($"📊 SOAP Java - Estado transferencia: {estado}");
                
                return estado == "1";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ SOAP Java Transferencia - Error: {ex.Message}");
                return false;
            }
        }

        private string BuildJavaSoapEnvelope(string methodName, string parameters)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""{SOAP_NAMESPACE}"">
    <soap:Header/>
    <soap:Body>
        <ws:{methodName}>
            {parameters}
        </ws:{methodName}>
    </soap:Body>
</soap:Envelope>";
        }

        private List<Movimiento> ParseMovimientos(XDocument doc)
        {
            var movimientos = new List<Movimiento>();

            try
            {
                // Buscar elementos "movimiento" en la respuesta
                var movimientoElements = doc.Descendants()
                    .Where(e => e.Name.LocalName == "movimiento")
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 SOAP Java - Encontrados {movimientoElements.Count} elementos movimiento");

                foreach (var element in movimientoElements)
                {
                    try
                    {
                        var fechaStr = GetElementValue(element, "fecha");
                        DateTime fecha = DateTime.Now;
                        
                        if (!string.IsNullOrEmpty(fechaStr))
                        {
                            if (!DateTime.TryParse(fechaStr, out fecha))
                            {
                                System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo parsear fecha: {fechaStr}");
                            }
                        }

                        var movimiento = new Movimiento
                        {
                            Cuenta = GetElementValue(element, "cuenta"),
                            NroMov = int.Parse(GetElementValue(element, "nromov") ?? "0"),
                            Fecha = fecha,
                            Tipo = GetElementValue(element, "tipo"),
                            Accion = GetElementValue(element, "accion"),
                            Importe = double.Parse(GetElementValue(element, "importe") ?? "0", System.Globalization.CultureInfo.InvariantCulture)
                        };
                        movimientos.Add(movimiento);
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Movimiento parseado: {movimiento.Tipo} - ${movimiento.Importe}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error parseando movimiento: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ParseMovimientos: {ex.Message}");
            }

            return movimientos;
        }

        private string GetElementValue(XElement parent, string elementName)
        {
            return parent.Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
                ?.Value ?? "";
        }

        public async Task<List<Sucursal>> ListarSucursalesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🏢 Listando todas las sucursales...");

                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:listarSucursales/>
    </soap:Body>
</soap:Envelope>";

                var url = _config.Url.Replace("EurekabankWS", "SucursalWS");
                System.Diagnostics.Debug.WriteLine($"📤 URL: {url}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);

                var response = await _soapHelper.CallSoapServiceAsync(url, "", envelope);

                System.Diagnostics.Debug.WriteLine($"📨 Response recibida");
                System.Diagnostics.Debug.WriteLine(response.ToString());

                var sucursales = ParseSucursales(response);
                System.Diagnostics.Debug.WriteLine($"✅ Parseadas {sucursales.Count} sucursales");

                return sucursales;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ListarSucursales: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return new List<Sucursal>();
            }
        }

        public async Task<Sucursal> ObtenerSucursalAsync(string codigo)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:obtenerSucursal>
            <codigo>{codigo}</codigo>
        </ws:obtenerSucursal>
    </soap:Body>
</soap:Envelope>";

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                var sucursales = ParseSucursales(response);
                return sucursales.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ObtenerSucursal: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CrearSucursalAsync(Sucursal sucursal)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:crearSucursal>
            <sucursal>
                <codigo>{sucursal.Codigo}</codigo>
                <nombre>{sucursal.Nombre}</nombre>
                <ciudad>{sucursal.Ciudad}</ciudad>
                <direccion>{sucursal.Direccion}</direccion>
                <contadorCuentas>{sucursal.ContadorCuentas}</contadorCuentas>
                <latitud>{sucursal.Latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</latitud>
                <longitud>{sucursal.Longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</longitud>
                <telefono>{sucursal.Telefono}</telefono>
                <email>{sucursal.Email}</email>
                <estado>{sucursal.Estado}</estado>
            </sucursal>
        </ws:crearSucursal>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"➕ Creando sucursal: {sucursal.Codigo}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());

                var resultado = SoapHelper.ExtractValue(response, "resultado");
                System.Diagnostics.Debug.WriteLine($"📊 Resultado extraído: '{resultado}'");

                // Intentar con diferentes nombres de elementos de retorno
                if (string.IsNullOrEmpty(resultado))
                {
                    resultado = SoapHelper.ExtractValue(response, "return");
                    System.Diagnostics.Debug.WriteLine($"📊 Resultado (return): '{resultado}'");
                }

                bool exito = resultado == "1" || resultado.Equals("true", StringComparison.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"📊 Creación {(exito ? "exitosa" : "fallida")}");

                return exito;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error CrearSucursal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<Sucursal> EncontrarSucursalMasCercanaAsync(double latitud, double longitud)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:encontrarSucursalMasCercana>
            <latitud>{latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</latitud>
            <longitud>{longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</longitud>
        </ws:encontrarSucursalMasCercana>
    </soap:Body>
</soap:Envelope>";

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                var sucursales = ParseSucursales(response);
                return sucursales.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error EncontrarSucursalMasCercana: {ex.Message}");
                return null;
            }
        }

        // Método auxiliar para parsear XML de sucursales
        private List<Sucursal> ParseSucursales(XDocument doc)
        {
            var sucursales = new List<Sucursal>();

            // IMPORTANTE: El servidor Java devuelve elementos <sucursales> (plural)
            var elements = doc.Descendants()
                .Where(e => e.Name.LocalName == "sucursales")
                .ToList();

            System.Diagnostics.Debug.WriteLine($"🔍 Encontrados {elements.Count} elementos <sucursales>");

            foreach (var element in elements)
            {
                try
                {
                    var sucursal = new Sucursal
                    {
                        Codigo = GetElementValue(element, "codigo"),
                        Nombre = GetElementValue(element, "nombre"),
                        Ciudad = GetElementValue(element, "ciudad"),
                        Direccion = GetElementValue(element, "direccion"),
                        ContadorCuentas = int.Parse(GetElementValue(element, "contadorCuentas") ?? "0"),
                        Latitud = double.Parse(GetElementValue(element, "latitud") ?? "0",
                            System.Globalization.CultureInfo.InvariantCulture),
                        Longitud = double.Parse(GetElementValue(element, "longitud") ?? "0",
                            System.Globalization.CultureInfo.InvariantCulture),
                        Telefono = GetElementValue(element, "telefono"),
                        Email = GetElementValue(element, "email"),
                        Estado = GetElementValue(element, "estado")
                    };

                    sucursales.Add(sucursal);
                    System.Diagnostics.Debug.WriteLine($"✅ Parseada: {sucursal.Nombre} - Lat: {sucursal.Latitud}, Lng: {sucursal.Longitud}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Error parseando sucursal: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"✅ Total parseadas: {sucursales.Count} sucursales");
            return sucursales;
        }

        // Implementaciones vacías para otros servicios (por ahora)
        // ============================================
        // IMPLEMENTACIÓN COMPLETA DE MÉTODOS FALTANTES
        // ============================================

        /// <summary>
        /// Actualiza los datos de una sucursal existente
        /// </summary>
        public async Task<bool> ActualizarSucursalAsync(Sucursal sucursal)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:actualizarSucursal>
            <sucursal>
                <codigo>{sucursal.Codigo}</codigo>
                <nombre>{sucursal.Nombre}</nombre>
                <ciudad>{sucursal.Ciudad}</ciudad>
                <direccion>{sucursal.Direccion}</direccion>
                <contadorCuentas>{sucursal.ContadorCuentas}</contadorCuentas>
                <latitud>{sucursal.Latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</latitud>
                <longitud>{sucursal.Longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</longitud>
                <telefono>{sucursal.Telefono}</telefono>
                <email>{sucursal.Email}</email>
                <estado>{sucursal.Estado}</estado>
            </sucursal>
        </ws:actualizarSucursal>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"✏️ Actualizando sucursal: {sucursal.Codigo}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());

                var resultado = SoapHelper.ExtractValue(response, "resultado");
                System.Diagnostics.Debug.WriteLine($"📊 Resultado extraído: '{resultado}'");

                // Intentar con diferentes nombres de elementos de retorno
                if (string.IsNullOrEmpty(resultado))
                {
                    resultado = SoapHelper.ExtractValue(response, "return");
                    System.Diagnostics.Debug.WriteLine($"📊 Resultado (return): '{resultado}'");
                }

                bool exito = resultado == "1" || resultado.Equals("true", StringComparison.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"📊 Actualización {(exito ? "exitosa" : "fallida")}");

                return exito;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ActualizarSucursal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Elimina una sucursal (cambia su estado a INACTIVO)
        /// </summary>
        public async Task<bool> EliminarSucursalAsync(string codigo)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/""
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:eliminarSucursal>
            <codigo>{codigo}</codigo>
        </ws:eliminarSucursal>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"🗑️ Eliminando sucursal: {codigo}");
                System.Diagnostics.Debug.WriteLine($"📤 Envelope:");
                System.Diagnostics.Debug.WriteLine(envelope);

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                System.Diagnostics.Debug.WriteLine($"📨 Response:");
                System.Diagnostics.Debug.WriteLine(response.ToString());

                var resultado = SoapHelper.ExtractValue(response, "resultado");
                System.Diagnostics.Debug.WriteLine($"📊 Resultado extraído: '{resultado}'");

                // Intentar con diferentes nombres de elementos de retorno
                if (string.IsNullOrEmpty(resultado))
                {
                    resultado = SoapHelper.ExtractValue(response, "return");
                    System.Diagnostics.Debug.WriteLine($"📊 Resultado (return): '{resultado}'");
                }

                bool exito = resultado == "1" || resultado.Equals("true", StringComparison.OrdinalIgnoreCase);
                System.Diagnostics.Debug.WriteLine($"📊 Eliminación {(exito ? "exitosa" : "fallida")}");

                return exito;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error EliminarSucursal: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Calcula la distancia entre dos sucursales usando sus códigos
        /// </summary>
        public async Task<double> CalcularDistanciaEntreSucursalesAsync(string codigo1, string codigo2)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:calcularDistanciaEntreSucursales>
            <codigoSucursal1>{codigo1}</codigoSucursal1>
            <codigoSucursal2>{codigo2}</codigoSucursal2>
        </ws:calcularDistanciaEntreSucursales>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"📏 Calculando distancia: {codigo1} → {codigo2}");

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                var distanciaStr = SoapHelper.ExtractValue(response, "distancia");

                if (double.TryParse(distanciaStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double distancia) && distancia >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Distancia calculada: {distancia:F2} km");
                    return distancia;
                }

                System.Diagnostics.Debug.WriteLine($"⚠️ No se pudo calcular la distancia");
                return -1.0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error CalcularDistanciaEntreSucursales: {ex.Message}");
                return -1.0;
            }
        }

        /// <summary>
        /// Obtiene todas las sucursales con sus distancias desde una ubicación específica
        /// </summary>
        public async Task<List<SucursalConDistancia>> ObtenerSucursalesConDistanciasAsync(double latitud, double longitud)
        {
            try
            {
                var envelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" 
               xmlns:ws=""http://ws.monster.edu.ec/"">
    <soap:Body>
        <ws:obtenerSucursalesConDistancias>
            <latitud>{latitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</latitud>
            <longitud>{longitud.ToString("F8", System.Globalization.CultureInfo.InvariantCulture)}</longitud>
        </ws:obtenerSucursalesConDistancias>
    </soap:Body>
</soap:Envelope>";

                System.Diagnostics.Debug.WriteLine($"🎯 Obteniendo sucursales con distancias desde: {latitud:F6}, {longitud:F6}");

                var response = await _soapHelper.CallSoapServiceAsync(
                    _config.Url.Replace("EurekabankWS", "SucursalWS"),
                    "",
                    envelope
                );

                return ParseSucursalesConDistancias(response);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error ObtenerSucursalesConDistancias: {ex.Message}");
                return new List<SucursalConDistancia>();
            }
        }

        /// <summary>
        /// Parsea la respuesta XML que contiene sucursales con distancias
        /// El servidor devuelve strings en formato: codigo|nombre|ciudad|direccion|lat|lng|distancia
        /// </summary>
        private List<SucursalConDistancia> ParseSucursalesConDistancias(XDocument doc)
        {
            var resultado = new List<SucursalConDistancia>();

            try
            {
                // El servidor devuelve elementos "sucursalesConDistancia" con strings separados por |
                var elements = doc.Descendants()
                    .Where(e => e.Name.LocalName == "sucursalesConDistancia")
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"🔍 Encontrados {elements.Count} elementos sucursalesConDistancia");

                foreach (var element in elements)
                {
                    try
                    {
                        var data = element.Value;

                        if (string.IsNullOrWhiteSpace(data))
                            continue;

                        // Formato: codigo|nombre|ciudad|direccion|lat|lng|distancia
                        var partes = data.Split('|');

                        if (partes.Length >= 7)
                        {
                            var sucursalConDistancia = new SucursalConDistancia
                            {
                                Sucursal = new Sucursal
                                {
                                    Codigo = partes[0],
                                    Nombre = partes[1],
                                    Ciudad = partes[2],
                                    Direccion = partes[3],
                                    Latitud = double.Parse(partes[4], System.Globalization.CultureInfo.InvariantCulture),
                                    Longitud = double.Parse(partes[5], System.Globalization.CultureInfo.InvariantCulture),
                                    Estado = "ACTIVO"
                                },
                                DistanciaKm = double.Parse(partes[6], System.Globalization.CultureInfo.InvariantCulture)
                            };

                            resultado.Add(sucursalConDistancia);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Error parseando elemento: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"✅ Parseadas {resultado.Count} sucursales con distancias");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error en ParseSucursalesConDistancias: {ex.Message}");
            }

            return resultado;
        }

    }
}
