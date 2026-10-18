namespace tool_cantam.Services.ErrorCodes;

public static class CsvToJsonErrorCodes
{
    public const string EmptyInput = "L.CsvToJson.Error.EmptyInput";
    public const string InputTooLarge = "L.CsvToJson.Error.InputTooLarge";
    public const string InvalidDelimiter = "L.CsvToJson.Error.InvalidDelimiter";
    public const string InconsistentColumns = "L.CsvToJson.Error.InconsistentColumns";
    public const string Unexpected = "L.Common.Error.Unexpected";
}