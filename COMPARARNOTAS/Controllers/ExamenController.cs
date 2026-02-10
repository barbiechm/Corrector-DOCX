using Microsoft.AspNetCore.Mvc;
using COMPARARNOTAS.Services;

namespace COMPARARNOTAS.Controllers
{
    [ApiController]
   
    [Route("api/examen")]
    public class ExamenController : ControllerBase
    {
        [HttpPost("comparar")]
        public async Task<IActionResult> CompararExamenes(
            [FromForm] IFormFile archivoRespuestas,
            [FromForm] IFormFile archivoClave)
        {
            try
            {
                if (archivoRespuestas == null || archivoClave == null)
                    return BadRequest(new { error = "Debe enviar ambos archivos" });

                var rutaRespuestas = Path.GetTempFileName();
                var rutaClave = Path.GetTempFileName();

                using (var stream = new FileStream(rutaRespuestas, FileMode.Create))
                    await archivoRespuestas.CopyToAsync(stream);

                using (var stream = new FileStream(rutaClave, FileMode.Create))
                    await archivoClave.CopyToAsync(stream);

                Console.WriteLine("📂 Extrayendo preguntas del estudiante...");
                var preguntasEstudiante = ExtractorExamenes.ExtraerPreguntas(rutaRespuestas);

                Console.WriteLine("📂 Extrayendo preguntas clave...");
                var preguntasClave = ExtractorExamenes.ExtraerPreguntas(rutaClave);

                if (!preguntasClave.Any())
                    return BadRequest(new { error = "No se encontraron preguntas en el archivo clave" });

                Console.WriteLine("🔍 Comparando exámenes...");
                var resultado = ComparadorLinguistico.CompararExamenes(preguntasClave, preguntasEstudiante);
                Console.WriteLine($"✅ Comparación completada");

                // Limpieza
                if (System.IO.File.Exists(rutaRespuestas)) System.IO.File.Delete(rutaRespuestas);
                if (System.IO.File.Exists(rutaClave)) System.IO.File.Delete(rutaClave);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }
    }
}