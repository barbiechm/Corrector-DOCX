using COMPARARNOTAS.Models;
using System.Text.RegularExpressions;
using System.Linq;

namespace COMPARARNOTAS.Services
{
    public class ComparadorLinguistico
    {
        private const double UMBRAL_APROBACION = 0.90;

        public static ResultadoComparacion CompararExamenes(
            List<PreguntaExamen> clave,
            List<PreguntaExamen> estudiante)
        {
            var resultado = new ResultadoComparacion();

            foreach (var pregClave in clave)
            {
                var pregEstudiante = estudiante.FirstOrDefault(p => p.Id == pregClave.Id);

                if (pregEstudiante == null || string.IsNullOrWhiteSpace(pregEstudiante.TextoCompleto))
                {
                    pregClave.Estado = "INCORRECTO";
                    pregClave.PuntosGanados = 0;
                    pregClave.Respondida = false;
                    pregClave.RespuestaEstudiante = "(No se encontró respuesta)";
                    pregClave.Feedback = "No respondió esta pregunta.";
                }
                else
                {
                    // 1. Extraer FRASES DE LA CLAVE (Aquí sí usamos comas porque el profe escribe bien)
                    var frasesClave = ExtraerFrasesClave(pregClave.TextoCompleto);

                    // 2. PREPARAR TEXTO ALUMNO (Lo convertimos en un bloque de texto limpio)
                    string textoAlumnoLimpio = LimpiarTextoAlumno(pregEstudiante.TextoCompleto);

                    // 3. BUSCAR Y CAPTURAR (Lógica nueva)
                    int aciertosReales = CalcularAciertosAvanzado(frasesClave, ref textoAlumnoLimpio);

                    // 4. Asignación de Puntaje
                    double puntosFinales = Math.Min(aciertosReales, pregClave.PuntosTotales);

                    pregClave.PuntosGanados = puntosFinales;

                    // Cálculo visual
                    double ratio = pregClave.PuntosTotales > 0 ? puntosFinales / pregClave.PuntosTotales : 0;
                    pregClave.PorcentajeAcierto = ratio * 100;

                    pregClave.Respondida = true;
                    pregClave.RespuestaEstudiante = pregEstudiante.TextoCompleto;

                    // 5. Estado
                    if (aciertosReales >= pregClave.PuntosTotales && pregClave.PuntosTotales > 0)
                        pregClave.Estado = "CORRECTO";
                    else if (puntosFinales > 0)
                        pregClave.Estado = "PARCIAL";
                    else
                        pregClave.Estado = "INCORRECTO";

                    pregClave.Feedback = GenerarFeedback(frasesClave, pregEstudiante.TextoCompleto);
                }

                resultado.Preguntas.Add(pregClave);
            }

            resultado.CalcularEstadisticas();
            return resultado;
        }

        // Método para el PROFESOR (Usa comas para separar conceptos)
        private static List<string> ExtraerFrasesClave(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return new List<string>();

            // Separamos por comas y limpiamos etiquetas
            var fragmentos = texto.ToLowerInvariant().Split(new[] { ',', '.', ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var frases = new List<string>();

            foreach (var fragmento in fragmentos)
            {
                string limpio = LimpiarEtiquetas(fragmento);
                if (limpio.Length > 0) frases.Add(limpio);
            }
            return frases;
        }

        // Método para el ALUMNO (Genera una sola "sopa de letras" limpia)
        private static string LimpiarTextoAlumno(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";

            // Convertimos a minúsculas
            string t = texto.ToLowerInvariant();

            // Reemplazamos comas y puntos por espacios para unificar
            t = t.Replace(",", " ").Replace(".", " ").Replace(";", " ");

            // Limpiamos las etiquetas (persona:, numero:, etc) del texto completo
            t = LimpiarEtiquetas(t);

            // Normalizamos espacios dobles
            t = Regex.Replace(t, @"\s+", " ").Trim();

            return t; // Retorna ej: "artículo determinado femenino singular"
        }

        private static string LimpiarEtiquetas(string texto)
        {
            var etiquetasIgnorar = new string[]
            {
                "persona:", "numero:", "número:", "tiempo:", "aspecto:",
                "modo:", "tipo de conjugación:", "tipo de conjugacion:",
                "vocal temática:", "vocal tematica:", "voz:", "flexión:",
                "flexion:", "significado:", "estructura:"
            };

            foreach (var etiqueta in etiquetasIgnorar)
            {
                texto = texto.Replace(etiqueta, " "); // Reemplazar por espacio, no borrar pegado
            }

            // Limpieza de caracteres raros
            return texto.Replace("\"", "").Replace("“", "").Replace("”", "").Replace("'", "").Trim();
        }

        // Lógica de "Búsqueda y Captura"
        private static int CalcularAciertosAvanzado(List<string> frasesClave, ref string textoAlumno)
        {
            int aciertos = 0;

            foreach (var clave in frasesClave)
            {
                // Manejo de opciones "O" (Ej: "determinado o definido")
                var opciones = clave.Split(new[] { " o " }, StringSplitOptions.RemoveEmptyEntries);
                bool encontrado = false;

                foreach (var opcion in opciones)
                {
                    string opcionLimpia = opcion.Trim();

                    // Buscamos la palabra/frase completa dentro del texto del alumno
                    // Usamos \b para asegurar que sean palabras completas (evitar que "uno" coincida con "alguno")
                    string patron = $@"\b{Regex.Escape(opcionLimpia)}\b";

                    if (Regex.IsMatch(textoAlumno, patron))
                    {
                        aciertos++;
                        encontrado = true;

                        // "Tachamos" la palabra encontrada para no contarla dos veces
                        // Reemplazamos solo la primera ocurrencia
                        var regex = new Regex(patron);
                        textoAlumno = regex.Replace(textoAlumno, " ", 1);
                        break; // Ya encontró una de las opciones, pasa a la siguiente clave
                    }
                }
            }
            return aciertos;
        }

        private static string GenerarFeedback(List<string> esperado, string textoAlumnoRaw)
        {
            // Feedback simplificado para no complicar la lógica inversa
            // Verificamos qué claves NO están en el texto original
            var faltantes = new List<string>();
            string textoNormalizado = LimpiarTextoAlumno(textoAlumnoRaw);

            foreach (var clave in esperado)
            {
                bool esta = false;
                var opciones = clave.Split(new[] { " o " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var op in opciones)
                {
                    if (Regex.IsMatch(textoNormalizado, $@"\b{Regex.Escape(op.Trim())}\b"))
                    {
                        esta = true;
                        break;
                    }
                }
                if (!esta) faltantes.Add(clave);
            }

            if (!faltantes.Any()) return "✓ Correcto";
            return $"Faltó: {string.Join(", ", faltantes)}";
        }
    }
}