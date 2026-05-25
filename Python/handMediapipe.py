import cv2
import socket
from cvzone.HandTrackingModule import HandDetector
import os
import math

def recognize_gesture(detector, hand):
    lmList = hand['lmList']
    fingers = [0, 0, 0, 0, 0]
    
    if len(lmList) < 21: return "None"
    
    def dist(p1, p2):
        return math.hypot(p1[0] - p2[0], p1[1] - p2[1])
        
    tips = [8, 12, 16, 20]
    pips = [6, 10, 14, 18]
    for i in range(4):
        if dist(lmList[0], lmList[tips[i]]) > dist(lmList[0], lmList[pips[i]]):
            fingers[i+1] = 1
            
    # Thumb
    if dist(lmList[4], lmList[17]) > dist(lmList[5], lmList[17]) * 1.2:
        fingers[0] = 1
    
    if fingers == [1, 1, 1, 1, 1]:
        return "Open Hand"
    elif fingers == [0, 1, 0, 0, 0]:
        return "Index Finger Up"
    elif fingers == [0, 1, 1, 0, 0]:
        return "Peace"
    elif fingers == [0, 0, 0, 0, 0]:
        return "Close"
        
    return "None"

cam = cv2.VideoCapture(0)

width,height = 1280,720

cam.set(3,width)
cam.set(4,height)

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '2'

detector = HandDetector(detectionCon=0.8,maxHands = 2)

sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1",5025)
 
while True:

    success,img = cam.read()
    if success:
        img = cv2.flip(img, 1)
    hands,img = detector.findHands(img,flipType=False)

    data = []

    if hands:
        hand = hands[0]
        handType = hand['type']
        lmList = hand['lmList']
        
        #Gesture
        gesture = recognize_gesture(detector, hand)
        if gesture != "None":
            cv2.putText(img, f"Gesture: {gesture}", (30, 50), cv2.FONT_HERSHEY_COMPLEX, 1, (0, 255, 0), 2)


        for lm in lmList:
            data.extend([lm[0],height - lm[1],lm[2]])
            
        data.append(gesture)
        
        print(data)
        sock.sendto(str.encode(str(data)),serverAddressPort)
    cv2.imshow("Image",img)

    if(cv2.waitKey(1) & 0xFF == ord('q')):
        break
cam.release()
cv2.destroyAllWindows()