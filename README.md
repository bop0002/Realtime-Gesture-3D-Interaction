# Realtime Gesture 3D Interaction

Dự án nghiên cứu, phát triển và tối ưu hóa hệ thống tương tác người - máy thời gian thực (Human-Computer Interaction - HCI) bằng cử chỉ bàn tay 3D sử dụng Webcam RGB thông thường. Hệ thống là sự kết hợp đồng bộ giữa pipeline AI xử lý ảnh và suy luận học máy (Python Backend) với các môi trường mô phỏng đồ họa và trò chơi tương tác (Unity Client) thông qua giao thức truyền dữ liệu UDP mạng cục bộ độ trễ thấp.

---

## 📂 Cấu trúc thư mục dự án

```text
Realtime-Gesture-3D-Interaction/
├── Python/                      # Module xử lý AI & nhận dạng cử chỉ (Backend)
│   ├── models/                  # Lưu trữ dữ liệu huấn luyện và file trọng số mô hình
│   │   ├── gesture_classification/  # Phân loại cử chỉ tĩnh (TFLite, Labels, CSV)
│   │   └── dynamic_gesture/         # Phân loại cử chỉ động (TFLite, Labels, CSV)
│   ├── utils/                   # Bộ công cụ bổ trợ (tính FPS, heuristics)
│   └── main.py                  # Script khởi chạy chính (suy luận & thu thập dữ liệu)
│
├── 3DHandModel/                 # Dự án Unity mô phỏng xương bàn tay 3D (Client 1)
│   └── Assets/
│       ├── Scenes/SampleScene.unity  # Scene chạy chính mô phỏng xương tay
│       └── 3DHand_Tracking/Scripts/  # Cịch bản đồng bộ xương tay, Grabbing & Reach Remap
│
├── FruitNinja/                  # Dự án Unity game Fruit Ninja điều khiển bằng cử chỉ (Client 2)
│   └── Assets/
│       ├── Scenes/FruitNinja.unity   # Scene game Fruit Ninja chém quả
│       └── Scripts/                  # Nhận dữ liệu ngón trỏ, One Euro Filter & logic game
│
├── BTL Template/                # Thư mục LaTeX báo cáo bài tập lớn môn Thực tập cơ sở
│   ├── Chuong/                  # Nội dung chi tiết các chương (1, 2, 3, 4)
│   ├── Hinhve/                  # Các sơ đồ kiến trúc, ma trận nhầm lẫn và kết quả
│   ├── BTL.tex                  # File biên dịch chính của tài liệu báo cáo
│   └── BTL.pdf                  # Tệp PDF báo cáo đã được biên dịch hoàn chỉnh
│
├── requirement.txt              # Danh sách thư viện Python cần cài đặt
├── .gitignore                   # Cấu hình bỏ qua các tệp rác (IDE, Unity, LaTeX build files)
└── README.md                    # Hướng dẫn tổng quan dự án (File này)
```

---

## 🛠️ Hướng dẫn cài đặt và vận hành

### 1. Phía Python Backend (AI & Suy luận)
Yêu cầu phiên bản Python từ `3.8` đến `3.10`.

**Bước 1: Cài đặt thư viện**
Khởi tạo môi trường ảo (khuyên dùng) và cài đặt các thư viện cần thiết:
```bash
pip install -r requirement.txt
```

**Bước 2: Khởi chạy Server**
```bash
python Python/main.py
```
*Hệ thống sẽ mặc định khởi chạy ở **Chế độ Dự đoán (Mode 0)**, mở webcam và thiết lập socket truyền UDP tại địa chỉ `127.0.0.1:5026`.*

**Các chế độ phím tắt điều khiển trên cửa sổ OpenCV:**
*   `p`: Chuyển sang **Mode 0 (Predict Mode)** - Nhận dạng cử chỉ tĩnh/động và truyền tọa độ sang Unity.
*   `k`: Chuyển sang **Mode 1 (Collect Static Mode)** - Thu thập dữ liệu cử chỉ tĩnh. Nhấn phím số từ `0` - `5` tương ứng với các nhãn để ghi landmark vào `gesture_classification.csv`.
*   `h`: Chuyển sang **Mode 2 (Collect Dynamic Mode)** - Thu thập dữ liệu cử chỉ động. Nhấn phím số từ `0` - `9` tương ứng với các nhãn quỹ đạo vẽ ngón trỏ để ghi vào `dynamic_gesture.csv`.
*   `Esc`: Thoát chương trình.

---

### 2. Phía Unity Client (Môi trường tương tác 3D)
Khuyên dùng phiên bản **Unity Editor LTS 2021/2022** trở lên.

