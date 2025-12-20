import os
from flask import Flask, request, jsonify
from flask_cors import CORS
import cv2
import pytesseract
from PIL import Image
import io
from azure.ai.textanalytics import TextAnalyticsClient
from azure.core.credentials import AzureKeyCredential
import torch
from transformers import pipeline

app = Flask(__name__)
CORS(app)

# Azure Credentials
LANGUAGE_KEY = os.getenv("AZURE_LANGUAGE_KEY", "YOUR_KEY")
LANGUAGE_ENDPOINT = os.getenv("AZURE_LANGUAGE_ENDPOINT", "YOUR_ENDPOINT")

# Initialize Azure Text Analytics
text_analytics_client = TextAnalyticsClient(
    endpoint=LANGUAGE_ENDPOINT,
    credential=AzureKeyCredential(LANGUAGE_KEY)
)

# Initialize HuggingFace Transformers
# Türkçe ? Rumence çevirisi için MarianMT modeli
device = 0 if torch.cuda.is_available() else -1
translator = pipeline(
    "translation_tr_to_ro",
    model="Helsinki-NLP/opus-mt-tr-ro",
    device=device
)

qa_pipeline = pipeline(
    "question-answering",
    model="deepset/roberta-base-squad2",
    device=device
)

@app.route('/api/detect-language', methods=['POST'])
def detect_language():
    """Metinde hangi dil konuþuluyor algýla"""
    try:
        data = request.json
        text = data.get('text', '')
        
        if not text:
            return jsonify({'error': 'Text required'}), 400
        
        # Azure ile dil algýlama
        result = text_analytics_client.detect_language(documents=[text])
        detected_language = result[0].primary_language.iso6391_name
        
        return jsonify({
            'language': detected_language,
            'confidence': result[0].primary_language.score
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/ocr', methods=['POST'])
def ocr():
    """Ekrandan metin çýkart (OCR)"""
    try:
        if 'image' not in request.files:
            return jsonify({'error': 'No image provided'}), 400
        
        file = request.files['image']
        image = Image.open(io.BytesIO(file.read()))
        
        # Türkçe/Rumence OCR
        text = pytesseract.image_to_string(image, lang='tur+ron')
        
        # Algýlanan dili belirle
        lang_result = text_analytics_client.detect_language(documents=[text])
        detected_language = lang_result[0].primary_language.iso6391_name
        
        return jsonify({
            'text': text,
            'detected_language': detected_language
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/translate', methods=['POST'])
def translate():
    """Türkçe ? Rumence çeviri"""
    try:
        data = request.json
        text = data.get('text', '')
        source_lang = data.get('source_language', 'tr')
        target_lang = data.get('target_language', 'ro')
        
        if not text:
            return jsonify({'error': 'Text required'}), 400
        
        # Basit çeviri (MarianMT veya Azure Translator)
        # Burada marianMT kullanýyoruz, isteðe göre Azure API'ye geçilebilir
        result = translator(text)[0]['translation_text']
        
        return jsonify({
            'original': text,
            'translated': result,
            'source_language': source_lang,
            'target_language': target_lang
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/api/ask', methods=['POST'])
def ask_assistant():
    """Yapay Zeka Asistanýna soru sor"""
    try:
        data = request.json
        question = data.get('question', '')
        context = data.get('context', '')
        language = data.get('language', 'tr')
        
        if not question or not context:
            return jsonify({'error': 'Question and context required'}), 400
        
        # QA Pipeline ile cevap üret
        try:
            result = qa_pipeline(
                question=question,
                context=context,
                max_answer_len=100
            )
            answer = result['answer']
        except:
            answer = "Bu sorunun cevabý kontekste göre bulunamadý."
        
        return jsonify({
            'question': question,
            'answer': answer,
            'language': language
        })
    except Exception as e:
        return jsonify({'error': str(e)}), 500

@app.route('/health', methods=['GET'])
def health():
    """Saðlýk kontrolü"""
    return jsonify({'status': 'healthy'})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)
