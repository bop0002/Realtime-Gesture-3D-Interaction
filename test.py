import cv2
from cvzone.HandTrackingModule import HandDetector

cam = cv2.VideoCapture(0)

width,height = 1280,720

cam.set(3,width)
cam.set(4,height)

detector = HandDetector(detectionCon=0.8,maxHands = 2)
data = []


while True:

    success,img = cam.read()

    hands,img = detector.findHands(img)

    if hands:
        hand = hands[0]

        lmList = hand['lmList']
        print(lmList)
        for lm in lmList:
            data.extend([lm[0],height - lm[1],lm[2]])
    cv2.imshow("Image",img)

    if(cv2.waitKey(1) & 0xFF ==ord('q')):
        break
cam.release()
cv2.destroyAllWindows()