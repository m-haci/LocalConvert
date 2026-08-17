using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using LocalConvert.Core.Conversion;
using LocalConvert.Core.Logging;
using LocalConvert.Core.Office;
using Microsoft.Extensions.Logging;

namespace LocalConvert.Office;

public sealed class WordToPdfConverter : OfficeDocumentToPdfConverter
{
    public WordToPdfConverter(IOfficeAvailability availability, ILogger<WordToPdfConverter> logger)
        : base(availability, logger, new OfficeConverterProfile(
            ConverterIds.WordToPdf,
            "Tool_WordToPdf",
            "Tool_WordToPdf_Desc",
            "\uE8A5",
            FileExtensions.Word,
            "Word.Application",
            OfficeAppKind.Word))
    {
    }
}

public sealed class PowerPointToPdfConverter : OfficeDocumentToPdfConverter
{
    public PowerPointToPdfConverter(IOfficeAvailability availability, ILogger<PowerPointToPdfConverter> logger)
        : base(availability, logger, new OfficeConverterProfile(
            ConverterIds.PowerPointToPdf,
            "Tool_PowerPointToPdf",
            "Tool_PowerPointToPdf_Desc",
            "\uE7F6",
            FileExtensions.PowerPoint,
            "PowerPoint.Application",
            OfficeAppKind.PowerPoint))
    {
    }
}

public sealed class ExcelToPdfConverter : OfficeDocumentToPdfConverter
{
    public ExcelToPdfConverter(IOfficeAvailability availability, ILogger<ExcelToPdfConverter> logger)
        : base(availability, logger, new OfficeConverterProfile(
            ConverterIds.ExcelToPdf,
            "Tool_ExcelToPdf",
            "Tool_ExcelToPdf_Desc",
            "\uE9F9",
            FileExtensions.Excel,
            "Excel.Application",
            OfficeAppKind.Excel))
    {
    }
}

public enum OfficeAppKind
{
    Word,
    PowerPoint,
    Excel
}

public sealed record OfficeConverterProfile(
    string Id,
    string DisplayNameKey,
    string DescriptionKey,
    string Glyph,
    IReadOnlyList<string> InputExtensions,
    string ProgId,
    OfficeAppKind AppKind);

public abstract class OfficeDocumentToPdfConverter : IFileConverter
{
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromMinutes(3);
    private const int MsoAutomationSecurityForceDisable = 3;
    private const int WdExportFormatPdf = 17;
    private const int PpFixedFormatTypePdf = 2;
    private const int XlTypePdf = 0;

    private readonly IOfficeAvailability _availability;
    private readonly ILogger _logger;
    private readonly OfficeConverterProfile _profile;

    protected OfficeDocumentToPdfConverter(
        IOfficeAvailability availability,
        ILogger logger,
        OfficeConverterProfile profile)
    {
        _availability = availability;
        _logger = logger;
        _profile = profile;
    }

    public string Id => _profile.Id;

    public string DisplayNameKey => _profile.DisplayNameKey;

    public string DescriptionKey => _profile.DescriptionKey;

    public string Glyph => _profile.Glyph;

    public ConverterCategory Category => ConverterCategory.Convert;

    public IReadOnlyList<string> InputExtensions => _profile.InputExtensions;

    public IReadOnlyList<string> OutputExtensions { get; } = [".pdf"];

    public bool SupportsMultipleInputs => false;

    public bool CanConvert(ConversionInput input)
    {
        return input.FilePaths.Count == 1 && ConverterHelpers.AllExtensionsAre(input, _profile.InputExtensions);
    }

    public async Task<ConversionResult> ConvertAsync(
        ConversionRequest request,
        IProgress<ConversionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.Now;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.InputPaths.Count != 1)
            {
                return ConversionResult.Failed(ConversionErrorCode.InvalidInput);
            }

            var inputPath = request.InputPaths[0];
            if (!File.Exists(inputPath))
            {
                return ConversionResult.Failed(ConversionErrorCode.FileNotFound);
            }

            Directory.CreateDirectory(request.OutputDirectory);
            var outputPath = Path.Combine(request.OutputDirectory, request.OutputFileName);
            var preference = ResolvePreference(request);
            var availability = _availability.Detect();
            var engine = OfficeEngineResolver.Resolve(preference, availability, Id);
            if (engine == OfficeEngineKind.None || !IsEngineUsable(engine, availability))
            {
                return ConversionResult.Failed(ConversionErrorCode.OfficeEngineMissing);
            }

            progress?.Report(ConversionProgress.FromPages(0, 1, Id));

