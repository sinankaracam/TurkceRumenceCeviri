# ?? Proje Yapýsý

```
TurkceRumenceCeviri/
??? TurkceRumenceCeviri/                    # Ana C# Projesi (.NET 8)
?   ??? Services/
?   ?   ??? ITranslationService.cs          # ? Çeviri servisi arayüzü
?   ?   ??? ISpeechRecognitionService.cs    # ? Ses tanýma arayüzü
?   ?   ??? ITextToSpeechService.cs         # ? Metin-to-ses arayüzü
?   ?   ??? IOcrService.cs                  # ? OCR arayüzü
?   ?   ??? IAIAssistantService.cs          # ? AI asistan arayüzü
?   ?   ??? SpeechSessionManager.cs         # ? Oturum yöneticisi
?   ?   ??? Implementations/
?   ?       ??? AzureTranslationService.cs   # ? Azure Translator
?   ?       ??? AzureSpeechRecognitionService.cs # ? Azure STT
?   ?       ??? AzureTextToSpeechService.cs  # ? Azure TTS
?   ?       ??? PythonBackendService.cs      # ? Python API client
?   ?
?   ??? ViewModels/
?   ?   ??? MainViewModel.cs                 # ? Ana ViewModel (MVVM)
?   ?   ??? RelayCommand.cs                  # ? ICommand implementasyonu
?   ?
?   ??? Configuration/
?   ?   ??? AzureConfig.cs                   # ? Konfigürasyon yöneticisi
?   ?
?   ??? Utilities/
?   ?   ??? DebugHelper.cs                   # ? Debug yardýmcýlarý
?   ?
?   ??? MainWindow.xaml                      # ? UI (WPF)
?   ??? MainWindow.xaml.cs                   # ? Code-behind
?   ??? App.xaml                             # ? App kaynaklarý
?   ??? App.xaml.cs                          # ? Dependency Injection
?   ??? TurkceRumenceCeviri.csproj           # ? Project file
?
??? backend/                                 # Python Flask Backend
?   ??? app.py                               # ? Flask uygulamasý
?   ??? requirements.txt                     # ? Python dependencies
?   ??? README.md                            # ? Backend setup
?   ??? venv/                                # Virtual environment (gitignore)
?
??? README.md                                # ? Proje açýklamasý
??? SETUP_GUIDE.md                          # ? Detaylý kurulum
??? GETTING_STARTED.md                      # ? Hýzlý baþlangýç
??? PROJECT_STRUCTURE.md                    # ? Bu dosya
??? .env.example                             # ? Ortam deðiþkenleri örneði
??? .gitignore                               # ? Git ignore kurallarý
```

## ?? Dependency Aðacý

```
TurkceRumenceCeviri (.NET 8 WPF)
??? Azure.AI.TextAnalytics
?   ??? Dil algýlama
??? Microsoft.CognitiveServices.Speech
?   ??? Speech-to-Text (STT)
?   ??? Text-to-Speech (TTS)
??? Newtonsoft.Json
?   ??? JSON serialization
??? System.Net.Http
    ??? REST API çaðrýlarý

Python Backend (Flask)
??? Flask
?   ??? Web framework
??? pytesseract
?   ??? OCR (Tesseract wrapper)
??? Pillow
?   ??? Image processing
??? transformers
?   ??? Hugging Face models
??? torch
?   ??? PyTorch
??? azure-cognitiveservices-speech
    ??? Azure SDK
```

## ?? Veri Akýþý

```
Kullanýcý
  ?
  ??? ?? Baþlat (Mikrofon)
  ?   ??? Azure Speech Service (STT)
  ?       ??? AzureSpeechRecognitionService
  ?           ??? MainViewModel.TurkishText/RomanianText
  ?
  ??? ?? Ekrandan Çevir (Seçili Yazý)
  ?   ??? PythonBackendService.OCR
  ?       ??? Tesseract OCR
  ?           ??? Dil Algýlama (Azure)
  ?               ??? Çeviri (Azure Translator)
  ?
  ??? ?? Seslendir
  ?   ??? Azure Text-to-Speech
  ?       ??? AzureTextToSpeechService
  ?           ??? Hoparlör
  ?
  ??? ?? Yapay Zekaya Sor
      ??? PythonBackendService.AskAssistant
          ??? Hugging Face QA Model
              ??? MainViewModel.AssistantResponse
```

## ?? API Endpoints (Python Backend)

```
POST /api/translate
  ?? input: text, source_language, target_language
  ?? output: translated text

POST /api/detect-language
  ?? input: text
  ?? output: language code

POST /api/ocr
  ?? input: image file
  ?? output: extracted text, detected language

POST /api/ask
  ?? input: question, context, language
  ?? output: AI answer
```

## ?? Key Classes

| Sýnýf | Amaç | Durum |
|-------|------|-------|
| `MainViewModel` | UI logic | ? Tamamlandý |
| `AzureTranslationService` | Çeviri motor | ? Tamamlandý |
| `AzureSpeechRecognitionService` | Ses tanýma | ? Tamamlandý |
| `AzureTextToSpeechService` | Metin okuma | ? Tamamlandý |
| `PythonBackendService` | OCR + AI | ? Tamamlandý |
| `SpeechSessionManager` | Oturum yönetimi | ? Tamamlandý |

## ?? Mimarinin Özellikleri

### Güçlü Yönler ?
- MVVM Pattern - Clean Architecture
- Dependency Injection - Esneklik
- Async/Await - Non-blocking UI
- Interface-based - Kolay test etme
- Separation of Concerns - Modüler yapý

### Geliþtirilebilir Alanlar ??
- Unit Tests ekleme
- Logging framework (Serilog)
- Caching layer
- Database persistence
- Docker containerization

## ?? Güvenlik Notlarý

- ? Credentials'ý **GIT'e commit etmeyin**
- ? `.env.example` dosyasýný template olarak kullanýn
- ? `.env` dosyasýný `.gitignore`'a ekleyin
- ? Azure Key Vault kullanmayý düþünün (production)

## ?? Performance Tips

1. **Caching**: Sýk çevirilen terimleri cache'leyin
2. **Async**: Long-running operations async yapýn ? (yapýldý)
3. **Connection Pooling**: HttpClient singleton ? (yapýldý)
4. **Batch Operations**: Birden çok metin için batch çeviri

## ?? Test Strategy

```
MainViewModel
??? Unit Tests (Xunit/NUnit)
?   ??? Translation logic
?   ??? Language detection
?   ??? Command execution
?
??? Integration Tests
    ??? Azure services
    ??? Python backend
```

## ?? Referans Mimarisi

Bu proje aþaðýdaki best practices'i takip eder:

- **MVVM Pattern** (WPF)
- **Dependency Injection** (Constructor)
- **Repository Pattern** (Services)
- **Async/Await** (Task-based)
- **Separation of Concerns**

---

**Son Güncelleme:** 2025-01-23
