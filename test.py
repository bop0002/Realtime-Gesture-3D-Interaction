import os
import cv2
import csv
import copy
import itertools
import pandas as pd
from collections import deque, Counter

from utils.cvfpscalc import CvFpsCalc
from cvzone.HandTrackingModule import HandDetector
from models.gesture_classification.gesture_classification import GestureClassifier
from models.dynamic_gesture.dynamic_gesture import DynamicClassifier

CSV_PATH = "models/gesture_classification/gesture_classification.csv"
LABEL_PATH = "models/gesture_classification/gesture_classification_label.csv"
MODEL_PATH = "models/gesture_classification/gesture_classification.tflite"

DYNAMIC_CSV_PATH = "models/dynamic_gesture/dynamic_gesture.csv"
DYNAMIC_LABEL_PATH = "models/dynamic_gesture/dynamic_gesture_label.csv"
DYNAMIC_MODEL_PATH = "models/dynamic_gesture/dynamic_gesture.tflite"

VALID_CLASSES = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
VALID_DYNAMIC_CLASSES = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]


def count_samples():
    try:
        if not os.path.exists(CSV_PATH): return {}
        data = pd.read_csv(CSV_PATH, header=None)
        return data[0].value_counts().to_dict()
    except Exception:
        return {}

def count_dynamic_samples():
    try:
        if not os.path.exists(DYNAMIC_CSV_PATH): return {}
        data = pd.read_csv(DYNAMIC_CSV_PATH, header=None)
        return data[0].value_counts().to_dict()
    except Exception:
        return {}


def pre_process_landmark(landmark_list):
    temp_landmark_list = copy.deepcopy(landmark_list)
    base_x, base_y = temp_landmark_list[0]

    for index, landmark_point in enumerate(temp_landmark_list):
        temp_landmark_list[index][0] = landmark_point[0] - base_x
        temp_landmark_list[index][1] = landmark_point[1] - base_y

    temp_landmark_list = list(itertools.chain.from_iterable(temp_landmark_list))
    max_value = max(map(abs, temp_landmark_list))

    if max_value == 0:
        return temp_landmark_list

    temp_landmark_list = [n / max_value for n in temp_landmark_list]
    return temp_landmark_list


def pre_process_point_history(image_shape, point_history):
    image_width, image_height = image_shape[1], image_shape[0]
    temp_point_history = copy.deepcopy(point_history)

    base_x, base_y = 0, 0
    for index, point in enumerate(temp_point_history):
        if index == 0:
            base_x, base_y = point[0], point[1]

        temp_point_history[index][0] = (temp_point_history[index][0] - base_x) / image_width
        temp_point_history[index][1] = (temp_point_history[index][1] - base_y) / image_height

    temp_point_history = list(itertools.chain.from_iterable(temp_point_history))
    return temp_point_history


def logging_csv(number, landmark_list):
    if number not in VALID_CLASSES:
        return
    with open(CSV_PATH, "a", newline="") as f:
        writer = csv.writer(f)
        writer.writerow([number, *landmark_list])


def logging_csv_dynamic(number, point_history_list):
    if number not in VALID_DYNAMIC_CLASSES:
        return
    with open(DYNAMIC_CSV_PATH, "a", newline="") as f:
        writer = csv.writer(f)
        writer.writerow([number, *point_history_list])


def draw_point_history(image, point_history):
    for index, point in enumerate(point_history):
        if point[0] != 0 and point[1] != 0:
            cv2.circle(image, (point[0], point[1]), 1 + int(index / 2),
                      (152, 251, 152), 2)
    return image


def draw_info(frame, mode, current_label, gesture_name, dynamic_gesture_name, counts, dynamic_counts, model_loaded, dyn_model_loaded, fps):
    if mode == 1: mode_text = "COLLECT STATIC"
    elif mode == 2: mode_text = "COLLECT DYNAMIC"
    else: mode_text = "PREDICT MODE"
        
    cv2.putText(frame, mode_text, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 255), 2)
    cv2.putText(frame, f"Label key: {current_label if current_label != -1 else 'None'}", (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
    cv2.putText(frame, f"Static Gesture: {gesture_name}", (10, 90), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
    cv2.putText(frame, f"Dynamic Gesture: {dynamic_gesture_name}", (10, 120), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 165, 255), 2)

    cv2.putText(frame, f"Models: Static={'OK' if model_loaded else 'NO'} | Dynamic={'OK' if dyn_model_loaded else 'NO'}",
                (10, 150), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 1)

    if mode == 1:
        # Show static counts
        for i, c in enumerate(VALID_CLASSES[:10]): 
             cv2.putText(frame, f"Class {i}: {counts.get(i, 0)}", (10, 180 + i*30), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 200), 1)
    elif mode == 2:
        # Show dynamic counts
        for i in VALID_DYNAMIC_CLASSES:
             cv2.putText(frame, f"Dyn Class {i}: {dynamic_counts.get(i, 0)}", (10, 180 + i*30), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (200, 200, 200), 1)

    cv2.putText(frame, f"FPS: {fps}", (10, 430), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 255), 2)
    cv2.putText(frame, "p: predict | k: col static | h: col dynamic | 0-9: label | esc: quit", (10, 460), cv2.FONT_HERSHEY_SIMPLEX, 0.55, (200, 200, 200), 1)


