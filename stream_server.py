import asyncio
import websockets
import cv2
import base64
import json

async def stream_frames(websocket, path):
    # Initialize the stereo camera
    cap = cv2.VideoCapture(2, cv2.CAP_DSHOW)  # Replace 4 with your camera index
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 2560)  # Width to include both frames
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)  # Set the appropriate height
    cap1 = cv2.VideoCapture()
    if not cap.isOpened():
        print("Cannot open the stereo camera.")
        return

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("Failed to grab frame.")
                break

            # Assuming the combined frame is split horizontally
            frame_height, frame_width = frame.shape[:2]
            half_width = frame_width // 2

            # Split the frame into left and right images
            frame_left = frame[:, :half_width]
            frame_right = frame[:, half_width:]

            # Resize frames to reduce bandwidth (optional)
            # frame_left = cv2.resize(frame_left, (320, 240))
            # frame_right = cv2.resize(frame_right, (320, 240))

            # Encode frames as JPEG with higher compression
            _, buffer_left = cv2.imencode('.jpg', frame_left, [int(cv2.IMWRITE_JPEG_QUALITY), 100])
            _, buffer_right = cv2.imencode('.jpg', frame_right, [int(cv2.IMWRITE_JPEG_QUALITY), 100])

            # Convert to base64
            encoded_left = base64.b64encode(buffer_left).decode('utf-8')
            encoded_right = base64.b64encode(buffer_right).decode('utf-8')

            # Create JSON message
            message = json.dumps({'left': encoded_left, 'right': encoded_right})

            # Send the message
            await websocket.send(message)

            # Control frame rate (~15 FPS)
            await asyncio.sleep(1/30)
    except websockets.exceptions.ConnectionClosed:
        print("Connection closed.")
    finally:
        cap.release()

start_server = websockets.serve(stream_frames, '0.0.0.0', 8765)

print("WebSocket server started on ws://0.0.0.0:8765")
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
