# ?? Türkçe - Rumence Çeviri Sistemi

Gerçek zamanlý ses çevirisi, OCR, ve yapay zeka destekli sorgu-cevap sistemi.

## ? Özellikler

- ?? **Gerçek Zamanlý Ses Tanýma** - Türkçe/Rumence otomatik algýlama
- ?? **Metin-to-Ses** - Çeviri sonuçlarýný seslendirme  
- ?? **OCR Çevirisi** - Ekrandan seçili metni çevirme
- ?? **AI Asistan** - Baðlam bazlý soru-cevap sistemi
- ?? **Azure Cognitive Services** - Profesyonel çeviri ve dil iþleme
- ?? **Çoklu Dil Algýlama** - Otomatik dil tespiti

## ??? Mimari

```
????????????????????????
?   WPF UI (.NET 8)    ? ? Windows Desktop Arayüz
?   C# / MVVM Pattern  ?
????????????????????????
           ? HTTP/REST
           ?
????????????????????????
? Python Flask API     ? ? OCR, AI Backend
? Tesseract, HF Models ?
????????????????????????
           ?
           ? REST API
           ?
????????????????????????
? Azure Services       ? ? Translator, Speech, Text Analytics
? Microsoft Cloud      ?
????????????????????????
```

## ?? Hýzlý Baþlangýç

### Windows Kullanýcýlarý

1. **Gereksinimleri Yükle**
   ```bash
   # .NET 8 SDK
   # Visual Studio 2022
   ```

2. **Projeyi Aç**
   ```bash
   git clone <repo-url>
   cd TurkceRumenceCeviri
   ```

3. **Azure Credentials Ayarla**
   - `.env.example` dosyasýný kopyalayýn ? `.env`
   - Azure Key ve Region'larý doldurun

4. **Python Backend'i Baþlat**
   ```bash
   cd backend
   python -m venv venv
   venv\Scripts\activate
   pip install -r requirements.txt
   python app.py
   ```

5. **WPF Uygulamasýný Çalýþtýr**
   - Visual Studio'da F5 tuþuna basýn

## ?? Komut Referansý

### Backend Python API

```bash
# Development
cd backend
pip install -r requirements.txt
python app.py

# Production
gunicorn -w 4 -b 0.0.0.0:5000 app:app
```

### WPF Uygulamasý

```bash
# Build
dotnet build

# Run
dotnet run

# Release
dotnet publish -c Release
```

## ?? Konfigürasyon

### Ortam Deðiþkenleri

```bash
# Windows PowerShell
$env:AZURE_TRANSLATOR_KEY = "your-key"
$env:PYTHON_API_URL = "http://localhost:5000"

# Windows CMD
setx AZURE_TRANSLATOR_KEY "your-key"

# Linux/macOS
export AZURE_TRANSLATOR_KEY="your-key"
```

### Azure Setup

1. [Azure Portal](https://portal.azure.com) açýn
2. Aþaðýdaki 2 kaynaðý oluþturun:
   - **Translator** (Free F0) ? Key ve Region'ý alýn
   - **Speech** (Free F0) ? Key ve Region'ý alýn
3. Deðerleri `.env` dosyasýna yapýþtýrýn

**Maliyet:** ? Tamamen BEDAVA (Free tier ile)

[Detaylý maliyet analizi](./COST_OPTIMIZATION.md)

## ?? API Endpoints

### Translation
```http
POST /api/translate
Content-Type: application/json

{
  "text": "Merhaba dünya",
  "source_language": "tr",
  "target_language": "ro"
}
```

### Language Detection
```http
POST /api/detect-language
Content-Type: application/json

{
  "text": "Bonjour le monde"
}
```

### OCR
```http
POST /api/ocr
Content-Type: multipart/form-data

image: <image-file>
```

### Ask AI
```http
POST /api/ask
Content-Type: application/json

{
  "question": "Metni özetle",
  "context": "Soru sorulacak metin...",
  "language": "tr"
}
```

## ?? Kullaným Örneði

1. **Baþlat** butonuna týklayýn
2. Türkçe konuþun (örn: "Merhaba")
3. **Durdur** butonuna týklayýn
4. Otomatik çeviri yapýlýr (Rumence çýkýþ)
5. **Seslendir** ile Rumence versiyonu dinleyin
6. AI Asistan'a soru sorun

## ?? Sorun Giderme

| Hata | Çözüm |
|------|-------|
| Connection refused | Python backend çalýþýyor mu? `python app.py` |
| Azure auth hatasý | Key/Region doðru mu? |
| Tesseract bulunamadý | Tesseract OCR yüklü mü? |
| Mikrofon hatasý | Windows Sound settings kontrol edin |

## ?? Dependencies

### C# (.NET 8)
- Azure.AI.TextAnalytics ~~(kaldýrýldý - Language Service'e ihtiyaç yok!)~~
- Microsoft.CognitiveServices.Speech
- Newtonsoft.Json

**Not:** Artýk sadece **2 Azure hizmetine** ihtiyaç:
- Translator (Çeviri + Dil Algýlama)
- Speech (STT/TTS)

### Python 3.9+
- Flask
- pytesseract
- transformers
- torch
- pillow

## ?? Lisans

MIT License - Özgürce kullanabilirsiniz

## ????? Geliþtirici

SMKRCM Team - 2025

## ?? Faydalý Linkler

- [Microsoft Learn - Azure Translator](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/)
- [Azure Speech Service](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/)
- [Python Tesseract](https://github.com/madmaze/pytesseract)
- [HuggingFace Transformers](https://huggingface.co/docs/transformers)

---

**Kurulum Kýlavuzu**: [SETUP_GUIDE.md](./SETUP_GUIDE.md) dosyasýna bakýn.
