# Türkçe - Rumence Çeviri Sistemi - Kurulum Kýlavuzu

## ?? Genel Mimari

```
???????????????????????
?   WPF UI (C#)       ?
?  .NET 8 Windows     ?
???????????????????????
           ? HTTP REST
           ?
???????????????????????
? Python Backend API  ?
?  Flask + Azure      ?
???????????????????????
```

---

## ?? C# / WPF Kurulumu

### 1. Ön Gereksinimler
- Visual Studio 2022 (v17.8+)
- .NET 8 SDK
- Windows 10 / Windows 11

### 2. NuGet Paketleri
Proje zaten aþaðýdaki paketleri içerir:
- `Azure.AI.TextAnalytics` - Dil algýlama
- `Microsoft.CognitiveServices.Speech` - STT/TTS
- `Newtonsoft.Json` - JSON iþleme

### 3. Azure Credentials Ayarý
`App.xaml.cs` dosyasýnda aþaðýdaki deðerleri doldurunuz:

```csharp
private const string TranslatorKey = "YOUR_TRANSLATOR_KEY";
private const string TranslatorRegion = "YOUR_TRANSLATOR_REGION";
private const string SpeechKey = "YOUR_SPEECH_KEY";
private const string SpeechRegion = "YOUR_SPEECH_REGION";
private const string AnalysisKey = "YOUR_ANALYSIS_KEY";
private const string AnalysisEndpoint = "YOUR_ANALYSIS_ENDPOINT";
```

**Alternatif: Ortam Deðiþkenleri**
```batch
setx AZURE_TRANSLATOR_KEY "YOUR_KEY"
setx AZURE_TRANSLATOR_REGION "YOUR_REGION"
setx AZURE_SPEECH_KEY "YOUR_KEY"
setx AZURE_SPEECH_REGION "YOUR_REGION"
setx AZURE_LANGUAGE_KEY "YOUR_KEY"
setx AZURE_LANGUAGE_ENDPOINT "YOUR_ENDPOINT"
setx PYTHON_API_URL "http://localhost:5000"
```

### 4. Build ve Çalýþtýrma
```bash
# Visual Studio'dan F5 tuþuna basýn
# veya komut satýrýndan:
dotnet build
dotnet run
```

---

## ?? Python Backend Kurulumu

### 1. Python 3.9+ Kurulumu
https://www.python.org/downloads/

### 2. Virtual Environment
```bash
cd backend
python -m venv venv

# Windows:
venv\Scripts\activate

# Linux/Mac:
source venv/bin/activate
```

### 3. Baðýmlýlýklarý Yükleyin
```bash
pip install -r requirements.txt
```

### 4. Tesseract OCR Kurulumu

**Windows:**
- https://github.com/UB-Mannheim/tesseract/wiki adresinden `.exe` indir
- Kurulum sýrasýnda Ýngilizce, Türkçe, Rumence dilleri seç
- Python'da TESSERACT_PATH'ý ayarla:

```bash
# pytesseract'i config et
pip install pytesseract

# Windows'ta tesseract kurulduysa:
# C:\Program Files\Tesseract-OCR\tesseract.exe
```

**Linux:**
```bash
sudo apt-get install tesseract-ocr
sudo apt-get install tesseract-ocr-tur
sudo apt-get install tesseract-ocr-ron
```

**macOS:**
```bash
brew install tesseract
```

### 5. Ortam Deðiþkenleri (.env)
`backend` klasöründe `.env` dosyasý oluþturun:

```
AZURE_LANGUAGE_KEY=YOUR_AZURE_LANGUAGE_KEY
AZURE_LANGUAGE_ENDPOINT=YOUR_AZURE_LANGUAGE_ENDPOINT
FLASK_ENV=development
FLASK_DEBUG=True
```

### 6. Python Uygulamasýný Baþlatýn
```bash
python app.py
```

Sunucu `http://localhost:5000` adresinde çalýþacaktýr.

### 7. Test
```bash
# Health check
curl http://localhost:5000/health
```

---

## ?? Azure Cognitive Services Setup

### Azure Translator
1. https://portal.azure.com adresine gidin
2. "+ Kaynak Oluþtur" ? "Translator" ara
3. Oluþturun ve Key/Region'ý alýn

### Azure Speech Services
1. "+ Kaynak Oluþtur" ? "Speech" ara
2. Oluþturun ve Key/Region'ý alýn

### Azure Language (Text Analytics)
1. "+ Kaynak Oluþtur" ? "Language" ara
2. Oluþturun ve Key/Endpoint'i alýn

---

## ?? Uygulama Özellikleri

### ?? Baþlat (Start Listening)
- Mikrofondan Türkçe/Rumence konuþmasýný dinler
- Dili otomatik algýlar ve uygun TextBox'a yazý ekler
- Durdur'a basana kadar sürekli dinler

### ?? Durdur (Stop)
- Dinlemeyi durdurur
- Algýlanan dile göre otomatik çeviri yapar
- Seslendir aktifse, seslendir iþlemini durdurur

### ?? Seslendir (Speak)
- Çeviri sonucunu algýlanan dilin tersinde seslendirer
- Örn: TR algýlansa ? RO'yu okur

### ?? Ekrandan Çevir (OCR)
- Ekrandan seçili yazýyý OCR'la çýkarýr
- Dili algýlar ve doðru TextBox'a koyar
- Otomatik çeviri yapýlýr

### ?? Yapay Zekaya Sor (Ask AI)
- Konuþulan metni baðlam olarak kullanýr
- Python backend'deki QA modeline soru sorar
- Cevabý "Asistan Cevabý" bölümüne yazdýrýr

---

## ?? Sorun Giderme

### "Connection refused" hatasý
- Python backend'in çalýþýp çalýþmadýðýný kontrol edin
- `http://localhost:5000/health` test edin

### Azure Authentication Error
- Key ve Region'ýn doðru olup olmadýðýný kontrol edin
- Ortam deðiþkenlerini yeniden yükleyin

### Tesseract hatalarý
- Tesseract'ýn yüklü olup olmadýðýný kontrol edin
- `pytesseract`'ýn PATH'i doðru þekilde ayarlandýðýný kontrol edin

---

## ?? Referanslar

- [Azure Translator Docs](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/)
- [Azure Speech Service](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/)
- [Tesseract OCR](https://github.com/UB-Mannheim/tesseract/wiki)
- [Python Flask](https://flask.palletsprojects.com/)

---

**Son Güncelleme:** 2025
