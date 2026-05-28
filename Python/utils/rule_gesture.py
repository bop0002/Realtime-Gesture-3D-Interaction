import math


def _dist(p1, p2):
    return math.hypot(p1[0] - p2[0], p1[1] - p2[1])


def recognize_gesture(lm_list):
    """Rule-based gesture detection từ 21 landmark MediaPipe.

    Trả về 1 trong các nhãn cùng vocab với model
    (models/gesture_classification/gesture_classification_label.csv):
        Open / Close / Pointer / Peace / OK / ThumbsUp / None
    """
    if lm_list is None or len(lm_list) < 21:
        return "None"

    fingers = [0, 0, 0, 0, 0]
    tips = [8, 12, 16, 20]
    pips = [6, 10, 14, 18]
    for i in range(4):
        if _dist(lm_list[0], lm_list[tips[i]]) > _dist(lm_list[0], lm_list[pips[i]]):
            fingers[i + 1] = 1

    # Thumb: so khoảng cách thumb_tip -> pinky_mcp với thumb_mcp -> pinky_mcp
    if _dist(lm_list[4], lm_list[17]) > _dist(lm_list[5], lm_list[17]) * 1.2:
        fingers[0] = 1

    # OK check trước: thumb_tip chạm index_tip + ngón giữa duỗi.
    # Pattern fingers[] của OK lệch với mọi rule khác (index curl xuống chạm thumb)
    # nên phải bắt riêng, không qua bảng so khớp.
    palm_len = _dist(lm_list[0], lm_list[9])  # wrist -> middle MCP, ref scale theo bàn tay
    if palm_len > 0 and fingers[2] == 1:
        if _dist(lm_list[4], lm_list[8]) < 0.4 * palm_len:
            return "OK"

    if fingers == [1, 1, 1, 1, 1]:
        return "Open"
    if fingers == [0, 1, 0, 0, 0]:
        return "Pointer"
    if fingers == [0, 1, 1, 0, 0]:
        return "Peace"
    if fingers == [1, 0, 0, 0, 0]:
        return "ThumbsUp"
    if fingers == [0, 0, 0, 0, 0]:
        return "Close"
    return "None"
