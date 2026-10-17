namespace tool_cantam.Core.Enums;

/// <summary>Hash aracında seçilebilen algoritmalar.</summary>
public enum HashAlgorithmKind
{
    Md5,
    Sha1,
    Sha256,
    Sha384,
    Sha512,
    Sha3_256,
    Sha3_512
}

/// <summary>Özetin gösterim biçimi.</summary>
public enum HashOutputFormat
{
    /// <summary>Büyük harfli onaltılık (varsayılan).</summary>
    HexUppercase,

    /// <summary>Küçük harfli onaltılık.</summary>
    HexLowercase,

    /// <summary>Base64.</summary>
    Base64
}
