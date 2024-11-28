import re
import os
import requests
import mysql.connector
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

# 儲存 URL 和 ID 至 MySQL 資料庫
def save_url_to_mysql(threads_id, url, replies_text=''):
    connection = None
    cursor = None
    try:
        # 建立資料庫連線
        connection = get_db_connection()
        cursor = connection.cursor()

        # 修改 SQL 插入語句，加入 replies_text 欄位
        query = "INSERT INTO threads (thread_id, link, replies_text) VALUES (%s, %s, %s)"
        cursor.execute(query, (threads_id, url, replies_text))
        
        # 提交交易
        connection.commit()

        print(f"URL 和 ID 已成功寫入資料庫: {threads_id}, {url}, {replies_text}")
    except mysql.connector.Error as err:
        print(f"資料庫錯誤: {err}")
    finally:
        if cursor is not None:
            cursor.close()
        if connection is not None and connection.is_connected():
            connection.close()


@app.route('/')
def home():
    return render_template('index.html')

# submit 路由處理表單提交
@app.route('/submit', methods=['POST'])
def submit():
    thread_id = request.form.get('thread_id')
    print(f"收到的 thread_id: {thread_id}")  # 日誌輸出
    if not thread_id:  # 暫時去掉正則驗證
        return "請輸入 Threads ID！", 400

    full_url = THREADS_BASE_URL + thread_id
    print(f"生成的 URL: {full_url}")  # 顯示生成的 URL

    # 假設 URL 驗證成功
    return render_template('success.html')


if __name__ == '__main__':
    port = int(os.environ.get('PORT', 5000))
    app.run(host='0.0.0.0', port=port)

