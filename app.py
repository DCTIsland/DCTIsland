from flask import Flask, render_template, request

app = Flask(__name__)

# 用於存儲使用者提交的 URL
url_list = []

# 主頁路由：顯示表單
@app.route('/')
def home():
    return render_template('index.html')

# 接收使用者提交的 URL
@app.route('/submit', methods=['POST'])
def submit():
    url = request.form.get('url')  # 從表單取得使用者輸入的 URL
    if url:
        url_list.append(url)  # 將 URL 加入串列
    print(f"目前儲存的 URL: {url_list}")  # 僅在伺服器端列印清單
    return render_template('success.html')  # 顯示成功訊息頁面

if __name__ == '__main__':
    app.run(debug=True)
