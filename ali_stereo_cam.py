#%%
import cv2

# Initialize the camera
cap = cv2.VideoCapture(1, cv2.CAP_DSHOW)  # Use the correct index for your stereo camera
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 2560)  # Set the width to include both left and right frames
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 1440)  # Set the height appropriately

while True:
    # Capture frame-by-frame
    ret, frame = cap.read()
    if not ret:
        print("Error: Could not read the frame.")
        break

    # Assuming the combined frame is split horizontally
    frame_height, frame_width = frame.shape[:2]
    half_width = frame_width // 2

    left_frame = frame[:, :half_width]  # Left half of the frame
    right_frame = frame[:, half_width:]  # Right half of the frame


    # Combine frames side-by-side for a single display
    combined_frame = cv2.hconcat([left_frame, right_frame])

    # Define label properties
    font = cv2.FONT_HERSHEY_SIMPLEX
    font_scale = 0.9
    color = (0, 0, 255)  # Black color
    thickness = 2
    left_label_position = (10, 30)
    right_label_position = (half_width + 10, 30)

    # Add labels to the combined frame
    cv2.putText(combined_frame, 'Left Frame', left_label_position, font, font_scale, color, thickness, cv2.LINE_AA)
    cv2.putText(combined_frame, 'Right Frame', right_label_position, font, font_scale, color, thickness, cv2.LINE_AA)

    # Display the labeled combined frame
    cv2.imshow('Stereo Camera', frame)
    # print the camera res
    print('Camera resolution: ', frame_width, 'x', frame_height)
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

# Release the video capture and close the windows
cap.release()
cv2.destroyAllWindows()

# %%
