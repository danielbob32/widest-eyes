using System;
using UnityEngine;
using NativeWebSocket;
using SimpleJSON;

public class VideoStreamReceiver : MonoBehaviour
{
    private WebSocket webSocket;
    public Material leftEyeMaterial;
    public Material rightEyeMaterial;
    private Texture2D leftTexture;
    private Texture2D rightTexture;

    async void Start()
    {
        string pcIP = "192.168.68.101"; // Replace with your PC's IP address
        webSocket = new WebSocket($"ws://{pcIP}:8765");

        // Event handlers: Open, Error, Close, Message
        webSocket.OnOpen += () => Debug.Log("WebSocket Connection Opened");
        webSocket.OnError += (e) => Debug.LogError("WebSocket Error: " + e);
        webSocket.OnClose += (e) => Debug.Log($"WebSocket Connection Closed: {e}");

        webSocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessMessage(message);
        };

        // Initialize textures
        leftTexture = new Texture2D(320, 240, TextureFormat.RGB24, false);
        rightTexture = new Texture2D(320, 240, TextureFormat.RGB24, false);

        // Assign initial textures to materials
        leftEyeMaterial.mainTexture = leftTexture;
        rightEyeMaterial.mainTexture = rightTexture;

        // Connect to the WebSocket server
        await webSocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket.DispatchMessageQueue();
#endif
    }

    async void OnApplicationQuit()
    {
        if (webSocket != null)
        {
            await webSocket.Close();
        }
    }

    void ProcessMessage(string message)
    {
        try
        {
            // if the message is empty, log an error and return
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("Received empty message");
                return;
            }
            
            // Parse the JSON message using SimpleJSON
            var data = JSON.Parse(message);
            
            // Extract the base64-encoded image data from the JSON message for the left and right eyes
            string leftBase64 = data["left"];
            string rightBase64 = data["right"];

            // if either image data is empty, log an error and return
            if (string.IsNullOrEmpty(leftBase64) || string.IsNullOrEmpty(rightBase64))
            {
                Debug.LogError("Received empty image data");
                return;
            }
            
            // Update the left and right eye textures with the new image data
            UpdateTexture(leftBase64, leftTexture);
            UpdateTexture(rightBase64, rightTexture);
        }

        catch (Exception e)
        {
            Debug.LogError("Error processing message: " + e);
        }
    }

    void UpdateTexture(string base64String, Texture2D texture)
    {
        // check if the texture or base64 string is invalid
        if (texture == null || string.IsNullOrEmpty(base64String))
        {
            Debug.LogError("Invalid texture or base64 string");
            return;
        }

        // Convert the base64 string to a byte array to load into the texture
        byte[] imageBytes = Convert.FromBase64String(base64String);

        
        if (!texture.LoadImage(imageBytes))
        {
            Debug.LogError("Failed to load image into texture");
            return;
        }
        
        // upload the updated texture to the GPU
        texture.Apply();
    }
}
