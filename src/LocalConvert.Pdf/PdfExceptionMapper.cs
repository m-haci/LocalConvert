using LocalConvert.Core.Conversion;
using PdfSharp.Pdf.IO;

namespace LocalConvert.Pdf;

internal static class PdfExceptionMapper
{
    public static ConversionErrorCode FromReader(PdfReaderException exception)
    {
        return MapReaderException(exception);
    }

    public static ConversionErrorCode MapReaderException(PdfReaderException exception)
    {
        var message = exception.Message ?? string.Empty;
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return ConversionErrorCode.FileEncrypted;
        }

        return ConversionErrorCode.FileCorrupt;
    }

    public static ConversionErrorCode MapMessage(Exception exception)
    {
        var message = exception.Message ?? string.Empty;
        if (message.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("encrypt", StringComparison.OrdinalIgnoreCase))
        {
            return ConversionErrorCode.FileEncrypted;
        }

        return ConversionErrorCode.FileCorrupt;
    }
}
