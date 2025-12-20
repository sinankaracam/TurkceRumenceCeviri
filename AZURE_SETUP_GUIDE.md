# ?? Azure Cognitive Services - Kurulum Rehberi

## ?? Önemli: 3 Farklý Azure Hizmeti

Bu projede **3 AYRI Azure hizmeti** kullanýlýr. Her birinin **kendi Key'i ve Endpoint'i** vardýr.

```
???????????????????????????????????????????????????????????????
?         Azure Cognitive Services (3 Ayrý Hizmet)           ?
???????????????????????????????????????????????????????????????
?                                                             ?
?  1??  TRANSLATOR                                            ?
?     ?? Amaç: Türkçe ? Rumence çeviri                      ?
?     ?? Resource Type: Translator                          ?
?     ?? Key: TRANSLATOR_KEY (32 karakter)                 ?
?     ?? Region: eastus, westus, etc.                      ?
?     ?? Endpoint: https://api.cognitive.microsofttranslator.com/
?                                                             ?
?  2??  SPEECH SERVICES                                       ?
?     ?? Amaç: Ses tanýma (STT) + Metin okuma (TTS)        ?
?     ?? Resource Type: Speech                             ?
?     ?? Key: SPEECH_KEY (32 karakter)                    ?
?     ?? Region: eastus, westus, etc.                      ?
?     ?? Endpoint: https://<region>.tts.speech.microsoft.com
?                                                             ?
?  3??  LANGUAGE (Text Analytics)                            ?
?     ?? Amaç: Dil algýlama (TR mi RO mu?)                ?
?     ?? Resource Type: Language                           ?
?     ?? Key: LANGUAGE_KEY (32 karakter)                  ?
?     ?? Region: eastus, westus, etc.                      ?
?     ?? Endpoint: https://<resource>.cognitiveservices.azure.com/
?                                                             ?
???????????????????????????????????????????????????????????????
```

---

## ?? Step-by-Step Azure Setup

### 1?? Azure Translator Oluþtur

1. **Azure Portal** aç: https://portal.azure.com
2. **"+ Kaynak Oluþtur"** týkla
3. **"Translator"** ara
4. **Oluþtur** týkla:
   - **Name**: `turkish-romanian-translator` (veya istediðin ad)
   - **Region**: `East US` (veya yakýn olan)
   - **Tier**: `Free F0` (test için yeterli) veya `Standard S1` (production)
5. **Create** týkla

? **Keys and Endpoint** sayfasýna git:
   - **Key 1** veya **Key 2**'i kopyala ? `AZURE_TRANSLATOR_KEY`
   - **Region** not et ? `AZURE_TRANSLATOR_REGION`

---

### 2?? Azure Speech Services Oluþtur

1. **"+ Kaynak Oluþtur"** týkla
2. **"Speech"** ara
3. **Oluþtur** týkla:
   - **Name**: `speech-service` (veya istediðin ad)
   - **Region**: `East US`
   - **Tier**: `Free F0` (test için) veya `Standard S0` (production)
4. **Create** týkla

? **Keys and Endpoint** sayfasýna git:
   - **Key 1** veya **Key 2**'i kopyala ? `AZURE_SPEECH_KEY`
   - **Region** not et ? `AZURE_SPEECH_REGION`

---

### 3?? Azure Language (Text Analytics) Oluþtur

1. **"+ Kaynak Oluþtur"** týkla
2. **"Language"** ara (veya "Text Analytics")
3. **Oluþtur** týkla:
   - **Name**: `language-service` (veya istediðin ad)
   - **Region**: `East US`
   - **Tier**: `Free F0` veya `Standard S`
4. **Create** týkla

? **Keys and Endpoint** sayfasýna git:
   - **Key 1** veya **Key 2**'i kopyala ? `AZURE_LANGUAGE_KEY`
   - **Endpoint** kopyala ? `AZURE_LANGUAGE_ENDPOINT`
   - Örnek: `https://my-language-resource.cognitiveservices.azure.com/`

---

## ?? Azure Keys Cheat Sheet

| Hizmet | Key Adý | Nerede Kullanýlýr |
|--------|---------|-------------------|
| **Translator** | `AZURE_TRANSLATOR_KEY` | `AzureTranslationService.cs` |
| **Translator** | `AZURE_TRANSLATOR_REGION` | `AzureTranslationService.cs` |
| **Speech** | `AZURE_SPEECH_KEY` | `AzureSpeechRecognitionService.cs` |
| **Speech** | `AZURE_SPEECH_REGION` | `AzureSpeechRecognitionService.cs` |
| **Language** | `AZURE_LANGUAGE_KEY` | `AzureTranslationService.cs` |
| **Language** | `AZURE_LANGUAGE_ENDPOINT` | `AzureTranslationService.cs` |