            if (engine == OfficeEngineKind.LibreOffice)
            {
                await ConvertWithLibreOfficeAsync(
                    availability.LibreOfficeExecutablePath!,
                    inputPath,
                    outputPath,
                    request.OutputDirectory,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ConvertWithMicrosoftOfficeAsync(inputPath, outputPath, cancellationToken).ConfigureAwait(false);
            }

            if (!File.Exists(outputPath))
            {
                return ConversionResult.Failed(ConversionErrorCode.OutputWriteFailed);
            }

            progress?.Report(ConversionProgress.FromPages(1, 1, Id));
            _logger.LogInformation("Converted {FileName} to PDF.", LogSanitizer.FileNameOnly(inputPath));
            return ConversionResult.Succeeded([outputPath], DateTimeOffset.Now - started);
        }
        catch (OperationCanceledException)
        {
            return ConversionResult.Cancelled();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Office to PDF conversion failed for {FileName}.",
                LogSanitizer.FileNameOnly(request.InputPaths.FirstOrDefault()));
            return ConversionResult.Failed(ConversionErrorCode.FileCorrupt, exception);
        }
    }

    private bool IsEngineUsable(OfficeEngineKind engine, OfficeAvailabilityResult availability)
    {
        if (engine == OfficeEngineKind.LibreOffice)
        {
            return availability.LibreOfficeAvailable && File.Exists(availability.LibreOfficeExecutablePath);
        }

        return _profile.AppKind switch
        {
            OfficeAppKind.Word => availability.WordAvailable,
            OfficeAppKind.Excel => availability.ExcelAvailable,
            OfficeAppKind.PowerPoint => availability.PowerPointAvailable,
            _ => false
        };
    }

    private static OfficeEnginePreference ResolvePreference(ConversionRequest request)
    {
        if (request.Options.TryGetValue(ConversionOptionKeys.OfficeEngine, out var value) &&
            Enum.TryParse(value, ignoreCase: true, out OfficeEnginePreference parsed))
        {
            return parsed;
        }

        return OfficeEnginePreference.Auto;
    }

    private async Task ConvertWithLibreOfficeAsync(
        string sofficePath,
        string inputPath,
        string outputPath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = sofficePath,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--norestore");
        startInfo.ArgumentList.Add("--nolockcheck");
        startInfo.ArgumentList.Add("--nodefault");
        startInfo.ArgumentList.Add("--nofirststartwizard");
        var profileDirectory = Path.Combine(outputDirectory, "lo-profile");
        Directory.CreateDirectory(profileDirectory);
        startInfo.ArgumentList.Add("-env:UserInstallation=" + ToFileUri(profileDirectory));
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add("pdf");
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException("LibreOffice could not be started.");
        }

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(ConversionTimeout);
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "LibreOffice exited with code {0}.", process.ExitCode));
        }

        var produced = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputPath) + ".pdf");
        if (!string.Equals(produced, outputPath, StringComparison.OrdinalIgnoreCase) && File.Exists(produced))
        {
            File.Move(produced, outputPath, overwrite: true);
        }
    }

    private async Task ConvertWithMicrosoftOfficeAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ConversionTimeout);
        await StaRunner.RunAsync(() => ExportWithCom(inputPath, outputPath), timeoutCts.Token).ConfigureAwait(false);
    }

    private void ExportWithCom(string inputPath, string outputPath)
    {
        var applicationType = Type.GetTypeFromProgID(_profile.ProgId);
        if (applicationType is null)
        {
            throw new InvalidOperationException("Microsoft Office is not registered.");
        }

        object? application = null;
        object? document = null;
        try
        {
            application = Activator.CreateInstance(applicationType);
            if (application is null)
            {
                throw new InvalidOperationException("Microsoft Office could not be started.");
            }

            SetProperty(application, "Visible", false);
            SetProperty(application, "DisplayAlerts", 0);
            SetProperty(application, "AutomationSecurity", MsoAutomationSecurityForceDisable);

            document = _profile.AppKind switch
            {
                OfficeAppKind.Word => Invoke(GetProperty(application, "Documents"), "Open", inputPath, false, true, false),
                OfficeAppKind.Excel => Invoke(GetProperty(application, "Workbooks"), "Open", inputPath, 0, true),
                OfficeAppKind.PowerPoint => Invoke(GetProperty(application, "Presentations"), "Open", inputPath, true, false, false),
                _ => throw new InvalidOperationException("Unsupported Office application.")
            };

            switch (_profile.AppKind)
            {
                case OfficeAppKind.Word:
                    Invoke(document, "ExportAsFixedFormat", outputPath, WdExportFormatPdf);
                    Invoke(document, "Close", false);
                    break;
                case OfficeAppKind.Excel:
                    Invoke(document, "ExportAsFixedFormat", XlTypePdf, outputPath);
                    Invoke(document, "Close", false);
                    break;
                case OfficeAppKind.PowerPoint:
                    Invoke(document, "ExportAsFixedFormat", outputPath, PpFixedFormatTypePdf);
                    Invoke(document, "Close");
                    break;
            }
        }
        finally
        {
            TryQuit(application);
            ReleaseCom(document);
            ReleaseCom(application);
        }
    }

    private static object? GetProperty(object target, string name)
    {
        return target.GetType().InvokeMember(name, BindingFlags.GetProperty, null, target, null);
    }

    private static void SetProperty(object target, string name, object value)
    {
        try
        {
            target.GetType().InvokeMember(name, BindingFlags.SetProperty, null, target, [value]);
        }
        catch (Exception)
        {
            // Some Office apps reject a subset of these properties; conversion can still succeed.
        }
    }

    private static object? Invoke(object? target, string name, params object[] arguments)
    {
        if (target is null)
        {
            throw new InvalidOperationException($"Office object was missing for {name}.");
        }

        return target.GetType().InvokeMember(name, BindingFlags.InvokeMethod, null, target, arguments);
    }

    private static void TryQuit(object? application)
    {
        if (application is null)
        {
            return;
        }

        try
        {
            application.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, application, [false]);
        }
        catch (Exception)
        {
            try
            {
                application.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, application, null);
            }
            catch (Exception)
            {
                // Best-effort COM shutdown.
            }
        }
    }

    private static void ReleaseCom(object? comObject)
    {
        if (comObject is null)
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(comObject);
        }
        catch (Exception)
        {
            // Best-effort COM release.
        }
    }

    private static string ToFileUri(string path)
    {
        var fullPath = Path.GetFullPath(path).Replace('\\', '/');
        if (!fullPath.StartsWith('/'))
        {
            fullPath = "/" + fullPath;
        }

        return "file://" + fullPath;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception)
        {
            // Best-effort process stop.
        }
    }
}

internal static class StaRunner
{
    public static Task RunAsync(Action action, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return completion.Task;
    }
}
