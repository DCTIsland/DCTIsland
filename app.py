import re
import os
import json
import asyncio
import requests
import jmespath
import mysql.connector
from typing import Dict
from playwright.async_api import async_playwright
from parsel import Selector
from nested_lookup import nested_lookup
from bs4 import BeautifulSoup
from flask import Flask, render_template, request

app = Flask(__name__)

# Threads 的基礎網址
THREADS_BASE_URL = "https://www.threads.net/@"  # 修改為正確的網址
# 定義合法的 Threads ID 格式：字母、數字、點、底線
THREADS_ID_REGEX = re.compile(r'^[a-zA-Z0-9._]+$')

# MySQL 資料庫設定
DB_CONFIG = {
    'host': os.environ.get('DB_HOST', 'mysql-dctisland-dctisland.e.aivencloud.com'),
    'user': os.environ.get('DB_USER', 'avnadmin'),
    'password': os.environ.get('DB_PASSWORD', 'AVNS__ZRQ9r7irwHzDuVDgp6'),
    'database': os.environ.get('DB_NAME', 'defaultdb'),
    'port': int(os.environ.get('DB_PORT', 12649))
}

# 建立 MySQL 連接
def get_db_connection():
    return mysql.connector.connect(
        host=DB_CONFIG['host'],
        user=DB_CONFIG['user'],
        password=DB_CONFIG['password'],
        database=DB_CONFIG['database'],
        port=DB_CONFIG['port']
    )

# 檢查 URL 是否有效
def is_url_accessible(url):
    try:
        response = requests.get(url, timeout=5)
        if response.status_code == 200:
            soup = BeautifulSoup(response.text, 'html.parser')
            # 檢查是否有 Threads 貼文的特定元素（例如標題）
            if soup.find("meta", {"property": "og:title"}):  # 假設有這樣的標籤
                return True
            # 沒有特徵內容，可能是錯誤頁面
            return False
        return False
    except requests.RequestException:
        return False

def parse_thread(data: Dict) -> str:
    """Parse Threads post JSON dataset for text content only"""
    result = jmespath.search("post.caption.text", data)
    return result

async def scrape_thread_text(url: str) -> dict:
    """爬取 Threads 帖子與回覆內容，只返回文本內容。"""
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
            # 解析 JSON 資料
            data = json.loads(hidden_dataset)
            thread_items = nested_lookup("thread_items", data)
            if not thread_items:
                continue
            # 擷取內容並返回
            threads_text = [parse_thread(t) for thread in thread_items for t in thread]
            return {
                "thread_text": threads_text[0],
                "replies_text": threads_text[1:],
            }
        raise ValueError("無法在頁面中找到 Threads 資料")

# 儲存 URL 和 ID 至 MySQL 資料庫
def save_url_to_mysql(thread_id, url):
    """將 Threads ID 和 URL 儲存到 MySQL 資料庫。"""
    connection = get_db_connection()
    try:
        cursor = connection.cursor()
        query = "INSERT INTO threads (thread_id, link) VALUES (%s, %s)"
        cursor.execute(query, (thread_id, url))
        connection.commit()
        print(f"成功儲存：{thread_id} - {url}")
    finally:
        cursor.close()
        connection.close()


@app.route('/')
def home():
    return render_template('index.html')

# submit 路由處理表單提交
@app.route('/submit', methods=['POST'])
def submit():
    thread_id = request.form.get('thread_id')
    if not thread_id or not THREADS_ID_REGEX.match(thread_id):
        return "無效的 Threads ID, 請確認格式正確！", 400

    full_url = THREADS_BASE_URL + thread_id

    try:
        loop = asyncio.new_event_loop()
        asyncio.set_event_loop(loop)
        thread_data = loop.run_until_complete(scrape_thread_text(full_url))

        # 印出爬取結果到日誌，不存入資料庫
        print(f"主帖內容: {thread_data['thread_text']}")
        print(f"回覆內容: {thread_data['replies_text']}")

        # 儲存 thread_id 和 URL 到 MySQL
        save_url_to_mysql(thread_id, full_url)

    except Exception as e:
        return f"爬取或儲存時發生錯誤：{e}", 500

    return render_template('success.html')

if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)

