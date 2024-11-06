import cv2
import os
import time
from pathlib import Path

class AutoCalibrationCapture:
    def __init__(self, camera_index=1, width=2560, height=720):
        # Create directories for saving images
        self.output_dir = Path("calibration_images")
        self.left_dir = self.output_dir / "left"
        self.right_dir = self.output_dir / "right"
        
        self.left_dir.mkdir(parents=True, exist_ok=True)
        self.right_dir.mkdir(parents=True, exist_ok=True)

        # Initialize camera
        self.cap = cv2.VideoCapture(camera_index)
        if not self.cap.isOpened():
            raise RuntimeError("Failed to open camera")
            
        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, width)
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, height)

        # Capture settings
        self.capture_interval = 0.7  # Time in seconds between captures
        self.total_images = 40      # Total number of images to capture
        
    def run(self):
        print(f"\nAutomatic Calibration Capture")
        print("---------------------------")
        print(f"Will capture {self.total_images} image pairs")
        print(f"Interval: {self.capture_interval} seconds")
        print("\nControls:")
        print("  SPACE - Start automatic capture")
        print("  Q - Quit")
        print("\nWhen ready to start:")
        print("1. Hold the calibration pattern steady")
        print("2. Press SPACE to begin automatic capture")
        print("3. Slowly move/rotate the pattern between captures")

        image_counter = 0
        auto_capture = False
        last_capture_time = 0

        while True:
            ret, frame = self.cap.read()
            if not ret:
                print("Failed to grab frame")
                break

            # Split into left and right images
            h, w = frame.shape[:2]
            left_img = frame[:, :w//2]
            right_img = frame[:, w//2:]

            # Display the images
            cv2.imshow('Stereo Cameras', frame)

            # Handle automatic capture
            if auto_capture:
                current_time = time.time()
                if current_time - last_capture_time >= self.capture_interval:
                    # Save the image pair
                    left_path = str(self.left_dir / f"left_{image_counter:02d}.png")
                    right_path = str(self.right_dir / f"right_{image_counter:02d}.png")
                    
                    cv2.imwrite(left_path, left_img)
                    cv2.imwrite(right_path, right_img)
                    
                    print(f"Captured pair {image_counter + 1}/{self.total_images}")
                    
                    image_counter += 1
                    last_capture_time = current_time

                    # Check if we're done
                    if image_counter >= self.total_images:
                        print("\nCapture complete!")
                        break

            # Handle key presses
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                break
            elif key == ord(' ') and not auto_capture:
                print("\nStarting automatic capture...")
                auto_capture = True
                last_capture_time = time.time()

        self.cap.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    try:
        capture = AutoCalibrationCapture()
        capture.run()
    except Exception as e:
        print(f"Error: {str(e)}")