// function setup() {
//     // 創建一個畫布，填滿整個視窗
//     createCanvas(windowWidth, windowHeight, WEBGL);
//   }
  
//   function draw() {
//     background(200);
  
//     // 讓方塊旋轉以增加互動性
//     rotateX(frameCount * 0.01);
//     rotateY(frameCount * 0.01);
  
//     // 繪製一個簡單的3D方塊
//     fill(150, 100, 250);
//     box(100);
//   }
  
  // 當視窗大小改變時，調整畫布大小
  function windowResized() {
    resizeCanvas(windowWidth, windowHeight);
  }
  let dino; // 恐龍
  let items = []; // 道具陣列
  let score = 0;
  let gameOver = false;
  let itemSpeed = 2.5; // 初始道具速度稍快
  let speedIncrement = 0.03; // 每次得分後速度增加
  
  function setup() {
      createCanvas(windowWidth, windowHeight); // 建立畫布
      resetGame();
  }
  
  function draw() {
      if (gameOver) {
          background(50);
          fill(255);
          textSize(32);
          textAlign(CENTER, CENTER);
          text('Game Over', width / 2, height / 2 - 20);
          text(`Score: ${score}`, width / 2, height / 2 + 20);
          textSize(20);
          text('Press R or Tap to Restart', width / 2, height / 2 + 60);
          return;
      }
  
      background('#2c2c2c');
  
      // 顯示分數
      fill(255);
      textSize(24);
      textAlign(LEFT, TOP);
      text(`Score: ${score}`, 20, 20);
  
      dino.update();
      dino.show();
  
      // 繪製地板
      fill('#555555');
      noStroke();
      rect(0, height / 2 + 20, width, 10); // 地板寬度10px
  
      // 更新和顯示道具
      for (let i = items.length - 1; i >= 0; i--) {
          items[i].update();
          items[i].show();
  
          // 檢查抓取
          if (items[i].caught(dino)) {
              score++;
              items.splice(i, 1); // 移除被抓到的道具
              itemSpeed += speedIncrement; // 增加道具速度
              continue; // 避免處理已經移除的道具
          }
  
          // 移除屏幕外的道具
          if (items[i].offscreen()) {
              items.splice(i, 1);
          }
      }
  
      // 隨機添加新道具
      if (frameCount % 200 === 0) {
          items.push(new Item());
      }
  }
  
  function touchStarted() {
      if (gameOver) {
          resetGame();
      } else {
          dino.jump();
      }
      return false; // 避免手機瀏覽器的預設行為
  }
  
  function keyPressed() {
      if (key === ' ' || key === 'ArrowUp') {
          dino.jump();
      } else if (key === 'r' || key === 'R') {
          resetGame();
      }
  }
  
  function resetGame() {
      dino = new Dinosaur();
      items = [];
      score = 0;
      gameOver = false;
      itemSpeed = 2.5; // 重置速度稍快
      items.push(new Item());
  }
  
  class Dinosaur {
      constructor() {
          this.r = 20; // 半徑調小
          this.x = width / 3; // 將恐龍放置於畫布中間偏左
          this.y = height / 2; // 調整為畫布中央
          this.gravity = 0.8;
          this.lift = -12;
          this.velocity = 0;
          this.doubleJumpAllowed = true; // 設置雙跳標誌
      }
  
      jump() {
          if (this.y === height / 2) {
              this.velocity = this.lift;
              this.doubleJumpAllowed = true; // 啟用雙跳
          } else if (this.doubleJumpAllowed) {
              this.velocity = this.lift;
              this.doubleJumpAllowed = false; // 使用雙跳後禁用
          }
      }
  
      update() {
          this.velocity += this.gravity;
          this.y += this.velocity;
  
          // 防止超過初始位置
          if (this.y >= height / 2) {
              this.y = height / 2;
              this.velocity = 0;
          }
      }
  
      show() {
          fill('#f5f5dc'); // 恐龍顏色改為米白色
          noStroke();
          this.drawHexagon(this.x, this.y, this.r); // 恐龍變成六角形
      }
  
      drawHexagon(x, y, radius) {
          beginShape();
          for (let i = 0; i < TWO_PI; i += TWO_PI / 6) {
              let sx = x + cos(i) * radius;
              let sy = y + sin(i) * radius;
              vertex(sx, sy);
          }
          endShape(CLOSE);
      }
  }
  
  class Item {
      constructor() {
          this.r = 10; // 六角形半徑調小
          this.x = width;
          this.y = random(height / 4, height / 2 - 20); // 道具在畫布中央的上方隨機位置
          this.speed = itemSpeed;
      }
  
      caught(dino) {
          let distance = dist(this.x, this.y, dino.x, dino.y);
          return distance < this.r + dino.r;
      }
  
      update() {
          this.x -= this.speed;
      }
  
      offscreen() {
          return this.x < -this.r;
      }
  
      show() {
          fill('#c95b4a'); // 道具顏色改為 #c95b4a
          noStroke(); // 移除道具邊框
          this.drawHexagon(this.x, this.y, this.r);
      }
  
      drawHexagon(x, y, radius) {
          beginShape();
          for (let i = 0; i < TWO_PI; i += TWO_PI / 6) {
              let sx = x + cos(i) * radius;
              let sy = y + sin(i) * radius;
              vertex(sx, sy);
          }
          endShape(CLOSE);
      }
  }