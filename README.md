# Realtime Gesture 3D Interaction

Dự án nghiên cứu và phát triển hệ thống tương tác người - máy thời gian thực bằng cử chỉ bàn tay 3D sử dụng Webcam thông thường. Hệ thống kết hợp giữa xử lý ảnh, học máy nhận dạng cử chỉ (phía Python Server) và môi trường mô phỏng/trò chơi tương tác 3D (phía Unity Client) kết nối qua giao thức mạng UDP.

---

## 📂 Cấu trúc thư mục dự án

```text
Realtime-Gesture-3D-Interaction/
├── Python/                      # Module xử lý AI & nhận dạng cử chỉ (Backend)
│   ├── models/                  # Lưu trữ dữ liệu huấn luyện và file trọng số mô hình
│   │   ├── gesture_classification/  # Phân loại cử chỉ tĩnh (Keras, H5, TFLite, CSV)
│   │   └── dynamic_gesture/         # Phân loại cử chỉ động (HDF5, TFLite, CSV, Code)
│   ├── utils/                   # Bộ công cụ bổ trợ (tính FPS,...)
│   ├── main.py                  # Script chạy chính (suy luận & thu thập dữ liệu)
│   ├── handMediapipe.py         # Module cấu hình MediaPipe Hands
│   └── README.md                # Hướng dẫn chi tiết cho phía Python
│
├── 3DHandModel/                 # Dự án Unity mô phỏng mô hình xương tay 3D (Client 1)
│   └── Assets/
│       ├── Scenes/SampleScene.unity  # Scene chạy chính mô phỏng xương tay
│       └── Scripts/             # Chứa mã nguồn nhận UDP, đồng bộ xương tay và grabbing vật lý
│
├── FruitNinja/                  # Dự án Unity game Fruit Ninja điều khiển bằng cử chỉ (Client 2)
│   └── Assets/
│       ├── Scenes/FruitNinja.unity   # Scene game Fruit Ninja chém quả
│       └── Scripts/             # Nhận dữ liệu ngón tay, áp dụng One Euro Filter và logic game
│
├── BTL Template/                # Thư mục LaTeX báo cáo bài tập lớn môn Thực tập cơ sở
│   ├── Chuong/                  # Nội dung các chương (1, 2, 3, 4)
│   ├── Hinhve/                  # Các sơ đồ, hình vẽ và Confusion Matrix
│   └── BTL.tex                  # File biên dịch chính của tài liệu báo cáo
│
├── requirement.txt              # Danh sách thư viện Python cần cài đặt
└── README.md                    # Hướng dẫn tổng quan dự án (File này)
```

---

## 🛠️ Hướng dẫn cài đặt và vận hành

### 1. Phía Python Backend (AI & Suy luận)
Yêu cầu Python version 3.8 - 3.10.

**Bước 1:** Di chuyển vào thư mục dự án và cài đặt các thư viện cần thiết:
```bash
pip install -r requirement.txt
```

**Bước 2:** Chạy file chính `main.py`:
```bash
python Python/main.py
```
*Mặc định hệ thống sẽ khởi động ở **Chế độ 0 (Dự đoán cử chỉ)** và truyền dữ liệu qua socket UDP (IP: `127.0.0.1`, Port: `5026`).*

Các chế độ hoạt động trong `main.py`:
* **Mode 0 (Predict Mode):** Đọc Webcam, trích xuất điểm mốc, suy luận cử chỉ tĩnh/động và gửi sang Unity.
* **Mode 1 (Collect Static Mode):** Thu thập dữ liệu cử chỉ tĩnh. Nhấn các phím tương ứng với nhãn để lưu landmark vào `gesture_classification.csv`.
* **Mode 2 (Collect Dynamic Mode):** Thu thập dữ liệu cử chỉ động (quỹ đạo đầu ngón trỏ) và lưu vào `dynamic_gesture.csv`.

---

### 2. Phía Unity Client (Môi trường tương tác 3D)
Yêu cầu Unity Editor (Khuyên dùng phiên bản LTS 2021/2022 trở lên).

#### Ứng dụng Mô phỏng xương tay 3D (`3DHandModel`)
1. Mở thư mục `3DHandModel` bằng **Unity Hub**.
2. Mở Scene chính tại đường dẫn `Assets/Scenes/SampleScene.unity`.
3. Nhấn nút **Play** trong Unity Editor để bắt đầu nhận dữ liệu tọa độ xương tay từ Python Backend.
   * **Cử chỉ tương tác:** Cử chỉ `Close` (Nắm tay) dùng để cầm nắm các vật thể có gắn thuộc tính `Grabbable` trong không gian 3D. Khi thả tay ra (`Open`), vật thể sẽ được ném đi với gia tốc vật lý tương ứng.

#### Trò chơi Fruit Ninja (`FruitNinja`)
1. Mở thư mục `FruitNinja` bằng **Unity Hub**.
2. Mở Scene chính tại đường dẫn `Assets/Scenes/FruitNinja.unity`.
3. Nhấn nút **Play** để chơi game.
   * **Điều khiển:** Sử dụng cử chỉ tĩnh `Pointer` (Chỉ ngón trỏ) để kích hoạt lưỡi dao chém hoa quả. Vị trí lưỡi dao bám sát theo ngón trỏ của bạn và đã được lọc mượt thích nghi bằng bộ lọc **One Euro Filter** để giảm tối đa hiện tượng rung nhấp nháy (jitter).

---

## ⚡ Giao thức truyền dữ liệu (UDP Socket)
* **IP Server:** `127.0.0.1` (Localhost)
* **Cổng (Port):** `5026`
* **Định dạng thông điệp:** Chuỗi UTF-8 phân tách bằng dấu phẩy gồm tọa độ 21 điểm mốc khớp tay và tên cử chỉ tĩnh:
  `x0,y0,z0,x1,y1,z1,...,x20,y20,z20,GestureName,W,H`

---

## 📊 Mô hình Học máy nhận dạng cử chỉ
Hệ thống sử dụng mạng nơ-ron truyền thẳng **MLP (Multi-Layer Perceptron)** được huấn luyện qua Keras và chuyển đổi sang định dạng **TensorFlow Lite (.tflite)** để tối ưu hóa suy luận thời gian thực trên CPU:
* **Mô hình cử chỉ tĩnh (6 lớp):** `Open`, `Close`, `Pointer`, `OK`, `Peace`, `ThumbsUp`. Độ chính xác trên tập kiểm thử đạt khoảng **99%**.
* **Mô hình cử chỉ động (10 lớp):** Các chuyển động vuốt (`Swipe Left/Right/Up/Down`), vẽ hình học (`Circle`, `Triangle`), ký tự vẽ tay (`Z-Shape`, `S-Shape`, `Infinity`). Độ chính xác trên tập kiểm thử đạt khoảng **84%** (kết hợp giải thuật lọc biểu quyết Majority Voting phía Python để đảm bảo ổn định nhãn đầu ra).