def main():
    cap = cv2.VideoCapture(0)
    detector = HandDetector(maxHands=1)
    # Tối ưu: Giảm độ phân giải Camera xuống để MediaPipe không bị lag
    cap.set(3, 960)
    cap.set(4, 540)

    fps_calc = CvFpsCalc()
    
    # Models
    gesture_classifier = None
    model_loaded = False
    dynamic_classifier = None
    dyn_model_loaded = False

    if os.path.exists(MODEL_PATH) and os.path.getsize(MODEL_PATH) > 0:
        try:
            gesture_classifier = GestureClassifier(model_path=MODEL_PATH)
            model_loaded = True
        except Exception:
            pass

    if os.path.exists(DYNAMIC_MODEL_PATH) and os.path.getsize(DYNAMIC_MODEL_PATH) > 0:
        try:
            dynamic_classifier = DynamicClassifier(model_path=DYNAMIC_MODEL_PATH)
            dyn_model_loaded = True
        except Exception:
            pass

    # Labels
    try:
        with open(LABEL_PATH, "r", encoding="utf-8") as f:
            gesture_labels = [row.strip() for row in f.readlines()]
    except: gesture_labels = []

    try:
        with open(DYNAMIC_LABEL_PATH, "r", encoding="utf-8") as f:
            dynamic_labels = [row.strip() for row in f.readlines()]
    except: dynamic_labels = []

    mode = 0  # 0 = predict, 1 = collect static, 2 = collect dynamic
    current_label = -1
    
    # Biến nhớ tạm để không phải load file CSV mỗi frame
    cached_static_counts = count_samples()
    cached_dynamic_counts = count_dynamic_samples()
    
    history_length = 32
    point_history = deque(maxlen=history_length)
    finger_gesture_history = deque(maxlen=history_length)

    while True:
        fps = fps_calc.get()
        ret, frame = cap.read()

        if not ret: break
        frame = cv2.flip(frame, 1)

        key = cv2.waitKey(1) & 0xFF
        if key == 27: break
        elif key == ord("k"): mode = 1
        elif key == ord("h"): mode = 2
        elif key == ord("p"): mode = 0

        if 48 <= key <= 57: # 0-9 keys
            current_label = key - 48

        hands, frame = detector.findHands(frame)
        gesture_name = "None"
        dynamic_gesture_name = "None"
        gesture_id = -1

        if hands:
            hand = hands[0]
            lmList = hand["lmList"]
            landmark_list = [[lm[0], lm[1]] for lm in lmList]
            pre_processed = pre_process_landmark(landmark_list)

            # Predict static gesture (always needed to act as trigger)
            if model_loaded and gesture_classifier is not None:
                try:
                    gesture_id = gesture_classifier(pre_processed)
                    if 0 <= gesture_id < len(gesture_labels):
                        gesture_name = gesture_labels[gesture_id]
                except Exception:
                    pass

            # Update point history ONLY if gesture is Pointer (Assume Pointer is ID 2)
            if gesture_id == 2:
                point_history.append(landmark_list[8]) # Index finger tip
            else:
                point_history.append([0, 0])

            # Thu dữ liệu tĩnh
            if mode == 1 and current_label in VALID_CLASSES:
                logging_csv(current_label, pre_processed)
                cached_static_counts[current_label] = cached_static_counts.get(current_label, 0) + 1
                gesture_name = f"Class {current_label}"

            # Xử lý Cử chỉ động
            pre_processed_point = pre_process_point_history(frame.shape, point_history)
            
            if mode == 2 and current_label in VALID_DYNAMIC_CLASSES:
                # Thu dữ liệu động liên tục
                logging_csv_dynamic(current_label, pre_processed_point)
                cached_dynamic_counts[current_label] = cached_dynamic_counts.get(current_label, 0) + 1
                dynamic_gesture_name = f"Dyn Class {current_label}"
            elif mode == 0 and dyn_model_loaded and dynamic_classifier is not None:
                if len(pre_processed_point) == (history_length * 2):
                    try:
                        dyn_id = dynamic_classifier(pre_processed_point)
                        finger_gesture_history.append(dyn_id)
                        most_common_fg_id = Counter(finger_gesture_history).most_common()
                        res_id = most_common_fg_id[0][0]
                        
                        if res_id != -1 and 0 <= res_id < len(dynamic_labels):
                            dynamic_gesture_name = dynamic_labels[res_id]
                    except ValueError:
                        # Model cũ chưa được cập nhật cho input size mới (64)
                        dynamic_gesture_name = "Model Cũ / Lỗi Size"
        else:
            point_history.append([0, 0])

        frame = draw_point_history(frame, point_history)
        draw_info(frame, mode, current_label, gesture_name, dynamic_gesture_name, cached_static_counts, cached_dynamic_counts, model_loaded, dyn_model_loaded, fps)

        cv2.imshow("Gesture Recognition", frame)

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()