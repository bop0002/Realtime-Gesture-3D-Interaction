import os
import cv2
import csv
import copy
import itertools
import pandas as pd

from utils.cvfpscalc import CvFpsCalc
from cvzone.HandTrackingModule import HandDetector
from models.gesture_classification.gesture_classification import GestureClassifier

CSV_PATH = "models/gesture_classification/gesture_classification.csv"
LABEL_PATH = "models/gesture_classification/gesture_classification_label.csv"
MODEL_PATH = "models/gesture_classification/gesture_classification.tflite"

VALID_CLASSES = [0, 1, 2]


def count_samples():
    try:
        data = pd.read_csv(CSV_PATH, header=None)
        counts = data[0].value_counts().to_dict()
        return counts
    except Exception:
        return {}


def pre_process_landmark(landmark_list):
    temp_landmark_list = copy.deepcopy(landmark_list)

    # Lấy landmark đầu tiên làm gốc
    base_x, base_y = temp_landmark_list[0]

    for index, landmark_point in enumerate(temp_landmark_list):
        temp_landmark_list[index][0] = landmark_point[0] - base_x
        temp_landmark_list[index][1] = landmark_point[1] - base_y

    # Flatten list [[x,y], [x,y], ...] -> [x,y,x,y,...]
    temp_landmark_list = list(itertools.chain.from_iterable(temp_landmark_list))

    max_value = max(map(abs, temp_landmark_list))

    # Tránh chia cho 0
    if max_value == 0:
        return temp_landmark_list

    temp_landmark_list = [n / max_value for n in temp_landmark_list]

    return temp_landmark_list


def logging_csv(number, landmark_list):
    if number not in VALID_CLASSES:
        return

    with open(CSV_PATH, "a", newline="") as f:
        writer = csv.writer(f)
        writer.writerow([number, *landmark_list])


def draw_info(frame, mode, current_label, gesture_name, counts, model_loaded,fps):
    mode_text = "COLLECT MODE" if mode == 1 else "PREDICT MODE"
    cv2.putText(
        frame,
        mode_text,
        (10, 30),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.8,
        (0, 255, 255),
        2
    )

    cv2.putText(
        frame,
        f"Label key: {current_label if current_label != -1 else 'None'}",
        (10, 60),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (255, 255, 255),
        2
    )

    cv2.putText(
        frame,
        f"Gesture: {gesture_name}",
        (10, 90),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.8,
        (0, 255, 0),
        2
    )

    cv2.putText(
        frame,
        f"Model: {'Loaded' if model_loaded else 'Not found'}",
        (10, 120),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (255, 255, 255),
        2
    )

    cv2.putText(
        frame,
        f"Open (0): {counts.get(0, 0)}",
        (10, 160),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (255, 255, 255),
        2
    )

    cv2.putText(
        frame,
        f"Close (1): {counts.get(1, 0)}",
        (10, 190),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (255, 255, 255),
        2
    )

    cv2.putText(
        frame,
        f"Pointer (2): {counts.get(2, 0)}",
        (10, 220),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (255, 255, 255),
        2
    )

    cv2.putText(
        frame,
        "k: collect mode | p: predict mode | 0/1/2: save label | esc: exit",
        (10, 460),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.55,
        (200, 200, 200),
        1
    )
    cv2.putText(
        frame,
        f"FPS: {fps}",
        (10, 260),
        cv2.FONT_HERSHEY_SIMPLEX,
        0.7,
        (0, 255, 255),
        2
    )

def main():
    cap = cv2.VideoCapture(0)
    detector = HandDetector(maxHands=1)
    cap.set(3,1920)
    cap.set(4,1080)

    fps_calc = CvFpsCalc()
    gesture_classifier = None
    model_loaded = False

    if os.path.exists(MODEL_PATH) and os.path.getsize(MODEL_PATH) > 0:
        try:
            gesture_classifier = GestureClassifier(model_path=MODEL_PATH)
            model_loaded = True
        except Exception as e:
            print("Cannot load TFLite model:", e)
            gesture_classifier = None
            model_loaded = False

    with open(LABEL_PATH, "r", encoding="utf-8") as f:
        gesture_labels = [row.strip() for row in f.readlines()]

    mode = 0  # 0 = predict, 1 = collect
    current_label = -1

    while True:
        fps = fps_calc.get()
        ret, frame = cap.read()

        if not ret:
            break

        frame = cv2.flip(frame, 1)

        key = cv2.waitKey(1) & 0xFF

        # Chuyển mode
        if key == ord("k"):
            mode = 1
        elif key == ord("p"):
            mode = 0

        # Gán nhãn khi thu dữ liệu
        if key in [ord("0"), ord("1"), ord("2")]:
            current_label = int(chr(key))

        hands, frame = detector.findHands(frame)
        gesture_name = "No hand detected"

        if hands:
            hand = hands[0]
            lmList = hand["lmList"]

            # Chỉ lấy x, y
            landmark_list = [[lm[0], lm[1]] for lm in lmList]

            pre_processed = pre_process_landmark(landmark_list)

            # Thu dữ liệu nếu đang ở collect mode
            if mode == 1 and current_label in VALID_CLASSES:
                logging_csv(current_label, pre_processed)
                gesture_name = f"Collecting for label {current_label}"

            # Dự đoán nếu có model và đang ở predict mode
            elif mode == 0 and model_loaded and gesture_classifier is not None:
                try:
                    gesture_id = gesture_classifier(pre_processed)
                    if 0 <= gesture_id < len(gesture_labels):
                        gesture_name = gesture_labels[gesture_id]
                    else:
                        gesture_name = f"Unknown ID: {gesture_id}"
                except Exception as e:
                    gesture_name = f"Predict error: {str(e)}"

            elif mode == 0 and not model_loaded:
                gesture_name = "No model - collect data first"

        counts = count_samples()
        draw_info(frame, mode, current_label, gesture_name, counts, model_loaded,fps)

        cv2.imshow("Gesture Recognition", frame)

        if key == 27:
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()