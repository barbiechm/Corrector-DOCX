using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Normalizador
{
    // SINÓNIMOS (Reglas de negocio)
    private static readonly Dictionary<string, string> _sinonimos = new Dictionary<string, string>
    {
        { "pasado", "preterito" },
        { "fut", "futuro" },
        { "sustantivo", "nombre" },
        { "fem", "femenino" },
        { "masc", "masculino" },
        { "sing", "singular" },
        { "plur", "plural" },
        { "activa", "activo" },
        { "pasiva", "pasivo" },
        { "indic", "indicativo" },
        { "subj", "subjuntivo" },
        { "perf", "perfectivo" },
        { "imperf", "imperfectivo" },
        { "1", "primera" },
        { "2", "segunda" },
        { "3", "tercera" }
    };

    // PALABRAS QUE NO SUMAN PUNTOS (Para el método antiguo, si lo usas)
    private static readonly HashSet<string> _palabrasIgnoradas = new HashSet<string>
    {
        "tiempo", "modo", "aspecto", "persona", "numero", "número", "tipo", "clase",
        "genero", "género", "voz", "grado", "conjugacion", "conjugación",
        "vocal", "tematica", "temática", "significado", "estructura", "forma", "flexion", "flexión",
        "de", "del", "el", "la", "los", "las", "un", "una", "unos", "unas",
        "y", "o", "u", "e", "es", "son", ":", ",", ".", ";", "-", "\"", "'"
    };

    // --- MÉTODO ANTIGUO (PALABRA POR PALABRA) ---
    public static List<string> ObtenerTokens(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return new List<string>();

        var limpio = System.Net.WebUtility.HtmlDecode(texto).ToLowerInvariant().Trim();
        limpio = QuitarTildes(limpio);
        limpio = Regex.Replace(limpio, @"[^\w]", " ");

        var partes = limpio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<string>();

        foreach (var parte in partes)
        {
            var p = parte.Trim();
            var palabraFinal = _sinonimos.ContainsKey(p) ? _sinonimos[p] : p;
            if (!_palabrasIgnoradas.Contains(palabraFinal))
            {
                tokens.Add(palabraFinal);
            }
        }
        return tokens;
    }

    private static string QuitarTildes(string texto)
    {
        var normalizedString = texto.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (var c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                stringBuilder.Append(c);
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    // --- MÉTODO NUEVO: POR BLOQUES (CORREGIDO CON SINÓNIMOS) ---
    public static List<string> ObtenerTokensPorBloques(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return new List<string>();

        // 1. Decodificar y minúsculas
        var limpio = System.Net.WebUtility.HtmlDecode(texto).ToLowerInvariant();
        limpio = QuitarTildes(limpio);

        // 2. Separar por CUALQUIER signo de puntuación común
        // Agregamos punto (.), punto y coma (;), y saltos de línea (\n)
        var bloques = limpio.Split(new[] { ',', '.', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        var tokens = new List<string>();

        foreach (var bloque in bloques)
        {
            // 3. Eliminar la etiqueta (lo que está antes del ':')
            string contenido = bloque;
            if (bloque.Contains(':'))
            {
                var partesBloque = bloque.Split(new[] { ':' }, 2);
                if (partesBloque.Length > 1)
                    contenido = partesBloque[1];
            }

            // 4. Limpieza: Quitamos comillas, paréntesis y caracteres raros
            contenido = Regex.Replace(contenido, @"[^\w\s]", "").Trim();

            // 5. Aplicar Sinónimos
            var palabras = contenido.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var palabrasCorregidas = new List<string>();

            foreach (var p in palabras)
            {
                var termino = _sinonimos.ContainsKey(p) ? _sinonimos[p] : p;
                palabrasCorregidas.Add(termino);
            }

            var contenidoFinal = string.Join(" ", palabrasCorregidas);

            if (!string.IsNullOrWhiteSpace(contenidoFinal))
            {
                tokens.Add(contenidoFinal);
            }
        }

        return tokens;
    }
}