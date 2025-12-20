# ?? Quick Reference Card

## ?? Quick Start (5 Minutes)

```bash
# 1. Azure Portal'da 2 resource oluþtur (2 dk)
#    - Translator (Free F0)
#    - Speech (Free F0)
#    Key'leri kopyala

# 2. Python Backend (1 dk)
cd backend
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
python app.py

# 3. Create .env file in project root (30 sec)
# .env.example kopyala ? .env yap
# Key'leri yapýþtýr

# 4. Open Visual Studio
# - Load TurkceRumenceCeviri.sln
# - F5 (Ctrl+Shift+S ile Visual Studio'yu yeniden baþlat)
```

? Hepsi bu kadar!

## ?? Azure Credentials

```
Portal ? Create Resources:
  ? Translator (Copy Key + Region)
  ? Speech (Copy Key + Region)
  ? Language (Copy Key + Endpoint)

Add to App.xaml.cs:
  TranslatorKey = "..."
  SpeechKey = "..."
  LanguageKey = "..."
```

## ?? Key Classes

| Class | Purpose | Location |
|-------|---------|----------|
| MainViewModel | UI Logic | ViewModels/ |
| AzureTranslationService | Translate | Services/Implementations/ |
| AzureSpeechRecognitionService | STT | Services/Implementations/ |
| AzureTextToSpeechService | TTS | Services/Implementations/ |
| PythonBackendService | OCR + AI | Services/Implementations/ |
| SpeechSessionManager | Multi-lang tracking | Services/ |

## ?? Button Functions

| Button | Action | Service |
|--------|--------|---------|
| ?? Baþlat | Listen continuously | AzureSpeechRecognitionService |
| ? Durdur | Stop + Auto-translate | AzureTranslationService |
| ?? Seslendir | Read translation | AzureTextToSpeechService |
| ?? Ekrandan Çevir | OCR + Translate | PythonBackendService |
| ?? Sor | Ask AI | PythonBackendService |

## ?? API Endpoints

```http
Backend URL: http://localhost:5000

POST /api/translate
POST /api/detect-language
POST /api/ocr
POST /api/ask
GET /health
```

## ?? MVVM Flow

```
Button Click
  ?
RelayCommand (ICommand)
  ?
ViewModel Method
  ?
Service Call (Async)
  ?
Update ViewModel Properties
  ?
XAML Binding ? UI Update
```

## ?? Debug Checklist

```
? Python backend running? (localhost:5000/health)
? Azure keys in App.xaml.cs?
? Microphone enabled?
? Internet connection?
? Tesseract installed? (for OCR)
```

## ?? Important Files

```
App.xaml.cs           ? Azure credentials
MainWindow.xaml       ? UI layout
MainViewModel.cs      ? Business logic
Services/Impl/*.cs    ? Azure integrations
backend/app.py        ? Python API
.env                  ? Configuration
```

## ?? Configuration

```csharp
// App.xaml.cs
private const string TranslatorKey = "YOUR_KEY";
private const string TranslatorRegion = "eastus";
private const string SpeechKey = "YOUR_KEY";
private const string SpeechRegion = "eastus";
private const string AnalysisKey = "YOUR_KEY";
private const string AnalysisEndpoint = "https://...";
```

## ?? Data Flow

```
Microphone
  ? (Audio)
Azure Speech Service (STT)
  ? (Text + Language)
MainViewModel
  ? (Send to translation)
AzureTranslationService
  ? (Translated text)
UI TextBox (Display)
  ? (User clicks Seslendir)
AzureTextToSpeechService
  ? (Audio)
Speaker
```

## ?? Commands & Methods

```csharp
// ViewModel Commands
StartListeningCommand      // Button: ?? Baþlat
StopListeningCommand       // Button: ? Durdur
SpeakCommand              // Button: ?? Seslendir
AskAssistantCommand       // Button: ?? Sor
ClearCommand              // Clear all fields

// Service Methods
TranslateAsync()          // Translate text
DetectLanguageAsync()     // Detect language
RecognizeAsync()          // Speech-to-text
SpeakAsync()              // Text-to-speech
ExtractTextAsync()        // OCR
AnswerQuestionAsync()     // AI Q&A
```

## ?? Supported Languages

```
Turkish (tr)    ? Romanian (ro)

Detected automatically:
- Speech recognition
- Text input
- OCR results
```

## ? Performance Tips

1. **Reuse** HttpClient (already singleton)
2. **Use** async/await (already implemented)
3. **Cache** common translations
4. **Optimize** image size for OCR

## ?? Common Issues

| Issue | Solution |
|-------|----------|
| Connection refused | Start Python backend |
| Azure auth failed | Check keys in App.xaml.cs |
| Tesseract not found | Install from GitHub |
| Mic not working | Check Windows settings |
| No translation | Check internet connection |

## ?? Documentation Files

| File | Content |
|------|---------|
| README.md | Overview |
| SETUP_GUIDE.md | Detailed installation |
| GETTING_STARTED.md | Quick start |
| PROJECT_STRUCTURE.md | Architecture |
| PROJECT_SUMMARY.md | Complete summary |

## ?? Build & Run

```bash
# Build
dotnet build

# Run
dotnet run

# Release
dotnet publish -c Release -o ./publish
```

## ?? Dependencies

### C#
```
Azure.AI.TextAnalytics
Microsoft.CognitiveServices.Speech
Newtonsoft.Json
```

### Python
```
Flask
pytesseract
transformers
torch
```

## ?? Secrets Management

```
? DO: Use .env files
? DO: Add .env to .gitignore
? DON'T: Commit credentials
? DON'T: Share keys publicly
```

## ?? Quick Contacts

- **Azure Support**: https://support.microsoft.com
- **Tesseract Issues**: https://github.com/UB-Mannheim/tesseract
- **Flask Docs**: https://flask.palletsprojects.com

---

## ?? Success Checklist

- [ ] Python backend at localhost:5000
- [ ] Azure credentials configured
- [ ] WPF app runs (F5)
- [ ] Microphone test works
- [ ] Translation produces output
- [ ] Voice input recognized
- [ ] OCR endpoint responds
- [ ] AI assistant answers

**All checked? You're ready! ??**

---

*Last updated: 2025-01-23*
*Quick Reference v1.0*
