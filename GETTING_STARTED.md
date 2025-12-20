# ?? Baþlangýç Rehberi

Bu rehberde, Türkçe-Rumence Çeviri Sistemini 10 dakika içinde çalýþtýrmayý öðreneceksiniz.

## ?? 1. Ön Gereksinimler (5 dakika)

### Yazýlýmlar
- [ ] Visual Studio 2022 v17.8+
- [ ] .NET 8 SDK
- [ ] Python 3.9+
- [ ] Git

### Azure Hesabý
- [ ] Azure Subscription (free trial yeterli)
- [ ] Translator kaynaðý
- [ ] Speech Services kaynaðý
- [ ] Language kaynaðý

## ?? 2. Azure Credentials Alma (2 dakika)

?? **ÖNEMLÝ**: Bu projede **sadece 2 Azure hizmeti** gereklidir! 

- **Azure Translator** - Çeviri yapar (Türkçe ? Rumence)
- **Azure Speech** - Ses tanýma ve okuma

**Dil algýlama için ayrý service'e ihtiyaç YOK** - Translator'ýn kendi detect endpoint'i var! ?

### Adým 1: Translator Oluþtur

1. https://portal.azure.com açýn
2. "+ Kaynak Oluþtur" týklayýn
3. **"Translator"** ara ve oluþturun:
   - Name: `turkish-romanian-translator`
   - Region: `East US`
   - Tier: **`Free F0`** ? (2 milyon karakter/ay)
4. Kaynaða gidip "Keys and Endpoint"ten **Key 1**'i kopyalayýn ? `.env`'de `AZURE_TRANSLATOR_KEY`
5. **Region**'ý not edin ? `.env`'de `AZURE_TRANSLATOR_REGION`

### Adým 2: Speech Services Oluþtur

1. "+ Kaynak Oluþtur" týklayýn
2. **"Speech"** ara ve oluþturun:
   - Name: `speech-service`
   - Region: `East US`
   - Tier: **`Free F0`** ? (5 saat/ay)
3. Kaynaða gidip "Keys and Endpoint"ten **Key 1**'i kopyalayýn ? `.env`'de `AZURE_SPEECH_KEY`
4. **Region**'ý not edin ? `.env`'de `AZURE_SPEECH_REGION`

? Hepsi bu kadar! Language Service'e ihtiyaç YOK!

## ?? 3. Python Backend Setup (2 dakika)

```bash
# Terminal aç
cd backend

# Virtual environment
python -m venv venv
venv\Scripts\activate

# Paketler
pip install -r requirements.txt

# Çalýþtýr
python app.py
```

? `Running on http://localhost:5000` mesajýný görmelisiniz.

## ?? 4. C# Uygulamasýný Çalýþtýr

1. Visual Studio'da projeyi aç
2. `TurkceRumenceCeviri.sln` dosyasýný açýn
3. **`.env` dosyasýný proje root'unda oluþtur** (GETTING_STARTED.md'deki Key'leri yapýþtýr)
4. Visual Studio'yu **yeniden baþlat** (environment variables'ý yüklemesi için)
5. F5 tuþuna basýn

**Not**: `.env` dosyasý `.gitignore`'a ekli olduðu için GIT'e gitmez (güvenli) ?

## ? 5. Ýlk Kullaným

### Türkçe Konuþmasýný Test Et

1. **Baþlat** butonuna týklayýn
2. Konuþun: "Merhaba dünya"
3. **Durdur** butonuna týklayýn
4. Çeviri otomatik yapýlmalý (Rumence: "Salut lume")
5. **Seslendir** ile dinleyin

### Ekrandan Çevir Test Et

1. Internet sayfasýnda Rumence metin seçin
2. **Ekrandan Çevir** butonuna týklayýn
3. Metin otomatik tanýnmalý ve çevrilmeli

### AI Asistan Test Et

1. Türkçe metin girin
2. "Metni özetle" butonuna týklayýn
3. **Sor** butonuna týklayýn
4. Cevap görünmelidir

## ?? Hata Yönetimi

### Python Backend Baðlantýsý Baþarýsýz
```bash
# Windows PowerShell'de kontrol et
Invoke-WebRequest http://localhost:5000/health

# Baþarýlý olmalý: {"status": "healthy"}
```

### Tesseract Bulunamadý (OCR Hatasý)
```bash
# Windows: Tesseract yükle
# https://github.com/UB-Mannheim/tesseract/wiki

# Linux:
sudo apt-get install tesseract-ocr
```

### Azure Key Hatasý
```bash
# Credentials kontrol et
echo %AZURE_TRANSLATOR_KEY%

# Yoksa, ortam deðiþkenlerini yeniden yükle
# Sistem ? Ortam Deðiþkenleri ? Yeni Oturum Aç
```

## ?? Sonraki Adýmlar

- [ ] [Setup Guide](./SETUP_GUIDE.md) - Detaylý kurulum
- [ ] [README](./README.md) - Tam dokumentasyon
- [ ] API Endpoints - `backend/app.py` dosyasýna bakýn
- [ ] Kod yapýsý - `TurkceRumenceCeviri/` klasörünü inceleyin

## ?? Ýpuçlarý

1. **Console Output Kontrol Et**
   - Visual Studio Output penceresine bakýn
   - Python terminal'inde hatalarý gözlemleyin

2. **Mikrofon Testi**
   - Windows Ses Ayarlarý ? Mikrofon test et
   - Þehir gürültüsü en aza indirin

3. **Cache Temizle**
   - Visual Studio: Build ? Clean Solution
   - Python: `pip cache purge`

4. **Güncellemeler**
   - Azure SDK'larýný güncelle: `pip install --upgrade azure-*`
   - NuGet paketlerini güncelle: Visual Studio NuGet Manager

## ?? Destek

Sorun varsa kontrol listesi:

- [ ] Azure credentials .env dosyasýnda var mý?
- [ ] Python 3.9+ kurulu mu?
- [ ] .NET 8 SDK kurulu mu?
- [ ] Ýnternet baðlantýsý var mý?
- [ ] Mikrofon etkin mi?
- [ ] Python backend çalýþýyor mu?

---

**Tebrikler! Sistem artýk hazýr.** ??

Soru ve öneriler için GitHub Issues'e bakýn.