#### Ứng dụng Mô phỏng xương tay 3D (`3DHandModel`)
1. Thêm và mở thư mục `3DHandModel` bằng **Unity Hub**.
2. Mở Scene chính tại: `Assets/3DHand_Tracking/Scenes/SampleScene.unity`.
3. Nhấn **Play** trong Unity Editor để bắt đầu nhận dữ liệu xương tay từ Python Backend.
   *   **Cơ chế Grabbing (Cầm nắm/Ném):** Khi nhận diện cử chỉ tĩnh `Close` (Nắm tay), các khối hộp gần ngón tay sẽ được hút vào lòng bàn tay (`PalmAnchor`). Khi chuyển trạng thái cử chỉ (`Open`), khối hộp sẽ được thả rơi hoặc ném đi với gia tốc thực tế.
   *   **Cơ chế Reach Remap (Khuếch đại tầm với):** Tích hợp tính năng khuếch đại phi tuyến giúp người dùng di chuyển nhẹ nhàng ngoài đời thực vẫn quét hết playgrounds ảo.
   *   **Cơ chế chống nhiễu:** Sử dụng dung hợp cử chỉ `BothMatch` (chỉ nắm tay khi cả AI MLP và Heuristics cùng đồng thuận nhãn "Close") và debounce trễ nhả đồ vật `0.5` giây.

#### Trò chơi Fruit Ninja (`FruitNinja`)
1. Thêm và mở thư mục `FruitNinja` bằng **Unity Hub**.
2. Mở Scene chính tại: `Assets/Scenes/FruitNinja.unity`.
3. Nhấn **Play** để chơi game.
   *   **Lưỡi dao (Blade):** Điều khiển trực tiếp bằng tọa độ đầu ngón trỏ khi duy trì cử chỉ `Pointer` (chỉ ngón trỏ).
   *   **Lọc chống rung thích nghi (One Euro Filter):** Áp dụng trực tiếp lên tọa độ ngón trỏ phía Unity Client để loại bỏ rung lắc vật lý (jitter) khi tay đứng yên, đồng thời giữ nguyên độ bám tức thời khi di chuyển nhanh.

---

## ⚡ Giao thức truyền dữ liệu UDP

*   **Địa chỉ:** `127.0.0.1` (mạng cục bộ)
*   **Cổng (Port):** `5026`
*   **Cấu trúc gói tin truyền (dạng chuỗi ký tự UTF-8 phân tách bằng dấu phẩy):**
    `[x0, y0, z0, ..., x20, y20, z20, GestureName, W, H, RuleGestureName]`
    *   `x_i, y_i, z_i`: Tọa độ 21 điểm mốc xương tay (Y đã được đảo chiều tương thích Unity).
    *   `GestureName`: Nhãn cử chỉ tĩnh dự đoán từ mô hình MLP.
    *   `W, H`: Chiều rộng, chiều cao thực tế của khung camera (dùng cho việc chuẩn hóa động).
    *   `RuleGestureName`: Nhãn cử chỉ tĩnh dự đoán từ giải thuật Heuristics.

---

## 📊 Mô hình Học máy & Kết quả thực nghiệm

Hệ thống huấn luyện các mạng nơ-ron truyền thẳng **MLP (Multi-Layer Perceptron)** thông qua Keras, sau đó tối ưu hóa sang định dạng **TensorFlow Lite (.tflite)** để thực thi thời gian thực tốc độ cao trên CPU phổ thông:

1.  **Bộ phân loại cử chỉ tĩnh (6 lớp: `Open`, `Close`, `Pointer`, `OK`, `Peace`, `ThumbsUp`):**
    *   **Tập dữ liệu:** 13.471 mẫu train, 4.491 mẫu test.
    *   **Độ chính xác (Accuracy):** Mạng MLP đạt **99.0%** (so với giải thuật luật hình học Heuristics đạt **78.27%**).
    *   **Tốc độ suy luận:** ~1.2 ms trên CPU.
2.  **Bộ phân loại cử chỉ động (10 lớp quỹ đạo ngón trỏ):**
    *   **Tập dữ liệu:** 23.538 chuỗi train, 7.847 chuỗi test.
    *   **Độ chính xác (Accuracy):** Đạt **84.0%** trên tập kiểm thử độc lập (sử dụng biểu quyết Majority Voting cửa sổ trượt $M=32$ để triệt tiêu nhiễu nhảy nhãn tức thời).
3.  **Hiệu năng toàn hệ thống:**
    *   **Tốc độ xử lý (FPS):** Duy trì ổn định từ **30 - 32 FPS** ở độ phân giải HD $960 \times 540$.
    *   **Độ trễ phản hồi ước tính:** Dao động từ **35 - 45 ms** (truyền UDP nội bộ máy tính).

---

## 📝 Biên dịch Báo cáo LaTeX

Tài liệu báo cáo chi tiết được tổ chức và biên dịch thông qua LaTeX nằm trong thư mục `BTL Template/`.

Để biên dịch báo cáo sang PDF:
1.  Đảm bảo máy tính đã cài đặt **MiKTeX** và trình biên dịch **pdflatex**.
2.  Mở terminal tại thư mục `BTL Template/` và chạy lệnh:
    ```bash
    pdflatex BTL.tex
    ```
    *(Khuyên chạy lệnh 2 lần để cập nhật chính xác các liên kết chéo và mục lục)*
3.  Tệp PDF kết quả được lưu tại [BTL.pdf](file:///c:/Users/TUF%20DASH/Realtime-Gesture-3D-Interaction/BTL%20Template/BTL.pdf).
