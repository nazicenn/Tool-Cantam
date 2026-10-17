namespace tool_cantam.Core.Enums;

/// <summary>Girdinin karakter kümesi; tanımlamanın ilk adımı.</summary>
public enum HashCharacterSet
{
    /// <summary>Yalnızca onaltılık basamaklar (0-9, a-f).</summary>
    Hex,

    /// <summary>Base64 alfabesi (A-Z, a-z, 0-9, +, / ve sonda en fazla iki '=').</summary>
    Base64,

    /// <summary>Unix crypt alfabesi (A-Z, a-z, 0-9, '.', '/').</summary>
    DesCrypt,

    /// <summary>Yalnızca harf ve rakam.</summary>
    Alphanumeric,

    /// <summary>Bilinen bir küme değil.</summary>
    Other
}
