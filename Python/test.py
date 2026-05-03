import cv2
import socket
from cvzone.HandTrackingModule import HandDetector

cam = cv2.VideoCapture(0)

width,height = 1280,720

cam.set(3,width)
cam.set(4,height)

detector = HandDetector(detectionCon=0.8,maxHands = 2)

sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1",5025)
 
while True:

    success,img = cam.read()
    if success:
        img = cv2.flip(img, 1)
    hands,img = detector.findHands(img,flipType=False)

    if hands:
        hand = hands[0]
        handType = hand['type']
        lmList = hand['lmList']
        for lm in lmList:
            data.extend([lm[0],height - lm[1],lm[2]])
        print(data)
        sock.sendto(str.encode(str(data)),serverAddressPort)
    cv2.imshow("Image",img)

    if(cv2.waitKey(1) & 0xFF == ord('q')):
        break
cam.release()
cv2.destroyAllWindows()