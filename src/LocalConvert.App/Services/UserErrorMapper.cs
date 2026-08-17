using LocalConvert.Core.Conversion;

namespace LocalConvert.App.Services;

public static class UserErrorMapper
{
    public static string TitleKey(ConversionErrorCode code)
    {
        return code switch
        {
            ConversionErrorCode.FileNotFound => "Error_FileNotFound",
            ConversionErrorCode.FileEncrypted => "Error_FileEncrypted",
            ConversionErrorCode.FileCorrupt => "Error_FileCorrupt",
            ConversionErrorCode.UnsupportedFormat => "Error_UnsupportedFormat",
            ConversionErrorCode.InvalidPageRange => "Error_InvalidPageRange",
            ConversionErrorCode.Cancelled => "Error_Cancelled",
            ConversionErrorCode.OutputWriteFailed => "Error_OutputWriteFailed",
            ConversionErrorCode.TemporaryFileFailed => "Error_TemporaryFileFailed",
            ConversionErrorCode.ConverterNotFound => "Error_ConverterNotFound",
            ConversionErrorCode.InvalidInput => "Error_InvalidInput",
            ConversionErrorCode.OfficeEngineMissing => "Error_OfficeEngineMissing",
            _ => "Error_Unknown"
        };
    }

    public static string HintKey(ConversionErrorCode code)
    {
        return TitleKey(code) + "_Hint";
    }
}
