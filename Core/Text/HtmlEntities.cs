namespace tool_cantam.Core.Text;

/// <summary>
/// HTML karakter varlıkları (entities). Kodlamada güvenli beşli kullanılır; çözmede HTML4'ün
/// yaygın adlandırılmış varlık kümesi tanınır. Adlar büyük/küçük harfe duyarlıdır
/// (<c>&amp;Auml;</c> ile <c>&amp;auml;</c> farklı karakterlerdir).
/// </summary>
public static class HtmlEntities
{
    /// <summary>Ad → karakter tanımları. Kod noktaları onaltılık yazılır.</summary>
    private static readonly (string Name, int CodePoint)[] Definitions =
    {
        // --- HTML'in beş kritik karakteri ---
        ("amp", 0x0026), ("lt", 0x003C), ("gt", 0x003E), ("quot", 0x0022), ("apos", 0x0027),

        // --- Latin-1 sembolleri ---
        ("nbsp", 0x00A0), ("iexcl", 0x00A1), ("cent", 0x00A2), ("pound", 0x00A3), ("curren", 0x00A4),
        ("yen", 0x00A5), ("brvbar", 0x00A6), ("sect", 0x00A7), ("uml", 0x00A8), ("copy", 0x00A9),
        ("ordf", 0x00AA), ("laquo", 0x00AB), ("not", 0x00AC), ("shy", 0x00AD), ("reg", 0x00AE),
        ("macr", 0x00AF), ("deg", 0x00B0), ("plusmn", 0x00B1), ("sup2", 0x00B2), ("sup3", 0x00B3),
        ("acute", 0x00B4), ("micro", 0x00B5), ("para", 0x00B6), ("middot", 0x00B7), ("cedil", 0x00B8),
        ("sup1", 0x00B9), ("ordm", 0x00BA), ("raquo", 0x00BB), ("frac14", 0x00BC), ("frac12", 0x00BD),
        ("frac34", 0x00BE), ("iquest", 0x00BF),

        // --- Latin-1 harfleri ---
        ("Agrave", 0x00C0), ("Aacute", 0x00C1), ("Acirc", 0x00C2), ("Atilde", 0x00C3), ("Auml", 0x00C4),
        ("Aring", 0x00C5), ("AElig", 0x00C6), ("Ccedil", 0x00C7), ("Egrave", 0x00C8), ("Eacute", 0x00C9),
        ("Ecirc", 0x00CA), ("Euml", 0x00CB), ("Igrave", 0x00CC), ("Iacute", 0x00CD), ("Icirc", 0x00CE),
        ("Iuml", 0x00CF), ("ETH", 0x00D0), ("Ntilde", 0x00D1), ("Ograve", 0x00D2), ("Oacute", 0x00D3),
        ("Ocirc", 0x00D4), ("Otilde", 0x00D5), ("Ouml", 0x00D6), ("times", 0x00D7), ("Oslash", 0x00D8),
        ("Ugrave", 0x00D9), ("Uacute", 0x00DA), ("Ucirc", 0x00DB), ("Uuml", 0x00DC), ("Yacute", 0x00DD),
        ("THORN", 0x00DE), ("szlig", 0x00DF), ("agrave", 0x00E0), ("aacute", 0x00E1), ("acirc", 0x00E2),
        ("atilde", 0x00E3), ("auml", 0x00E4), ("aring", 0x00E5), ("aelig", 0x00E6), ("ccedil", 0x00E7),
        ("egrave", 0x00E8), ("eacute", 0x00E9), ("ecirc", 0x00EA), ("euml", 0x00EB), ("igrave", 0x00EC),
        ("iacute", 0x00ED), ("icirc", 0x00EE), ("iuml", 0x00EF), ("eth", 0x00F0), ("ntilde", 0x00F1),
        ("ograve", 0x00F2), ("oacute", 0x00F3), ("ocirc", 0x00F4), ("otilde", 0x00F5), ("ouml", 0x00F6),
        ("divide", 0x00F7), ("oslash", 0x00F8), ("ugrave", 0x00F9), ("uacute", 0x00FA), ("ucirc", 0x00FB),
        ("uuml", 0x00FC), ("yacute", 0x00FD), ("thorn", 0x00FE), ("yuml", 0x00FF),

        // --- Tipografi ve Latin genişletmeleri ---
        ("OElig", 0x0152), ("oelig", 0x0153), ("Scaron", 0x0160), ("scaron", 0x0161), ("Yuml", 0x0178),
        ("fnof", 0x0192), ("circ", 0x02C6), ("tilde", 0x02DC), ("ensp", 0x2002), ("emsp", 0x2003),
        ("thinsp", 0x2009), ("zwnj", 0x200C), ("zwj", 0x200D), ("lrm", 0x200E), ("rlm", 0x200F),
        ("ndash", 0x2013), ("mdash", 0x2014), ("lsquo", 0x2018), ("rsquo", 0x2019), ("sbquo", 0x201A),
        ("ldquo", 0x201C), ("rdquo", 0x201D), ("bdquo", 0x201E), ("dagger", 0x2020), ("Dagger", 0x2021),
        ("bull", 0x2022), ("hellip", 0x2026), ("permil", 0x2030), ("prime", 0x2032), ("Prime", 0x2033),
        ("lsaquo", 0x2039), ("rsaquo", 0x203A), ("oline", 0x203E), ("frasl", 0x2044), ("euro", 0x20AC),
        ("trade", 0x2122),

        // --- Matematik ve oklar ---
        ("larr", 0x2190), ("uarr", 0x2191), ("rarr", 0x2192), ("darr", 0x2193), ("harr", 0x2194),
        ("minus", 0x2212), ("lowast", 0x2217), ("radic", 0x221A), ("prop", 0x221D), ("infin", 0x221E),
        ("ang", 0x2220), ("and", 0x2227), ("or", 0x2228), ("cap", 0x2229), ("cup", 0x222A),
        ("int", 0x222B), ("there4", 0x2234), ("sim", 0x223C), ("cong", 0x2245), ("asymp", 0x2248),
        ("ne", 0x2260), ("equiv", 0x2261), ("le", 0x2264), ("ge", 0x2265), ("sub", 0x2282),
        ("sup", 0x2283), ("nsub", 0x2284), ("sube", 0x2286), ("supe", 0x2287), ("oplus", 0x2295),
        ("otimes", 0x2297), ("perp", 0x22A5), ("sdot", 0x22C5),

        // --- Teknik ve iskambil sembolleri ---
        ("lceil", 0x2308), ("rceil", 0x2309), ("lfloor", 0x230A), ("rfloor", 0x230B), ("lang", 0x2329),
        ("rang", 0x232A), ("loz", 0x25CA), ("spades", 0x2660), ("clubs", 0x2663), ("hearts", 0x2665),
        ("diams", 0x2666)
    };

