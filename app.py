import re
import os
import json
import openai
import asyncio
import requests
import jmespath
import firebase_admin
from dotenv import load_dotenv
from firebase_admin import credentials, initialize_app, db
from typing import Dict
from playwright.async_api import async_playwright
from parsel import Selector
from nested_lookup import nested_lookup
from bs4 import BeautifulSoup
from flask import Flask, render_template, request, redirect, url_for

app = Flask(__name__)

# Threads 的基礎網址
THREADS_BASE_URL = "https://www.threads.net/@"
THREADS_ID_REGEX = re.compile(r'^[a-zA-Z0-9._]+$')

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

if not firebase_admin._apps:
    initialize_app(cred, {'databaseURL': firebase_database_url})

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

# 儲存 URL 和 ID 至 Firebase
def save_url_to_firebase(threads_id, url, topics, emotion):
    try:
        ref = db.reference('threads')
        data = {
            "thread_id": threads_id,
            "link": url,
            "topic1": topics[0],
            "topic2": topics[1],
            "topic3": topics[2],
            "emotion": emotion
        }
        ref.push(data)  # 使用 push 方法新增一筆資料
        print(f"已成功儲存至 Firebase: {threads_id}, {url}, {data}")
    except Exception as e:
        print(f"儲存到 Firebase 失敗: {e}")
        
def parse_thread(data: Dict) -> str:
    """Parse Threads post JSON dataset for text content only"""
    result = jmespath.search("post.caption.text", data)
    return result

def clean_and_truncate_text(text, max_length=200):
    """
    清理多餘的空格和換行，並截斷文字長度。
    
    :param text: 原始文字
    :param max_length: 最大長度，預設為 500
    :return: 清理後且截斷的文字
    """
    # 去掉多餘的換行和空格
    cleaned_text = re.sub(r'\s+', ' ', text).strip()
    # 截斷到指定長度
    return cleaned_text[:max_length]

async def scrape_thread_text(url: str) -> dict:
    """
    爬取 Threads 頁面並提取文字，包含清理與截斷。
    
    :param url: Threads 的 URL
    :return: 包含 thread_text 和 replies_text 的字典
    """
    async with async_playwright() as pw:
        browser = await pw.chromium.launch()
        context = await browser.new_context(viewport={"width": 1920, "height": 1080})
        page = await context.new_page()
        await page.goto(url)
        await page.wait_for_selector("[data-pressable-container=true]")
        selector = Selector(await page.content())
        hidden_datasets = selector.css('script[type="application/json"][data-sjs]::text').getall()

        for hidden_dataset in hidden_datasets:
            if '"ScheduledServerJS"' not in hidden_dataset or "thread_items" not in hidden_dataset:
                continue
            data = json.loads(hidden_dataset)
            thread_items = nested_lookup("thread_items", data)
            if not thread_items:
                continue
            threads_text = [parse_thread(t) for thread in thread_items for t in thread]

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
    """
    使用 GPT-3.5-turbo 分析文字，提取三個主題
    """
    try:
        response = openai.ChatCompletion.create(
            model="gpt-3.5-turbo",
            messages=[
                {"role": "system", "content": "You are an advanced text analysis assistant."},
                {"role": "user", "content": f"""
                 Analyze the following text and return with three keys: 
                 - "keywords": A list of three themes derived from the text as specific items in English
                 - "Emotion": The most frequent emotion in the text, only limited to: Sadness, Fear, Happiness, Anger, Disgust
                 - JSON Format: {{"keywords": ["", "", ""], "emotion": ""}}
                 Here is the text:{text}
                """}
            ],
            max_tokens=500  # 減少 token 消耗，只需要提取關鍵主題
        )
        # 提取 GPT 的返回內容
        content = response['choices'][0]['message']['content'].strip()
        print(f"GPT 返回內容: {content}")  # 調試用

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
        valid_emotions = {"Happiness", "Fear", "Sadness", "Anger", "Disgust"}
        if emotion not in valid_emotions:
            raise ValueError(f"Emotion 必須在指定範圍內: {valid_emotions}")

        return {"keywords": keywords, "emotion": emotion}

    except json.JSONDecodeError as e:
        print(f"JSON 解析錯誤: {e}")
        raise ValueError("GPT 返回的內容無法解析為 JSON 格式")
    except ValueError as e:
        print(f"GPT 返回結構不正確: {e}")
        raise

@app.route('/')
def home():
    return render_template('index.html')

@app.route('/submit', methods=['POST'])
async def submit():
    thread_id = request.form.get('thread_id')
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return "無效的 Threads ID, 請確認格式正確！", 400

    # 檢查資料是否已存在
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        # 資料已存在，跳轉到結果頁面，並顯示已存在的資料
        return render_template(
            'result.html',
            full_url=existing_data['link'],
            topics=[existing_data['topic1'], existing_data['topic2'], existing_data['topic3']],
            emotion=existing_data['emotion']
        )
    
    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return f"生成的 URL 無效或不存在：{full_url}", 404

    # 跳轉到 loading.html，處理完後轉到 success.html
    try:
        page_data = await scrape_thread_text(full_url)
        cleaned_text = page_data['thread_text']
        gpt_analysis = analyze_with_gpt(cleaned_text)
        topics = gpt_analysis['keywords']
        emotion = gpt_analysis['emotion']
        save_url_to_firebase(thread_id, full_url, topics=topics, emotion=emotion)

        # 返回結果頁面
        return render_template('success.html', full_url=full_url, topics=topics, emotion=emotion)
    except Exception as e:
        return str(e), 500
    
@app.route('/loading', methods=['POST'])
def loading():
    # 從表單中獲取 thread_id
    thread_id = request.form.get('thread_id')
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return "無效的 Threads ID, 請確認格式正確！", 400

    # 渲染 loading.html
    return render_template('loading.html', thread_id=thread_id)

@app.route('/process', methods=['GET'])
async def process():
    thread_id = request.args.get('thread_id')
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return "無效的 Threads ID, 請確認格式正確！", 400

    # 檢查資料是否已存在
    existing_data = is_thread_in_firebase(thread_id)
    if existing_data:
        # 資料已存在，跳轉到結果頁面，並顯示已存在的資料
        return render_template(
            'result.html',
            thread_id=existing_data['thread_id'],
            full_url=existing_data['link'],
            topics=[existing_data['topic1'], existing_data['topic2'], existing_data['topic3']],
            emotion=existing_data['emotion']
        )

    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return f"生成的 URL 無效或不存在：{full_url}", 404

    try:
        # 爬取頁面文字並分析
        page_data = await scrape_thread_text(full_url)
        cleaned_text = page_data['thread_text']
        gpt_analysis = analyze_with_gpt(cleaned_text)
        topics = gpt_analysis['keywords']
        emotion = gpt_analysis['emotion']
        save_url_to_firebase(thread_id, full_url, topics=topics, emotion=emotion)

        # 渲染 success.html
        return render_template('success.html', full_url=full_url, topics=topics, emotion=emotion)
    except Exception as e:
        return f"處理過程中發生錯誤: {str(e)}", 500

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)
