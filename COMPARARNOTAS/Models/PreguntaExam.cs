namespace COMPARARNOTAS.Models
{

    public class PreguntaExamen
    {
        public string Id { get; set; } = string.Empty;      // El número (ej: "3")
        public string Texto { get; set; } = string.Empty;   // El texto completo
        public double Puntos { get; set; } = 0;             // Valor de la pregunta
        
        // Datos Gramaticales
        public string Gramatical { get; set; } = string.Empty;
        public string Clasificacion { get; set; } = string.Empty;

        // Banderas de control
        public bool Respondida { get; set; } = false;
        
        // Para el reporte de Excel
        public string Estado { get; set; } = "NO RESPONDIÓ";
        public double PuntosGanados { get; set; } = 0;
        public string Correccion { get; set; } = string.Empty;
    }
}
    

