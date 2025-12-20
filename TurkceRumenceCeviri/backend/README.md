"""
Türkçe - Rumence Çeviri Sistemi
Backend Baþlatma Kýlavuzu
"""

# 1. Python kurulumu (3.9+)
# https://www.python.org/downloads/

# 2. Virtual environment oluþtur
# python -m venv venv

# 3. Virtual environment aktifleþtir
# Windows:
# venv\Scripts\activate
# Linux/Mac:
# source venv/bin/activate

# 4. Gereksinimleri yükle
# pip install -r requirements.txt

# 5. Tesseract OCR kurulumu
# Windows: https://github.com/UB-Mannheim/tesseract/wiki
# Linux: sudo apt-get install tesseract-ocr
# Mac: brew install tesseract

# 6. Ortam deðiþkenlerini ayarla (.env dosyasý oluþtur)
AZURE_LANGUAGE_KEY=YOUR_AZURE_KEY
AZURE_LANGUAGE_ENDPOINT=YOUR_AZURE_ENDPOINT

# 7. Uygulamayý baþlat
# python app.py

# Sunucu http://localhost:5000 adresinde çalýþacaktýr
