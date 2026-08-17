using LocalConvert.Core.Office;

namespace LocalConvert.Office;

public sealed class WindowsOfficeDetector : IOfficeAvailability
{
    public OfficeAvailabilityResult Detect()
    {
        var sofficePath = FindLibreOfficeExecutable();
        return new OfficeAvailabilityResult
        {
            WordAvailable = HasProgId("Word.Application"),
            ExcelAvailable = HasProgId("Excel.Application"),
            PowerPointAvailable = HasProgId("PowerPoint.Application"),
            LibreOfficeAvailable = sofficePath is not null,
            LibreOfficeExecutablePath = sofficePath
        };
    }

    private static bool HasProgId(string progId)
    {
        try
        {
            return Type.GetTypeFromProgID(progId) is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string? FindLibreOfficeExecutable()
    {
        var candidates = new List<string>();
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        AddCandidate(candidates, programFiles, "LibreOffice", "program", "soffice.exe");
        AddCandidate(candidates, programFilesX86, "LibreOffice", "program", "soffice.exe");

        foreach (var root in new[] { programFiles, programFilesX86 })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var directory in Directory.EnumerateDirectories(root, "LibreOffice*"))
            {
                AddCandidate(candidates, directory, "program", "soffice.exe");
            }
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void AddCandidate(List<string> candidates, params string[] parts)
    {
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return;
        }

        candidates.Add(Path.Combine(parts));
    }
}
