import asyncio
import json
import cv2
from aiortc import RTCPeerConnection, VideoStreamTrack
from aiortc.contrib.media import MediaPlayer
import websockets

class VideoCaptureTrack(VideoStreamTrack):
    """
    A video stream track that captures video from a camera.
    """
    def __init__(self, camera_id):
        super().__init__()
        self.cap = cv2.VideoCapture(camera_id)

    async def recv(self):
        # Read a frame from the camera
        ret, frame = self.cap.read()
        if not ret:
            raise Exception("Failed to grab frame")
        
        # Resize and convert to RGB
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        return frame

    def stop(self):
        self.cap.release()

async def run_signaling(pc, websocket):
    while True:
        message = await websocket.recv()
        data = json.loads(message)

        if 'sdp' in data:
            await pc.setRemoteDescription(data['sdp'])
            answer = await pc.createAnswer()
            await pc.setLocalDescription(answer)
            await websocket.send(json.dumps({'sdp': pc.localDescription}))

async def stream_frames(websocket, path):
    pc = RTCPeerConnection()
    video_track = VideoCaptureTrack(0)  # Change the camera ID if necessary
    pc.addTrack(video_track)

    # Handle signaling
    await run_signaling(pc, websocket)

start_server = websockets.serve(stream_frames, '0.0.0.0', 8765)
print("WebSocket server started on ws://0.0.0.0:8765")
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
