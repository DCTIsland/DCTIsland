import re
import os
import json
import openai
import asyncio
import requests
import jmespath
import random
import base64
import firebase_admin
import pytz
from dotenv import load_dotenv
from firebase_admin import credentials, initialize_app, storage, db
from typing import Dict
from playwright.async_api import async_playwright
from parsel import Selector
from nested_lookup import nested_lookup
from bs4 import BeautifulSoup
from flask import Flask, render_template, request, redirect, url_for, Response, abort, jsonify
from datetime import datetime
from flask import session

app = Flask(__name__)

# Threads 的基礎網址
THREADS_BASE_URL = "https://www.threads.net/@"
THREADS_ID_REGEX = re.compile(r'^[a-z0-9._]+$')

# 加載 .env 文件中的環境變數
load_dotenv()

# OpenAI API 設定
openai.api_key = os.environ.get('OPENAI_API_KEY')

# Firebase 初始化
firebase_service_account = os.environ.get('FIREBASE_SERVICE_ACCOUNT')
if not firebase_service_account:
    raise ValueError("FIREBASE_SERVICE_ACCOUNT 環境變數未設置，請在 Zeabur 上設定此環境變數")

service_account_info = json.loads(firebase_service_account)
cred = credentials.Certificate(service_account_info)
firebase_database_url = os.environ.get('FIREBASE_DATABASE_URL')
firebase_storage_bucket = os.environ.get('FIREBASE_STORAGE_BUCKET')

if not firebase_admin._apps:
    initialize_app(cred, {
        'databaseURL': firebase_database_url,
        'storageBucket': firebase_storage_bucket
    })

# 檢查資料是否已存在於 Firebase 中
def is_thread_in_firebase(thread_id):
    try:
        ref = db.reference('threads')
        all_threads = ref.get()
        for key, value in all_threads.items():
            if value.get("thread_id") == thread_id:
                return value  # 如果找到匹配的 thread_id，返回其內容
        return None
    except Exception as e:
        print(f"檢查 Firebase 資料失敗: {e}")
        return None

# 檢查 URL 是否有效
def is_url_accessible(url):
    try:
        response = requests.get(url, timeout=5)
        response.raise_for_status()
        soup = BeautifulSoup(response.text, 'html.parser')
        return bool(soup.find("meta", {"property": "og:title"}))
    except requests.RequestException:
        return False
        
def parse_thread(data: Dict) -> str:
    result = jmespath.search("post.caption.text", data)
    return result

def clean_and_truncate_text(text, max_length=600):
    # 去掉多餘的換行和空格
    cleaned_text = re.sub(r'\s+', ' ', text).strip()
    # 截斷到指定長度
    return cleaned_text[:max_length]

async def scrape_thread_text(url: str) -> dict:
    async with async_playwright() as pw:
        browser = await pw.chromium.launch()
        context = await browser.new_context(viewport={"width": 1920, "height": 1080})
        page = await context.new_page()
        await page.goto(url)
        await page.wait_for_selector("[data-pressable-container=true]", timeout=5000)
        selector = Selector(await page.content())
        hidden_datasets = selector.css('script[type="application/json"][data-sjs]::text').getall()

        for hidden_dataset in hidden_datasets:
            if '"ScheduledServerJS"' not in hidden_dataset or "thread_items" not in hidden_dataset:
                continue
            data = json.loads(hidden_dataset)
            thread_items = nested_lookup("thread_items", data)
            if not thread_items:
                continue
            threads_text = []
            for thread in thread_items:
                for t in thread:
                    parsed_text = parse_thread(t)
                    if parsed_text and parsed_text.strip():  # 避免加入None或空字串
                        threads_text.append(parsed_text)

            if not threads_text:
                continue  # 如果全部都是空資料，跳過這批資料
            # 合併文字並清理處理
            combined_text = ' '.join(threads_text)
            cleaned_text = clean_and_truncate_text(combined_text)

            # 處理回覆文字
            print(f"清理後的爬蟲結果: {cleaned_text}")
            replies_text = [clean_and_truncate_text(reply) for reply in threads_text[1:]]
            
            return {
                "thread_text": cleaned_text,
                "replies_text": replies_text,
            }

        raise ValueError("無法在頁面中找到 Threads 資料")

