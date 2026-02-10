using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Normalizador
{
    // Diccionario SIN duplicados
    private static readonly Dictionary<string, string> _sinonimos = new Dictionary<string, string>
    {
        // Tiempos verbales
        { "pasado", "preterito" }, { "pret", "preterito" },
        { "fut", "futuro" }, { "futur", "futuro" },
        { "pres", "presente" }, { "copret", "copreterito" },
        { "imperf", "imperfecto" }, { "perf", "perfecto" },
        
        // Categorías gramaticales
        { "sustantivo", "nombre" }, { "sust", "nombre" },
        { "adj", "adjetivo" }, { "v", "verbo" },
        { "adv", "adverbio" }, { "prep", "preposicion" },
        { "art", "articulo" }, { "pron", "pronombre" },
        
        // Género y Número
        { "fem", "femenino" }, { "f", "femenino" },
        { "masc", "masculino" }, { "m", "masculino" },
        { "sing", "singular" }, { "s", "singular" },
        { "plur", "plural" }, { "pl", "plural" },
        
        // Voz y Modo
        { "activa", "activo" }, { "act", "activo" },
        { "pasiva", "pasivo" }, { "pas", "pasivo" },
        { "indic", "indicativo" }, { "ind", "indicativo" },
        { "subj", "subjuntivo" }, { "sub", "subjuntivo" },
        { "imper", "imperativo" }, { "imp", "imperativo" },
        
        // Personas
        { "1", "primera" }, { "1a", "primera" }, { "1era", "primera" },
        { "2", "segunda" }, { "2a", "segunda" }, { "2da", "segunda" },
        { "3", "tercera" }, { "3a", "tercera" }, { "3ra", "tercera" },
        
        // Tipos específicos
        { "det", "determinado" }, { "indet", "indeterminado" },
        { "definido", "determinado" }, { "indefinido", "indeterminado" },
        { "demos", "demostrativo" }, { "comun", "comun" },
        { "incontable", "incontable" }, { "reciproco", "reciproco" },
        { "atono", "atono" }, { "tonico", "tonico" },

        // Palabras técnicas que queremos normalizar para que coincidan
        { "intransitivo", "intransitivo" }, { "transitivo", "transitivo" },
        { "derivado", "derivado" }, { "primitivo", "primitivo" },
        { "simple", "simple" }, { "compuesto", "compuesto" }
    };

    // Palabras que NO aportan valor (Etiquetas y basura)
    private static readonly HashSet<string> _stopWords = new HashSet<string>
    {
        // Artículos y preposiciones
        "de", "del", "la", "el", "los", "las", "un", "una", "unos", "unas",
        "en", "a", "al", "o", "y", "u", "e", "con", "sin", "por", "para",
        "es", "son", "fue", "fueron", "era", "eran", "se", "que", "cual",
        "muy", "mas", "menos",

        // Etiquetas Estructurales (Ignorar "Persona:", "Número:", etc.)
        "persona", "numero", "tiempo", "aspecto", "modo",
        "tipo", "clase", "forma", "conjugacion", "vocal", "tematica",
        "voz", "flexion", "significado", "estructura",
        "palabra", "valor", "genero", "funcion", "grado",
        
        // Ignorar también la palabra "punto" o "puntos" si se escapó del regex
        "punto", "puntos"
    };

    public static List<string> ExtraerConceptosLinguisticos(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return new List<string>();

        try
        {
            // 1. Limpiar etiquetas de puntos (Ej: "[1punto]" -> " ")
            string textoSinPuntos = Regex.Replace(texto, @"\[\d+\s*puntos?\]", " ", RegexOptions.IgnoreCase);

            // 2. Decodificar HTML y minusculas (USANDO textoSinPuntos ✅)
            var limpio = System.Net.WebUtility.HtmlDecode(textoSinPuntos).ToLowerInvariant();

            // 3. Quitar tildes
            limpio = QuitarTildes(limpio);

            // 4. Quitar caracteres especiales (dejar solo letras y números)
            limpio = Regex.Replace(limpio, @"[^\w\s]", " ");

            // 5. Dividir en palabras
            var palabras = limpio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // 6. Normalizar y Filtrar
            // Usamos List<string> para permitir palabras repetidas si el profe lo pide así
            var conceptos = new List<string>();

            foreach (var palabra in palabras)
            {
                if (string.IsNullOrWhiteSpace(palabra) || _stopWords.Contains(palabra))
                    continue;

                // Aplicar sinónimo si existe
                var concepto = _sinonimos.TryGetValue(palabra, out var sinonimo) ? sinonimo : palabra;
                conceptos.Add(concepto);
            }

            // Ordenamos para que la comparación visual sea más fácil
            return conceptos.OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Normalizador: {ex.Message}");
            return new List<string>();
        }
    }

    private static string QuitarTildes(string texto)
    {
        try
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
        catch { return texto; }
    }
}