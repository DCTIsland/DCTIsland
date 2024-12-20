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
from flask import Flask, render_template, request

app = Flask(__name__)

# Threads 的基礎網址
THREADS_BASE_URL = "https://www.threads.net/@"
THREADS_ID_REGEX = re.compile(r'^[a-zA-Z0-9._]+$')

# 加載 .env 文件中的環境變數
load_dotenv()

# OpenAI API 設定
# openai.api_key = os.environ.get('OPEN_API_KEY')

# 加載環境變數
firebase_service_account = os.getenv('FIREBASE_SERVICE_ACCOUNT')
firebase_credentials_path = os.getenv('FIREBASE_SERVICE_ACCOUNT_PATH')

try:
    if firebase_service_account:
        # 從環境變數中讀取憑證
        service_account_info = json.loads(firebase_service_account)
        cred = credentials.Certificate(service_account_info)
    elif firebase_credentials_path:
        # 從文件中讀取憑證
        if not os.path.exists(firebase_credentials_path):
            raise FileNotFoundError(f"Firebase 憑證文件未找到：{firebase_credentials_path}")
        cred = credentials.Certificate(firebase_credentials_path)
    else:
        raise ValueError("Firebase 憑證未設置，請檢查環境變數或文件路徑")

    # 初始化 Firebase
    if not firebase_admin._apps:
        initialize_app(cred, {'databaseURL': os.getenv('FIREBASE_DATABASE_URL')})
    print("Firebase 初始化成功！")

except json.JSONDecodeError as e:
    raise ValueError(f"FIREBASE_SERVICE_ACCOUNT JSON 格式錯誤：{e}")
except Exception as e:
    raise ValueError(f"Firebase 初始化失敗：{e}")

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
def save_url_to_firebase(threads_id, url, topics=''):
    try:
        ref = db.reference('threads')
        data = {
            "thread_id": threads_id,
            "link": url,
            "topics": topics
        }
        ref.push(data)  # 使用 push 方法新增一筆資料
        print(f"已成功儲存至 Firebase: {threads_id}, {url}, {topics}")
    except Exception as e:
        print(f"儲存到 Firebase 失敗: {e}")

def parse_thread(data: Dict) -> str:
    """Parse Threads post JSON dataset for text content only"""
    result = jmespath.search("post.caption.text", data)
    return result

async def scrape_thread_text(url: str) -> dict:
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
            combined_text = ' '.join(threads_text)
            if len(combined_text) > 200:
                combined_text = combined_text[:200]
            
            return {
                "thread_text": combined_text,
                "replies_text": threads_text[1:],
            }
        raise ValueError("無法在頁面中找到 Threads 資料")

# # 用 GPT-3.5-turbo 分析頁面內容並提取三個主題
# def analyze_with_gpt(text):
#     try:
#         response = openai.ChatCompletion.create(
#             model="gpt-3.5-turbo",
#             messages=[
#                 {"role": "system", "content": "You are an assistant that extracts key topics."},
#                 {"role": "user", "content": f"針對文章結果歸納出三個關鍵詞，並想像成相關的具體物件，最終只印出三個英文單詞:\n{text}"}
#             ],
#             max_tokens=400
#         )
#         topics = response['choices'][0]['message']['content'].strip()
#         return topics
#     except Exception as e:
#         print(f"GPT 分析失敗: {e}")
#         return "分析失敗"

@app.route('/')
def home():
    return render_template('index.html')

@app.route('/submit', methods=['POST'])
async def submit():
    thread_id = request.form.get('thread_id')
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return "無效的 Threads ID, 請確認格式正確！", 400

    full_url = THREADS_BASE_URL + thread_id
    if not is_url_accessible(full_url):
        return f"生成的 URL 無效或不存在：{full_url}", 404

    page_title = await scrape_thread_text(full_url)
    # topics = analyze_with_gpt(page_title)
    topics = ""
    scraped_text = page_title['thread_text']
    save_url_to_firebase(thread_id, full_url, topics=topics)

    print(f"儲存的 Threads URL 和主題: {full_url}, {topics}")
    print(f"Scraped text: {scraped_text}")
    return render_template('success.html')

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)