# 用 GPT-3.5-turbo 分析頁面內容並提取三個主題
def analyze_with_gpt(text):
    try:
        response = openai.ChatCompletion.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are an advanced text analysis assistant."},
                {"role": "user", "content": f"""
                 Analyze the following text and return with three keys: 
                 - "keywords": A list of three themes derived from the text as specific items. All items must be in English.
                 - "Emotion": The most frequent emotion in the text, You must choose only one from the following five options: "Sadness", "Fear", "Happiness", "Anger", "Disgust". Do not include any emotion outside this list. However, if "Happiness" seems to apply but has already been selected too frequently in prior responses, choose another reasonable alternative from the remaining options.
                 - JSON Format: {{"keywords": ["", "", ""], "emotion": ""}}
                 Here is the text:{text}
                """}
            ],
            max_tokens=1500  # 減少 token 消耗，只需要提取關鍵主題
        )
        # 提取 GPT 的返回內容
        content = response['choices'][0]['message']['content'].strip()
        # print(f"GPT 返回內容: {content}")  # 調試用

        # 清理內容，去掉非 JSON 區塊或多餘字符
        cleaned_content = re.sub(r'^```json|```$', '', content).strip()
        print(f"清理後內容: {cleaned_content}")  # 調試用

        # 嘗試解析為 JSON 格式
        result = json.loads(cleaned_content)

        # 標準化鍵名為小寫
        result = {key.lower(): value for key, value in result.items()}

        # 驗證返回的結構
        keywords = result.get("keywords", [])
        emotion = result.get("emotion", "")

        # 確保 keywords 是三個英文字詞
        if not isinstance(keywords, list) or len(keywords) != 3 or not all(isinstance(word, str) for word in keywords):
            raise ValueError("Keywords 必須是三個英文字詞")

        # 確保情緒在指定範圍內
        valid_emotions = ["Happiness", "Fear", "Sadness", "Anger", "Disgust"]
        if emotion not in valid_emotions:
            emotion = random.choice(valid_emotions)

        return {"keywords": keywords, "emotion": emotion}

    except json.JSONDecodeError as e:
        print(f"JSON 解析錯誤: {e}")
        raise ValueError("GPT 返回的內容無法解析為 JSON 格式")
    except ValueError as e:
        print(f"GPT 返回結構不正確: {e}")
        raise

# Stability AI 生成圖片
def generate_texture_image(threads_id, url, topics, emotion):
    if not topics or len(topics) < 1:
        raise ValueError("關鍵字列表為空，無法生成圖片")

    selected_keyword = topics[0]
    print(f"選擇的關鍵字: {selected_keyword}")

    STABILITY_API_KEY = os.getenv("STABILITY_API_KEY")
    if not STABILITY_API_KEY:
        raise ValueError("STABILITY_API_KEY 環境變數未設定，請確認 API Key")

    engine_id = "stable-diffusion-xl-1024-v1-0"
    api_host = os.getenv("API_HOST", "https://api.stability.ai")
    api_url = f"{api_host}/v1/generation/{engine_id}/text-to-image"

    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json",
        "Authorization": f"Bearer {STABILITY_API_KEY}"
    }

    prompt = (
        f"A seamless, tileable, repeating pattern of {selected_keyword}, "
        "minimalist, simple shapes, vibrant colors, bold outlines, "
        "flat design, clean lines, high contrast, adorable, PBR material, "
        "loopable background, vector-like, 4K resolution"
    )

    payload = {
        "text_prompts": [{"text": prompt}],
        "cfg_scale": 7,
        "height": 1024,
        "width": 1024,
        "samples": 1,
        "steps": 30
    }

    try:
        response = requests.post(api_url, headers=headers, json=payload, timeout=20)

        if response.status_code != 200:
            print(f"API 回應錯誤: {response.text}")
            return None

        data = response.json()

        if "artifacts" not in data or not data["artifacts"]:
            print("API 回應未包含圖片數據")
            return None

        # 解析 API 回應的圖片
        image_data = base64.b64decode(data["artifacts"][0]["base64"])
        # 上傳圖片到 Firebase Storage
        img_url = upload_to_firebase_storage(image_data, threads_id)
        
        if img_url:
            print(f"Firebase Storage 圖片 URL: {img_url}")
            # 在這裡統一寫入 Firebase
            save_url_to_firebase(threads_id, url, img_url, topics, emotion)
            return img_url
        else:
            print("圖片上傳 Firebase Storage 失敗！")
            return None

    except requests.exceptions.RequestException as e:
        print(f"Stability AI API 請求失敗: {e}")
        return None
    except Exception as e:
        print(f"圖片生成過程中發生錯誤: {e}")
        return None
    
# 上傳圖片到 Firebase Storage
def upload_to_firebase_storage(image_data, threads_id):
    try:
        bucket = storage.bucket()
        blob = bucket.blob(f"textures/{threads_id}.png")
        blob.upload_from_string(image_data, content_type="image/png")
        blob.make_public()  # 確保圖片可公開存取
        return blob.public_url
    except Exception as e:
        print(f"上傳 Firebase Storage 失敗: {e}")
        return None

