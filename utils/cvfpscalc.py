import cv2 as cv

class CvFpsCalc(object):
    def __init__(self, buffer_len=1):
        self._tick_frequency = cv.getTickFrequency()
        self._start_tick = cv.getTickCount()

    def get(self):
        current_tick = cv.getTickCount()
        different_tick = current_tick - self._start_tick
        self._start_tick = current_tick
        return round(self._tick_frequency / different_tick, 1)