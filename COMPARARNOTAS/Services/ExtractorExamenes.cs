using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using COMPARARNOTAS.Models;
using System.Text;

namespace COMPARARNOTAS.Services
{
    public class ExtractorExamenes
    {
        public static List<PreguntaExamen> ExtraerPreguntas(string rutaArchivo)
        {
            var preguntas = new Dictionary<string, PreguntaExamen>();
            int contadorPreguntas = 0;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(rutaArchivo, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return new List<PreguntaExamen>();

                var parrafos = body.Descendants<Paragraph>().ToList();
                PreguntaExamen preguntaActual = null;

                foreach (var parrafo in parrafos)
                {
                    var texto = parrafo.InnerText?.Trim();
                    if (string.IsNullOrWhiteSpace(texto)) continue;

                    bool esNuevaPregunta = false;

                    // ---------------------------------------------------------
                    // DETECCIÓN INTELIGENTE DE ID (Soporta todos los formatos sucios)
                    // ---------------------------------------------------------

                    // 1. Numeración Automática de Word (Metadatos internos)
                    var numPr = parrafo.ParagraphProperties?.NumberingProperties;

                    // 2. Regex para listas manuales tipo "1. Hola" o "1) Hola"
                    //    Explicación: ^(\d+) -> Empieza con numero
                    //                 [\.\)\-] -> Sigue punto, parentesis o guion
                    //                 \s* -> Cualquier cantidad de espacios (o ninguno)
                    //                 (.+)     -> El resto del texto
                    var matchLista = Regex.Match(texto, @"^\s*(\d+)[\.\)\-]\s*(.+)");

                    // 3. Regex para corchetes tipo "[1]Hola", "[1]. Hola", "[1] Hola"
                    //    Explicación: ^\s*\[(\d+)\] -> Empieza con [Numero]
                    //                 [\.\s]* -> Sigue OPCIONALMENTE un punto o espacios
                    //                 (.+)          -> El resto del texto
                    var matchCorchete = Regex.Match(texto, @"^\s*\[(\d+)\][\.\s]*(.+)");

                    if (numPr != null || matchLista.Success || matchCorchete.Success)
                    {
                        esNuevaPregunta = true;

                        string idNumerico = "";
                        string contenidoPregunta = "";

                        // Prioridad 1: Automático
                        if (numPr != null)
                        {
                            contadorPreguntas++;
                            idNumerico = contadorPreguntas.ToString();
                            contenidoPregunta = texto;
                        }
                        // Prioridad 2: Corchetes (Más específico, probamos este antes que la lista simple)
                        else if (matchCorchete.Success)
                        {
                            idNumerico = matchCorchete.Groups[1].Value;
                            contenidoPregunta = matchCorchete.Groups[2].Value.Trim();
                        }
                        // Prioridad 3: Lista simple (1.)
                        else if (matchLista.Success)
                        {
                            idNumerico = matchLista.Groups[1].Value;
                            contenidoPregunta = matchLista.Groups[2].Value.Trim();
                        }

                        var id = $"[{idNumerico}]";

                        preguntaActual = new PreguntaExamen
                        {
                            Id = id,
                            TextoCompleto = contenidoPregunta,
                            PuntosTotales = 0
                        };
                        preguntas[id] = preguntaActual;
                    }

                    // ---------------------------------------------------------
                    // CONTINUACIÓN (PEGAMENTO)
                    // ---------------------------------------------------------
                    if (!esNuevaPregunta && preguntaActual != null)
                    {
                        // Evitamos duplicar si el texto ya está incluido (a veces pasa con formatos mixtos)
                        if (!preguntaActual.TextoCompleto.Contains(texto))
                        {
                            preguntaActual.TextoCompleto += " " + texto;
                        }
                    }
                }
            }

            var resultado = preguntas.Values.OrderBy(p => ObtenerNumeroId(p.Id)).ToList();

            // --- PROCESO FINAL (Limpieza y Puntos) ---
            foreach (var pregunta in resultado)
            {
                // A. PUNTOS (Suma todas las etiquetas [1punto])
                pregunta.PuntosTotales = SumarTodosLosPuntos(pregunta.TextoCompleto);

                // B. VISOR (Separar Palabra : Definición)
                int indiceDosPuntos = pregunta.TextoCompleto.IndexOf(':');
                if (indiceDosPuntos > 0)
                {
                    pregunta.PalabraObjetivo = pregunta.TextoCompleto.Substring(0, indiceDosPuntos).Trim();
                    pregunta.TextoCompleto = pregunta.TextoCompleto.Substring(indiceDosPuntos + 1).Trim();
                }
                else
                {
                    pregunta.PalabraObjetivo = string.Empty;
                }

                // C. LIMPIEZA
                pregunta.TextoCompleto = Regex.Replace(pregunta.TextoCompleto, @"\[\d+\s*puntos?\]", "", RegexOptions.IgnoreCase);
                pregunta.TextoCompleto = Regex.Replace(pregunta.TextoCompleto, @"\s+", " ").Trim();
                pregunta.PalabraObjetivo = Regex.Replace(pregunta.PalabraObjetivo ?? "", @"\s+", " ").Trim();
            }

            return resultado;
        }

        private static double SumarTodosLosPuntos(string texto)
        {
            double total = 0;
            var matches = Regex.Matches(texto, @"\[(\d+)\s*puntos?\]", RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (double.TryParse(match.Groups[1].Value, out double valor))
                {
                    total += valor;
                }
            }
            return matches.Count > 0 ? total : 1.0;
        }

        private static int ObtenerNumeroId(string id)
        {
            var match = Regex.Match(id, @"\d+");
            return match.Success ? int.Parse(match.Value) : 9999;
        }
    }
}