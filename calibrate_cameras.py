import cv2
import numpy as np
import glob
import os

# Calibration parameters
CHESSBOARD_SIZE = (9, 6)  # Chessboard pattern dimensions (inner corners)
SQUARE_SIZE = 0.025       # Size of each square in meters (adjust based on actual size)

# Criteria for termination of the corner subpixel search algorithm
criteria = (cv2.TERM_CRITERIA_EPS + cv2.TERM_CRITERIA_MAX_ITER, 30, 0.001)

def calibrate_camera(image_dir, save_filename, debug_dir):
    """
    Calibrate a camera based on chessboard images and save calibration results.

    Args:
    - image_dir: Directory containing chessboard images for calibration.
    - save_filename: Path to save the calibration results (npz format).
    - debug_dir: Directory to save images with detected corners drawn.

    """
    # Prepare object points for a standard chessboard
    objp = np.zeros((CHESSBOARD_SIZE[1] * CHESSBOARD_SIZE[0], 3), np.float32)
    objp[:, :2] = np.mgrid[0:CHESSBOARD_SIZE[0], 0:CHESSBOARD_SIZE[1]].T.reshape(-1, 2)
    objp *= SQUARE_SIZE

    # Arrays to store object points and image points from all images
    objpoints = []  # 3D points in real-world space
    imgpoints = []  # 2D points in image plane
    image_shape = None  # To store the shape of the calibration images

    # Create debug directory if it doesn't exist
    os.makedirs(debug_dir, exist_ok=True)

    # Collect all calibration images
    images = glob.glob(os.path.join(image_dir, '*.png'))

    for idx, fname in enumerate(images):
        img = cv2.imread(fname)

        # Convert to grayscale
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

        # Store image shape (for the first image only)
        if image_shape is None:
            image_shape = gray.shape[::-1]

        # Find the chessboard corners
        ret, corners = cv2.findChessboardCorners(gray, CHESSBOARD_SIZE, None)

        # If corners are found, add object points, image points (after refining them)
        if ret:
            objpoints.append(objp)
            corners2 = cv2.cornerSubPix(gray, corners, (11, 11), (-1, -1), criteria)
            imgpoints.append(corners2)

            # Draw and save the corners for debugging
            cv2.drawChessboardCorners(img, CHESSBOARD_SIZE, corners2, ret)
            cv2.imwrite(os.path.join(debug_dir, f'detected_corners_{idx}.png'), img)
        else:
            print(f"Chessboard corners not found in image: {fname}")

    # Check if any chessboard corners were found
    if not objpoints or not imgpoints:
        print("Error: No chessboard corners were detected in any images.")
        return

    # Camera calibration
    ret, camera_matrix, dist_coeffs, rvecs, tvecs = cv2.calibrateCamera(
        objpoints, imgpoints, image_shape, None, None
    )

    print("Calibration complete.")
    print("Camera matrix:", camera_matrix)
    print("Distortion coefficients:", dist_coeffs)

    # Save calibration parameters
    np.savez(save_filename,
             camera_matrix=camera_matrix,
             dist_coeffs=dist_coeffs,
             rvecs=rvecs,
             tvecs=tvecs,
             image_shape=image_shape)

    # Generate undistortion maps (remapping functions)
    map1, map2 = cv2.initUndistortRectifyMap(
        camera_matrix, dist_coeffs, None, camera_matrix, image_shape, cv2.CV_32FC1
    )

    # Save the undistortion maps
    np.savez(os.path.join(debug_dir, 'undistort_maps.npz'), map1=map1, map2=map2)

    # Undistort an example image using the maps
    example_img = cv2.imread(images[0])
    undistorted_img = cv2.remap(example_img, map1, map2, interpolation=cv2.INTER_LINEAR)

    # Save the undistorted example image
    cv2.imwrite(os.path.join(debug_dir, 'undistorted_example.png'), undistorted_img)
    print(f"Undistorted image saved in {debug_dir}.")

    # Normalize the maps to the range [0, 1]
    map1_norm = map1 / (image_shape[0] - 1)
    map2_norm = map2 / (image_shape[1] - 1)

    # Stack the normalized maps into a 3-channel image
    zeros_channel = np.zeros_like(map1_norm, dtype=np.float32)
    undistort_map = cv2.merge([map1_norm, map2_norm, zeros_channel])

    # Convert to 16-bit unsigned integers
    undistort_map_16u = np.uint16(undistort_map * 65535)

    # Save the undistortion map as a 3-channel PNG image
    map_filename = os.path.join(debug_dir, 'undistort_map.png')
    cv2.imwrite(map_filename, undistort_map_16u)
    print(f"Undistortion map saved as image in {debug_dir}.")

if __name__ == "__main__":
    calibrate_camera(
        image_dir=r"D:\Projects\Python\Stereo\left_wide",    # Replace with your left camera images folder
        save_filename="left_camera_calibration.npz",
        debug_dir="left_calibration_debug"
    )

    calibrate_camera(
        image_dir=r"D:\Projects\Python\Stereo\right_wide",   # Replace with your right camera images folder
        save_filename="right_camera_calibration.npz",
        debug_dir="right_calibration_debug"
    )
