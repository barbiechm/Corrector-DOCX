namespace COMPARARNOTAS.Models
{
    public class PreguntaExamen
    {
        public string Id { get; set; } = string.Empty;                    // [1], [2], etc.
        public string TextoCompleto { get; set; } = string.Empty;         // Respuesta esperada/recibida
        public double Puntos { get; set; } = 1.0;                         // Valor de la pregunta

        // ✅ AGREGAR ESTAS PROPIEDADES
        public string PalabraObjetivo { get; set; } = string.Empty;       // La palabra a analizar
        public double PuntosTotales { get; set; } = 1.0;                  // Total de puntos posibles

        // Componentes lingüísticos
        public List<string> ComponentesLinguisticos { get; set; } = new();

        // Respuesta del estudiante
        public string RespuestaEstudiante { get; set; } = string.Empty;
        public List<string> ComponentesRespuesta { get; set; } = new();

        // Evaluación
        public bool Respondida { get; set; } = false;
        public double PuntosGanados { get; set; } = 0;
        public string Estado { get; set; } = "NO RESPONDIÓ";
        public double PorcentajeAcierto { get; set; } = 0;
        public string Feedback { get; set; } = string.Empty;
    }
}