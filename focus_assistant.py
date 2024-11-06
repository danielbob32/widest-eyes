import cv2
import numpy as np
from pathlib import Path

class StereoFocusAssistant:
    def __init__(self, camera_index=1, width=2560, height=720):
        self.cap = cv2.VideoCapture(camera_index)
        if not self.cap.isOpened():
            raise RuntimeError("Failed to open camera")
        
        # Set resolution for side-by-side stereo
        self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, width)
        self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, height)
    
    def analyze_focus(self, image):
        """Calculate focus metrics for an image region"""
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        return cv2.Laplacian(gray, cv2.CV_64F).var()
    
    def draw_focus_pattern(self, image):
        """Draw focus assistance pattern on image"""
        h, w = image.shape[:2]
        overlay = image.copy()
        
        # Define regions to analyze
        regions = [
            # Center
            (w//3, h//3, 2*w//3, 2*h//3),
            # Corners
            (w//8, h//8, w//4, h//4),
            (3*w//4, h//8, 7*w//8, h//4),
            (w//8, 3*h//4, w//4, 7*h//8),
            (3*w//4, 3*h//4, 7*w//8, 7*h//8)
        ]
        
        for x1, y1, x2, y2 in regions:
            roi = image[y1:y2, x1:x2]
            focus_value = self.analyze_focus(roi)
            
            # Color code based on focus quality
            if focus_value > 100:
                color = (0, 255, 0)  # Green for good focus
            elif focus_value > 50:
                color = (0, 255, 255)  # Yellow for moderate focus
            else:
                color = (0, 0, 255)  # Red for poor focus
            
            cv2.rectangle(overlay, (x1, y1), (x2, y2), color, 2)
            cv2.putText(overlay, f"{focus_value:.0f}", (x1 + 5, y1 + 25),
                       cv2.FONT_HERSHEY_SIMPLEX, 0.7, color, 2)
        
        return overlay
    
    def run(self):
        """Main loop for focus assistance"""
        print("\nStereo Camera Focus Assistant")
        print("-----------------------------")
        print("Controls:")
        print("  F - Toggle focus assistance overlay")
        print("  Q - Quit")
        
        show_overlay = True
        
        while True:
            ret, frame = self.cap.read()
            if not ret:
                break
            
            # Split into left and right images
            h, w = frame.shape[:2]
            left_img = frame[:, :w//2]
            right_img = frame[:, w//2:]
            
            if show_overlay:
                left_display = self.draw_focus_pattern(left_img)
                right_display = self.draw_focus_pattern(right_img)
            else:
                left_display = left_img
                right_display = right_img
            
            # Display images
            cv2.imshow('Left Camera', left_display)
            cv2.imshow('Right Camera', right_display)
            
            key = cv2.waitKey(1) & 0xFF
            if key == ord('q'):
                break
            elif key == ord('f'):
                show_overlay = not show_overlay
        
        self.cap.release()
        cv2.destroyAllWindows()

if __name__ == "__main__":
    try:
        assistant = StereoFocusAssistant()
        assistant.run()
    except Exception as e:
        print(f"Error: {str(e)}")