# 儲存 Firebase Storage 圖片 URL 至 Firebase Database
def save_url_to_firebase(threads_id, url, img_url, topics, emotion, snapshot_url=""):
    try:
        # 獲取當前台灣時間並格式化
        tz = pytz.timezone('Asia/Taipei')
        current_time = datetime.now(tz)
        formatted_time = current_time.strftime("%Y-%m-%d %H:%M:%S")

        snapshot_url = f"https://firebasestorage.googleapis.com/v0/b/dctdb-8c8ad.firebasestorage.app/o/snapshots%2F{threads_id}.png?alt=media"

        ref = db.reference('threads')
        data = {
            "thread_id": threads_id,
            "link": url,
            "image_url": img_url,
            "snapshot_url": snapshot_url,  # 預設空
            "topic1": topics[0],
            "topic2": topics[1],
            "topic3": topics[2],
            "emotion": emotion,
            "created_at": formatted_time  # 新增時間戳記
        }
        ref.push(data)
        print(f"已更新 Firebase Database: {data}")
    except Exception as e:
        print(f"更新 Firebase Database 失敗: {e}")

@app.route('/')
def home():
    return render_template('index.html')

@app.route('/intro')
def intro_page():
    return render_template('intro.html')

@app.route('/index')
def index_page():
    return render_template('index.html')

@app.route('/mode')
def mode_page():
    return render_template('mode.html')

@app.route('/search')
def search_page():
    return render_template('search.html')

@app.route('/search_submit', methods=['POST'])
def search_submit():
    thread_id = request.form.get('thread_id', '').lower()
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return redirect(url_for('error_page', msg="無效的 ID, 請確認格式正確！")), 400
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        return redirect(url_for('result_page', thread_id=thread_id))
    else:
        return "查無此資料，請先建立島嶼！", 404

@app.route('/input')
def input_page():
    return render_template('input.html')  # 指向修改後的 input.html

@app.route('/submit', methods=['POST'])
async def submit():
    thread_id = request.form.get('thread_id', '').lower()
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return redirect(url_for('error_page', msg="無效的 Threads ID, 請確認格式正確！"))
    
    # 檢查ID是否存在於Threads（用is_url_accessible）
    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return redirect(url_for('error_page', msg=f"生成的 URL 無效或不存在：{full_url}"))
    
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        return redirect(url_for('result_page', thread_id=thread_id))
    
    # 如果所有檢查都通過，才進入 loading 頁面
    return render_template('loading.html', thread_id=thread_id)

@app.route('/loading', methods=['POST'])
def loading():
    thread_id = request.form.get('thread_id', '').lower()
    user_id = request.form.get('user_id', '').lower()
    user_thought = request.form.get('user_thought')
    if user_id:
        return render_template('loading.html', user_id=user_id, user_thought=user_thought)
    elif thread_id and THREADS_ID_REGEX.match(thread_id):
        return render_template('loading.html', thread_id=thread_id)
    else:
        return redirect(url_for('error_page', msg="無效的 ID, 請確認格式正確！"))

@app.route('/process', methods=['GET'])
async def process():
    thread_id = request.args.get('thread_id', '').lower()
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return redirect(url_for('error_page', msg="無效的 Threads ID, 請確認格式正確！"))
    
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        return redirect(url_for('result_page', thread_id=thread_id))
    
    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return redirect(url_for('error_page', msg=f"生成的 URL 無效或不存在：{full_url}", thread_id=thread_id))
    
    try:
        page_data = await scrape_thread_text(full_url)
        if not page_data or not page_data.get('thread_text'):
            return redirect(url_for('error_page', msg="無法取得Threads內容，請確認帳號與貼文狀態！", thread_id=thread_id))
        
        cleaned_text = page_data['thread_text']
        gpt_analysis = analyze_with_gpt(cleaned_text)
        topics = gpt_analysis['keywords']
        emotion = gpt_analysis['emotion']
        texture_path = generate_texture_image(thread_id, full_url, topics, emotion)
        
        if texture_path is None:
            return redirect(url_for('error_page', msg="圖片生成失敗，請稍後再試", thread_id=thread_id))
        
        return redirect(url_for('result_page', thread_id=thread_id))
    except Exception as e:
        print(f"錯誤發生於 process: {e}")
        return redirect(url_for('error_page', msg="處理 Threads 資料時發生錯誤，請確認連線與內容格式是否正確！", thread_id=thread_id))

@app.route('/proxy-image')
def proxy_image():
    url = request.args.get('url')
    if not url:
        abort(400, "Missing image URL")

    try:
        resp = requests.get(url, stream=True)
        if resp.status_code != 200:
            return render_template("error.html", error=f"圖片無法載入，錯誤碼：{resp.status_code}")

        # 回傳原始圖片內容
        return Response(resp.content, content_type=resp.headers['Content-Type'])

    except Exception as e:
        return render_template("error.html", error="圖片無法載入")

