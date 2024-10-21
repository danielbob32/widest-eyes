import asyncio
import websockets
import cv2
import base64
import json

async def stream_frames(websocket, path):
    # Initialize video captures for both cameras (a version prior using stereo camera)
    cap_left = cv2.VideoCapture(0)   # Integrated camera on laptop
    cap_right = cv2.VideoCapture(1)  # External camera

    if not cap_left.isOpened():
        print("Cannot open integrated camera (ID 0).")
        return
    if not cap_right.isOpened():
        print("Cannot open external camera (ID 1).")
        return

    try:
        while True:
            ret_left, frame_left = cap_left.read()
            ret_right, frame_right = cap_right.read()

            if not ret_left or not ret_right:
                print("Failed to grab frames.")
                break

            # Resize frames to reduce bandwidth due to lag in streaming, might remove this later
            frame_left = cv2.resize(frame_left, (320, 240))
            frame_right = cv2.resize(frame_right, (320, 240))

            # Encode frames as JPEG with higher compression
            _, buffer_left = cv2.imencode('.jpg', frame_left, [int(cv2.IMWRITE_JPEG_QUALITY), 50])
            _, buffer_right = cv2.imencode('.jpg', frame_right, [int(cv2.IMWRITE_JPEG_QUALITY), 50])

            # Convert to base64
            encoded_left = base64.b64encode(buffer_left).decode('utf-8')
            encoded_right = base64.b64encode(buffer_right).decode('utf-8')

            # Create JSON message
            message = json.dumps({'left': encoded_left, 'right': encoded_right})

            # Send the message
            await websocket.send(message)

            # Control frame rate (~15 FPS)
            await asyncio.sleep(1/15)
    except websockets.exceptions.ConnectionClosed:
        print("Connection closed.")
    finally:
        cap_left.release()
        cap_right.release()

start_server = websockets.serve(stream_frames, '0.0.0.0', 8765)

print("WebSocket server started on ws://0.0.0.0:8765")
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
