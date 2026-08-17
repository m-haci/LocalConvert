# LocalConvert

LocalConvert, belgeleri ve görselleri bilgisayardan dışarı göndermeden dönüştürmek için geliştirilmiş çevrimdışı bir Windows masaüstü uygulamasıdır.

> Proje geliştirme aşamasındadır. Mevcut özellikler ve kullanıcı arayüzü değişebilir.

## Özellikler

- Görselleri PDF dosyasına dönüştürme
- PDF dosyalarını birleştirme
- PDF bölme ve sayfa çıkarma
- PDF sayfalarını döndürme ve yeniden sıralama
- PDF metadata işlemleri
- PDF sayfalarını görsele dönüştürme
- Microsoft Office veya LibreOffice üzerinden belge dönüştürmeye uygun altyapı
- Arka planda çalışan dönüşüm kuyruğu
- Türkçe ve İngilizce arayüz kaynakları
- Dosyaları uzak bir sunucuya yüklemeden yerel çalışma

## Teknolojiler

- .NET 8
- C#
- WinUI 3 ve Windows App SDK
- CommunityToolkit.Mvvm
- PDFsharp
- Docnet.Core
- PdfPig
- Serilog
- xUnit

Bağımlılıklar ve lisans tercihleri için [docs/DEPENDENCIES.md](docs/DEPENDENCIES.md) dosyasına bakabilirsiniz.

## Gereksinimler

- Windows 10 sürüm 1809 veya üzeri
- x64 işletim sistemi
- [.NET SDK 8.0.424](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows uygulamasını geliştirmek için Visual Studio 2022 ve ilgili Windows/WinUI iş yükleri

Depodaki `global.json`, SDK sürümünü `8.0.424` olarak sabitler.

## Başlangıç

Depoyu klonlayın:

```powershell
git clone https://github.com/m-haci/LocalConvert.git
Set-Location LocalConvert
```

Bağımlılıkları geri yükleyin:

```powershell
dotnet restore LocalConvert.sln
```

Çözümü derleyin:

```powershell
dotnet build LocalConvert.sln --configuration Release
```

Testleri çalıştırın:

```powershell
dotnet test LocalConvert.sln --configuration Release
```

Uygulamayı çalıştırın:

```powershell
dotnet run --project src/LocalConvert.App/LocalConvert.App.csproj --configuration Debug
```

## Proje yapısı

```text
src/
  LocalConvert.App             WinUI 3 kullanıcı arayüzü
  LocalConvert.Core            Dönüşüm modelleri ve ortak soyutlamalar
  LocalConvert.Images          Görsel → PDF işlemleri
  LocalConvert.Infrastructure  Ayarlar, günlükleme ve dosya sistemi hizmetleri
  LocalConvert.Office          Office ve LibreOffice entegrasyon altyapısı
  LocalConvert.Pdf             PDF dönüştürücüleri ve yardımcı araçlar
  LocalConvert.Worker          Arka plan iş kuyruğu ve yürütücü

tests/
  LocalConvert.Core.Tests
  LocalConvert.Pdf.Tests

docs/
  ARCHITECTURE.md
  DEPENDENCIES.md
```

Ayrıntılı mimari açıklama için [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) dosyasına bakabilirsiniz.

## Gizlilik

LocalConvert yerel kullanım amacıyla tasarlanmıştır. Dönüşüm işlemleri kullanıcının bilgisayarında gerçekleştirilir; belgelerin bir web servisine yüklenmesi gerekmez.

## Katkıda bulunma

Hata bildirimleri ve geliştirme önerileri için GitHub Issues kullanılabilir. Değişiklik göndermeden önce çözümü derleyip ilgili testleri çalıştırmanız önerilir.

## Lisans

Bu proje [MIT Lisansı](LICENSE) altında açık kaynak olarak yayımlanmaktadır.