@app.route('/custom_input')
def custom_input():
    return render_template('text.html')  # 新增 text.html 讓使用者輸入自創 ID 和想法

@app.route('/custom_submit', methods=['POST'])
def custom_submit():
    user_id = request.form.get('user_id', '').lower()
    user_thought = request.form.get('user_thought')
    if not user_id or not user_thought:
        return redirect(url_for('error_page', msg="請輸入有效的 ID 和想法！"))
    if len(user_thought) > 500:
        return redirect(url_for('error_page', msg="想法內容不可超過 500 字！"))
    if is_thread_in_firebase(user_id):
        return redirect(url_for('result_page', user_id=user_id))
    # 直接進入 loading 頁面
    return render_template('loading.html', user_id=user_id, user_thought=user_thought)

@app.route('/process_custom', methods=['GET'])
async def process_custom():
    user_id = request.args.get('user_id', '').lower()
    user_thought = request.args.get('user_thought', '')
    
    if not user_id or not user_thought:
        return redirect(url_for('error_page', msg="無效的輸入！"))
    
    existing_data = is_thread_in_firebase(user_id)
    if existing_data:
        return redirect(url_for('result_page', user_id=user_id))
    
    try:
        gpt_analysis = analyze_with_gpt(user_thought)
        topics = gpt_analysis['keywords']
        emotion = gpt_analysis['emotion']
        img_url = generate_texture_image(user_id, "Custom Input", topics, emotion)
        
        if img_url is None:
            return redirect(url_for('error_page', msg="圖片生成失敗，請稍後再試"))
        
        # 儲存到 Firebase
        save_url_to_firebase(user_id, "Custom Input", img_url, topics, emotion)
        
        return redirect(url_for('result_page', user_id=user_id))
    except Exception as e:
        print(f"錯誤發生於 process_custom: {e}")
        return redirect(url_for('error_page', msg="處理自訂輸入時發生錯誤，請稍後再試"))

@app.route('/submit_feedback', methods=['POST'])
def submit_feedback():
    try:
        thread_id = request.form.get('thread_id', '').lower()
        feedback = request.form.get('feedback')
        rating = request.form.get('rating')
        
        if not thread_id or not feedback or not rating:
            return {"status": "error", "message": "Missing required fields"}, 400
            
        # Get reference to the feedback node
        ref = db.reference('feedback')
        
        # 獲取當前台灣時間並格式化
        tz = pytz.timezone('Asia/Taipei')
        current_time = datetime.now(tz)
        formatted_time = current_time.strftime("%Y-%m-%d %H:%M:%S")
        
        # Create new feedback entry
        feedback_data = {
            "thread_id": thread_id,
            "feedback": feedback,
            "rating": int(rating),  # 轉換為整數
            "timestamp": formatted_time
        }
        
        # Push the feedback to Firebase
        ref.push(feedback_data)
        
        return {"status": "success", "message": "Feedback submitted successfully"}
        
    except Exception as e:
        print(f"Error submitting feedback: {e}")
        return {"status": "error", "message": "Failed to submit feedback"}, 500

@app.route('/result')
def result_page():
    thread_id = request.args.get('thread_id')
    user_id = request.args.get('user_id')
    
    # 使用 thread_id 或 user_id 來查詢資料
    data = is_thread_in_firebase(thread_id or user_id)
    if not data:
        return redirect(url_for('error_page', msg="查無此資料"))
    
    return render_template(
        'result.html',
        thread_id=data['thread_id'],
        full_url=data['link'],
        img_url=data.get('image_url', ''),
        snapshot_url=data.get('snapshot_url', ''),
        topics=[data['topic1'], data['topic2'], data['topic3']],
        emotion=data['emotion']
    )

@app.route('/error')
def error_page():
    msg = request.args.get('msg', '發生未知錯誤')
    return render_template('error.html', error=msg)

@app.route('/check_status')
def check_status():
    thread_id = request.args.get('thread_id', '').lower()
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return jsonify({
            'status': 'error',
            'message': '無效的 Threads ID, 請確認格式正確！',
            'redirect_url': url_for('error_page', msg="無效的 Threads ID, 請確認格式正確！")
        })
    
    # 檢查ID是否存在於Threads
    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return jsonify({
            'status': 'error',
            'message': f'生成的 URL 無效或不存在：{full_url}',
            'redirect_url': url_for('error_page', msg=f"生成的 URL 無效或不存在：{full_url}")
        })
    
    # 檢查是否已存在於資料庫
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        return jsonify({
            'status': 'duplicate',
            'message': '你已經是島民了，要看看嗎？',
            'redirect_url': url_for('result_page', thread_id=thread_id)
        })
    
    return jsonify({
        'status': 'processing',
        'message': '正在處理中...'
    })

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)
