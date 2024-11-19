import numpy as np
import cv2 as cv

# Load stereo map parameters
cv_file = cv.FileStorage()
cv_file.open('stereoMap.xml', cv.FileStorage_READ)

stereoMapL_x = cv_file.getNode('stereoMapL_x').mat()
stereoMapL_y = cv_file.getNode('stereoMapL_y').mat()
stereoMapR_x = cv_file.getNode('stereoMapR_x').mat()
stereoMapR_y = cv_file.getNode('stereoMapR_y').mat()
cv_file.release()

# Load the images
left_image_path = "left_wide/left_12.png"
right_image_path = "right_wide/right_12.png"

frame_left = cv.imread(left_image_path)
frame_right = cv.imread(right_image_path)

if frame_left is None or frame_right is None:
    print("Error: Could not load one or both images.")
    exit()

# Undistort and rectify the images
rectified_left = cv.remap(frame_left, stereoMapL_x, stereoMapL_y, cv.INTER_LANCZOS4, cv.BORDER_CONSTANT, 0)
rectified_right = cv.remap(frame_right, stereoMapR_x, stereoMapR_y, cv.INTER_LANCZOS4, cv.BORDER_CONSTANT, 0)

# Display the rectified images
cv.imshow("Rectified Left Image", rectified_left)
cv.imshow("Rectified Right Image", rectified_right)

cv.waitKey(0)
cv.destroyAllWindows()