---

## ? Key'leri Doðrula

### Her Key'i Test Et

```bash
# PowerShell'de çalýþtýr

# 1. Translator Key'ini test et
$translatorKey = "YOUR_TRANSLATOR_KEY"
$translatorRegion = "eastus"

Invoke-WebRequest `
  -Uri "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=tr&to=ro&text=Merhaba" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$translatorKey; "Ocp-Apim-Subscription-Region"=$translatorRegion} `
  -Method Post
# Baþarýlý olmalý: 200 status code

# 2. Language Key'ini test et
$languageKey = "YOUR_LANGUAGE_KEY"
$languageEndpoint = "https://YOUR_RESOURCE.cognitiveservices.azure.com/"

Invoke-WebRequest `
  -Uri "$languageEndpoint/text/analytics/v3.1/languages" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$languageKey} `
  -Method Post
# Baþarýlý olmalý: 200 status code
```

---

## ?? Key'leri Güvenli Saklama

### ? Doðru Yol (Yapýlacak)

1. **.env dosyasý** oluþtur (gitignore'a ekle):
```
AZURE_TRANSLATOR_KEY=your_key_here
AZURE_TRANSLATOR_REGION=eastus
AZURE_SPEECH_KEY=your_key_here
AZURE_SPEECH_REGION=eastus
AZURE_LANGUAGE_KEY=your_key_here
AZURE_LANGUAGE_ENDPOINT=https://...
```

2. **Environment Variables** kullan:
```csharp
var key = Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
```

3. **Production'da** Azure Key Vault kullan

### ? Yanlýþ Yol (Yapma!)

- ? Key'leri kod içine yazmayýn
- ? GitHub'a push etmeyin
- ? Public repo'larda paylaþmayýn

---

## ?? Sorun Giderme

### Error: "Invalid subscription key"

**Sebep**: Key yanlýþ kopyalanmýþ

**Çözüm**:
1. Azure Portal'a git
2. Kaynaða (resource) git
3. "Keys and Endpoint" ? Key 1'i kopyala
4. Boþluk olmadýðýný kontrol et
5. `.env` dosyasýný güncelle

### Error: "Region not supported"

**Sebep**: Region yanlýþ

**Çözüm**:
- Translator için desteklenen regionlar: https://aka.ms/cognitive-service-regions
- Genelde `eastus`, `westus`, `westeurope` çalýþýr

### Error: "401 Unauthorized"

**Sebep**: Key yok, yanlýþ, veya kaynaðý siledin

**Çözüm**:
1. Kaynaðýn hala var olduðunu kontrol et
2. Key'i yeniden kopyala
3. Yeni kaynaðý oluþtur

---

## ?? Maliyet Hesapla

### Free Tier (Ücretsiz)

| Hizmet | Limit | Yeterli mi? |
|--------|-------|-----------|
| Translator | 2 milyon karakter/ay | ? Test için yeterli |
| Speech | 5 saat/ay STT + 5 saat/ay TTS | ? Test için yeterli |
| Language | 5,000 records/month | ? Test için yeterli |

### Production (Ücretli)

```
Translator: ~15$ / 1 milyon karakter
Speech: ~1$ / saat kullaným
Language: ~1$ / 1000 records
```

**Toplam aylýk (orta kullaným)**: ~50-100$ tahmini

---

## ?? Key'leri Döndürme (Rotation)

Güvenlik için 90 günde bir key'i deðiþtir:

1. Azure Portal'da **Key 2**'yi kopyala
2. `.env` dosyasýný güncelle
3. Test et
4. **Key 1**'i yenile (portal'da "Rotate" tuþu)
5. Tekrar oluþturulan **Key 1**'i kopyala
6. 90 gün sonra **Key 2** için tekrar et

---

## ?? Referans Linkler

- [Azure Translator Docs](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/)
- [Azure Speech Services](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/)
- [Azure Language Service](https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/)
- [Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/)

---

## ? Next Step

Þimdi `.env` dosyasýný oluþtur ve key'leri ekle:

```bash
# Proje root directory'sinde
copy .env.example .env

# .env dosyasýný text editor'da aç ve key'leri doldur
```

Ardýndan `GETTING_STARTED.md` kalan adýmlarý takip et!

---

**Azure Setup Tamamlandý! ??**

Sýradaki adým: [GETTING_STARTED.md](./GETTING_STARTED.md)
