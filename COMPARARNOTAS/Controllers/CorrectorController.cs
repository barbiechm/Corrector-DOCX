using COMPARARNOTAS.Models;
using HtmlAgilityPack;
using Mammoth;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CorrectorExamenesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CorrectorController : ControllerBase
    {
        [HttpPost("procesar")]
        public IActionResult ProcesarExamen(IFormFile archivoProfesor, IFormFile archivoEstudiante)
        {
            // 1. VALIDACIÓN BÁSICA
            if (archivoProfesor == null || archivoEstudiante == null)
                return BadRequest("Faltan archivos.");

            // 2. VALIDACIÓN DE EXTENSIÓN (Seguridad extra)
            var extProf = Path.GetExtension(archivoProfesor.FileName).ToLower();
            var extEst = Path.GetExtension(archivoEstudiante.FileName).ToLower();

            if (extProf != ".docx" || extEst != ".docx")
            {
                return BadRequest("Solo se permiten archivos de Word (.docx).");
            }

            try
            {
                // 3. CONVERSIÓN (Protegida contra archivos corruptos)
                var htmlProf = ConvertirDocxAHtml(archivoProfesor);
                var htmlEst = ConvertirDocxAHtml(archivoEstudiante);

                // 4. EXTRACCIÓN DE DATOS
                var listaProfesor = ExtraerDatos(htmlProf, true);
                var listaEstudiante = ExtraerDatos(htmlEst, false);

                // 5. VALIDACIÓN ANTI-GOOGLE DOCS (Tu código actual)
                if (listaProfesor.Count == 0)
                {
                    return BadRequest("Error: No se encontraron preguntas en el archivo del PROFESOR.\n" +
                                      "Causas probables:\n" +
                                      "1. El archivo fue creado en Google Docs (ábrelo en Word y guárdalo de nuevo).\n" +
                                      "2. El formato de la tabla no es estándar.");
                }

                if (listaEstudiante.Count == 0)
                {
                    return BadRequest("Error: El archivo del ESTUDIANTE parece estar vacío o sin formato de tabla válido.");
                }

                // 6. CÁLCULO DE NOTAS
                double puntosTotalesExamen = 0;
                double puntosObtenidosExamen = 0;
                var detalleRevision = new List<object>();

                for (int i = 0; i < listaProfesor.Count; i++)
                {
                    var profe = listaProfesor[i];
                    puntosTotalesExamen += profe.Puntos;

                    var alumno = (i < listaEstudiante.Count) ? listaEstudiante[i] : null;

                    // A. OBTENER TOKENS DEL PROFESOR (Por Bloques)
                    var tokensGramProfe = Normalizador.ObtenerTokensPorBloques(profe.Gramatical);
                    var tokensClasProfe = Normalizador.ObtenerTokensPorBloques(profe.Clasificacion);

                    // Limpieza de duplicados o vacíos en el profesor para evitar conteos falsos
                    tokensClasProfe = tokensClasProfe.Distinct().Where(t => t.Length > 1).ToList();

                    // Detección de Trampa (Fila vacía)
                    bool esTrampa = (tokensGramProfe.Count + tokensClasProfe.Count) == 0;

                    // VARIABLES DE PUNTUACIÓN
                    double ganadosGram = 0;
                    double ganadosClas = 0;

                    int aciertosGram = 0;
                    int aciertosClas = 0;
                    string estado = "NO RESPONDIÓ";

                    bool tieneRespuesta = alumno != null && alumno.Respondida;

                    if (tieneRespuesta || (esTrampa && alumno != null))
                    {
                        // --- 1. Evaluar Gramática (ESTRICTA: 1 punto o 0) ---
                        var tokensGramAlumno = Normalizador.ObtenerTokensPorBloques(alumno!.Gramatical);
                        aciertosGram = ContarCoincidencias(tokensGramProfe, tokensGramAlumno);

                        // Si el profesor puso algo y el alumno acertó todo
                        if (tokensGramProfe.Count > 0 && aciertosGram >= tokensGramProfe.Count)
                        {
                            ganadosGram = 1.0;
                        }

                        // --- 2. Evaluar Clasificación (SUMATIVA: 1 punto por acierto) ---
                        var tokensClasAlumno = Normalizador.ObtenerTokensPorBloques(alumno!.Clasificacion);
                        aciertosClas = ContarCoincidencias(tokensClasProfe, tokensClasAlumno);

                        // Multiplicamos por 1 directamente (7 aciertos = 7 puntos)
                        ganadosClas = aciertosClas * 1.0;

                        // --- 3. ESTADO VISUAL ---
                        if (esTrampa)
                        {
                            estado = (tokensGramAlumno.Count > 0 || tokensClasAlumno.Count > 0) ? "TRAMPA" : "NEUTRO";
                        }
                        else
                        {
                            int totalAciertos = aciertosGram + aciertosClas;
                            int totalItems = tokensGramProfe.Count + tokensClasProfe.Count;

                            if (totalAciertos >= totalItems) estado = "CORRECTA";
                            else if (totalAciertos == 0) estado = "INCORRECTA";
                            else estado = "PARCIAL";
                        }
                    }

                    // SUMA FINAL
                    double puntosGanadosFila = ganadosGram + ganadosClas;

                    // TOPE DE SEGURIDAD:
                    // Si el alumno sacó 12 puntos (1 gram + 11 clas) pero la pregunta vale 11, se queda en 11.
                    if (puntosGanadosFila > profe.Puntos) puntosGanadosFila = profe.Puntos;

                    puntosObtenidosExamen += puntosGanadosFila;

                    // PREPARAR JSON
                    detalleRevision.Add(new
                    {
                        Id = profe.Id,
                        Pregunta = profe.Texto,
                        ValorTotal = profe.Puntos,
                        ValorObtenido = Math.Round(puntosGanadosFila, 2),
                        Estado = estado,
                        Gramatica = new
                        {
                            Esperada = profe.Gramatical,
                            Recibida = alumno?.Gramatical ?? "---",
                            Aciertos = aciertosGram,
                            TotalItems = tokensGramProfe.Count,
                            EsCorrecta = aciertosGram >= tokensGramProfe.Count && tokensGramProfe.Count > 0
                        },
                        Clasificacion = new
                        {
                            Esperada = profe.Clasificacion,
                            Recibida = alumno?.Clasificacion ?? "---",
                            Aciertos = aciertosClas,
                            TotalItems = tokensClasProfe.Count,
                            EsCorrecta = aciertosClas >= tokensClasProfe.Count && tokensClasProfe.Count > 0
                        }
                    });
                }

                // NOTA FINAL (Base 20)
                double notaBase20 = puntosTotalesExamen > 0
                    ? (puntosObtenidosExamen * 20 / puntosTotalesExamen)
                    : 0;

                return Ok(new
                {
                    Resumen = new
                    {
                        NotaFinal = Math.Round(notaBase20, 2),
                        PuntosObtenidos = Math.Round(puntosObtenidosExamen, 2),
                        PuntosTotales = puntosTotalesExamen,
                        TotalPreguntas = listaProfesor.Count
                    },
                    Detalle = detalleRevision
                });
            }
            catch (Exception ex)
            {
                // CAPTURA DE ERROR: Si Mammoth falla, te dirá por qué.
                return BadRequest($"Error al leer el archivo (posible archivo corrupto o protegido). Detalles: {ex.Message}");
            }
        }

        // --- FUNCIONES AUXILIARES ---
        private int ContarCoincidencias(List<string> listaProfe, List<string> listaAlumno)
        {
            int aciertos = 0;
            var copiaAlumno = new List<string>(listaAlumno);

            foreach (var concepto in listaProfe)
            {
                int indice = copiaAlumno.IndexOf(concepto);
                if (indice != -1)
                {
                    aciertos++;
                    copiaAlumno.RemoveAt(indice);
                }
            }
            return aciertos;
        }

        // --- LECTURA DE ARCHIVOS ---
        private string ConvertirDocxAHtml(IFormFile archivo)
        {
            using (var stream = archivo.OpenReadStream())
            {
                var converter = new DocumentConverter();
                var result = converter.ConvertToHtml(stream);
                return result.Value;
            }
        }

        private List<PreguntaExamen> ExtraerDatos(string html, bool esProfesor)
        {
            var lista = new List<PreguntaExamen>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var tabla = doc.DocumentNode.SelectSingleNode("//table");
            if (tabla == null) return lista;
            var filas = tabla.SelectNodes(".//tr");
            if (filas == null) return lista;

            int idxPts = 1;
            int idxGram = esProfesor ? 2 : 1;
            int idxClas = esProfesor ? 3 : 2;

            for (int i = 1; i < filas.Count; i++)
            {
                var celdas = filas[i].SelectNodes("td");
                if (celdas == null) continue;
                if (esProfesor && celdas.Count < 4) continue;
                if (!esProfesor && celdas.Count < 3) continue;

                var textoBruto = celdas[0].InnerText.Trim();

                var item = new PreguntaExamen
                {
                    Id = Regex.Match(textoBruto, @"^(\d+)").Value,
                    Texto = textoBruto,
                    Puntos = esProfesor ? ExtraerPuntos(celdas[idxPts].InnerText) : 0,
                    Gramatical = celdas[idxGram].InnerText.Trim(),
                    Clasificacion = celdas[idxClas].InnerText.Trim()
                };

                if (!string.IsNullOrWhiteSpace(item.Gramatical) || !string.IsNullOrWhiteSpace(item.Clasificacion))
                    item.Respondida = true;

                lista.Add(item);
            }
            return lista;
        }

        private double ExtraerPuntos(string texto)
        {
            var match = Regex.Match(texto, @"[0-9]+(\.[0-9]+)?");
            return match.Success ? double.Parse(match.Value) : 0;
        }
    }
}