    private static readonly Dictionary<string, string> NamedEntities = BuildNamedEntities();

    /// <summary>Kodlamada adlandırılmış varlık kullanılabilecek karakterler.</summary>
    private static readonly Dictionary<char, string> EncodableCharacters = new()
    {
        ['&'] = "amp",
        ['<'] = "lt",
        ['>'] = "gt",
        ['"'] = "quot"
        // Tek tırnak için sayısal gösterim (&#39;) kullanılır: &apos; HTML4'te tanımlı değildir.
    };

    /// <summary>
    /// HTML5'in sayısal karakter referansları için tanımladığı eski Windows-1252 eşlemesi.
    /// <c>&amp;#150;</c> gibi referanslar en tire (U+2013) olarak yorumlanır.
    /// </summary>
    private static readonly Dictionary<int, int> LegacyCodePointRemap = new()
    {
        [0x80] = 0x20AC, [0x82] = 0x201A, [0x83] = 0x0192, [0x84] = 0x201E, [0x85] = 0x2026,
        [0x86] = 0x2020, [0x87] = 0x2021, [0x88] = 0x02C6, [0x89] = 0x2030, [0x8A] = 0x0160,
        [0x8B] = 0x2039, [0x8C] = 0x0152, [0x8E] = 0x017D, [0x91] = 0x2018, [0x92] = 0x2019,
        [0x93] = 0x201C, [0x94] = 0x201D, [0x95] = 0x2022, [0x96] = 0x2013, [0x97] = 0x2014,
        [0x98] = 0x02DC, [0x99] = 0x2122, [0x9A] = 0x0161, [0x9B] = 0x203A, [0x9C] = 0x0153,
        [0x9E] = 0x017E, [0x9F] = 0x0178
    };

    /// <summary>Adlandırılmış varlığı karaktere çevirir.</summary>
    public static bool TryGetCharacter(string name, out string value) => NamedEntities.TryGetValue(name, out value!);

    /// <summary>Karakter için kodlamada kullanılacak varlık adını döndürür; yoksa null.</summary>
    public static string? GetEntityName(char character) =>
        EncodableCharacters.TryGetValue(character, out string? name) ? name : null;

    /// <summary>Sayısal referansı HTML5 kurallarına göre düzeltir (eski Windows-1252 aralığı).</summary>
    public static int RemapLegacyCodePoint(int codePoint) =>
        LegacyCodePointRemap.TryGetValue(codePoint, out int remapped) ? remapped : codePoint;

    private static Dictionary<string, string> BuildNamedEntities()
    {
        var map = new Dictionary<string, string>(Definitions.Length, StringComparer.Ordinal);

        foreach ((string name, int codePoint) in Definitions)
            map[name] = char.ConvertFromUtf32(codePoint);

        return map;
    }
}
