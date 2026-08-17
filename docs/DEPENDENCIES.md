# Dependencies and licenses

Milestone 1 only uses packages that are safe for commercial closed-source distribution without copyleft or revenue-threshold licenses.

| Package | License | Why |
|---|---|---|
| PDFsharp-GDI 6.2.4 | MIT | Image → PDF, merge, split on Windows |
| CommunityToolkit.Mvvm | MIT | ViewModels, commands |
| Microsoft.Extensions.* | MIT | DI, logging abstractions |
| Microsoft.WindowsAppSDK | MIT-style Microsoft license | WinUI 3 |
| Serilog / Serilog.Sinks.File | Apache-2.0 | File logging without document content |
| xUnit / FluentAssertions 7 / NSubstitute | permissive | Tests |

Explicitly not used:

| Package | Reason |
|---|---|
| iText | AGPL |
| Ghostscript | AGPL |
| MuPDF | AGPL |
| QuestPDF | Community license has a revenue threshold |
| SixLabors.ImageSharp | Split license with a revenue threshold |
| Aspose / IronPDF / Syncfusion / Spire | Paid commercial SDKs |

Reserved for later milestones (permissive):

- Docnet.Core (MIT) + PDFium (Apache-2.0) for PDF → image
- PdfPig (Apache-2.0) for metadata
- Magick.NET (Apache-2.0) for HEIC/WEBP/TIFF/AVIF. Do not enable Ghostscript delegates.
