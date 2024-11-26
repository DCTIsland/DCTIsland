import re
import requests
import csv
from bs4 import BeautifulSoup
from flask import Flask, render_template, request

app = Flask(__name__)

# Threads 的基礎網址
THREADS_BASE_URL = "https://www.threads.net/@"  # 修改為正確的網址

# 定義合法的 Threads ID 格式：字母、數字、點、底線
THREADS_ID_REGEX = re.compile(r'^[a-zA-Z0-9._]+$')

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

# 儲存 URL 至 CSV 檔案
def save_url_to_csv(url):
    try:
        with open('urls.csv', mode='a', newline='', encoding='utf-8') as file:
            writer = csv.writer(file)
            writer.writerow([url])  # 寫入 URL 到 CSV
        print(f"URL 已成功寫入 CSV: {url}")
    except Exception as e:
        print(f"無法寫入 CSV 檔案：{e}")

@app.route('/')
def home():
    return render_template('index.html')

@app.route('/submit', methods=['POST'])
def submit():
    threads_id = request.form.get('threads_id')  # 從表單取得使用者輸入的 Threads ID
    if not threads_id or not THREADS_ID_REGEX.match(threads_id):  # 使用正則表達式驗證
        return "無效的 Threads ID, 請確認格式正確！", 400

    # 自動拼接完整的 Threads URL
    full_url = THREADS_BASE_URL + threads_id

    # 檢查 URL 是否存在並有效
    if not is_url_accessible(full_url):
        return f"生成的 URL 無效或不存在：{full_url}", 404

    # 儲存 URL 至 CSV 檔案
    save_url_to_csv(full_url)

    print(f"目前儲存的 Threads URL: {full_url}")  # 僅在伺服器端列印清單
    return render_template('success.html')  # 顯示成功訊息頁面

if __name__ == '__main__':
    app.run(debug=True)
