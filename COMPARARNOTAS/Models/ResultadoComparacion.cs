namespace COMPARARNOTAS.Models
{
    public class ResultadoComparacion
    {
        public List<PreguntaExamen> Preguntas { get; set; } = new();
        public double PuntajeTotal { get; set; }
        public double PuntajeObtenido { get; set; }
        public double PorcentajeGeneral { get; set; }

        // Contadores
        public int Correctas { get; set; }
        public int Parciales { get; set; }
        public int Incorrectas { get; set; }
        public int NoRespondidas { get; set; }

        public void CalcularEstadisticas()
        {
            // CAMBIO 1: Usamos 'PuntosTotales' que es como se llama en el nuevo modelo
            PuntajeTotal = Preguntas.Sum(p => p.PuntosTotales);

            PuntajeObtenido = Preguntas.Sum(p => p.PuntosGanados);

            // Evitar división por cero
            PorcentajeGeneral = PuntajeTotal > 0 ? (PuntajeObtenido / PuntajeTotal) * 100 : 0;

            // CAMBIO 2: Aseguramos que los textos coincidan con ComparadorLinguistico
            Correctas = Preguntas.Count(p => p.Estado == "CORRECTO");
            Parciales = Preguntas.Count(p => p.Estado == "PARCIAL");
            Incorrectas = Preguntas.Count(p => p.Estado == "INCORRECTO");

            // Ajuste: En el comparador usamos "SIN_RESPONDER"
            NoRespondidas = Preguntas.Count(p => p.Estado == "SIN_RESPONDER" || p.Estado == "NO RESPONDIÓ");
        }
    }
}