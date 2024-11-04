import cv2

cap = cv2.VideoCapture(1,cv2.CAP_DSHOW)

while True:
    ret, frame = cap.read()
    
    cv2.imshow('frame',frame)
    
    if cv2.waitKey(1) == ord('q'):
        break

# Release the capture and writer objects
cap.release()
cv2.destroyAllWindows()