# Architecture

LocalConvert is an offline Windows document toolbox. Milestone 1 ships a WinUI 3 unpackaged shell, a plugin-style converter catalog, a background job queue, and three working tools: Image → PDF, PDF merge, and PDF split.

```text
UI (WinUI 3)
  -> ConversionSession / ViewModels
  -> IJobQueue (Channel workers)
  -> IConversionExecutor (in-process)
  -> IConverterCatalog
  -> IFileConverter implementations
```

New converters are added by implementing `IFileConverter` and registering the type in DI. The catalog discovers them without changing existing converters.

`IConversionExecutor` is the isolation seam. Milestone 1 uses `InProcessConversionExecutor`. Office COM / LibreOffice workers in Milestone 3 can move to a separate process without changing the UI or job model.

Office detection lives in `LocalConvert.Office` as `UnavailableOfficeDetector` until Milestone 3.
