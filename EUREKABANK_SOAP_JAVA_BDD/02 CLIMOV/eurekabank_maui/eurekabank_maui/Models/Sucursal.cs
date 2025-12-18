using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eurekabank_Maui.Models
{
    public class Sucursal
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Ciudad { get; set; }
        public string Direccion { get; set; }
        public int ContadorCuentas { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Estado { get; set; }

        // Propiedades calculadas para la UI
        public string Ubicacion => $"{Ciudad} - {Direccion}";
        public string Coordenadas => $"{Latitud:F6}, {Longitud:F6}";
        public Color EstadoColor => Estado == "ACTIVO" ? Colors.Green : Colors.Red;
    }

    public class SucursalConDistancia
    {
        public Sucursal Sucursal { get; set; }
        public double DistanciaKm { get; set; }
        public string DistanciaTexto => $"{DistanciaKm:F2} km";
    }
}