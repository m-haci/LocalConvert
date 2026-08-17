namespace LocalConvert.Core.Conversion;

public enum ConversionErrorCode
{
    Unknown = 0,
    FileNotFound = 1,
    FileEncrypted = 2,
    FileCorrupt = 3,
    UnsupportedFormat = 4,
    InvalidPageRange = 5,
    Cancelled = 6,
    OutputWriteFailed = 7,
    TemporaryFileFailed = 8,
    ConverterNotFound = 9,
    InvalidInput = 10,
    OfficeEngineMissing = 11
}
