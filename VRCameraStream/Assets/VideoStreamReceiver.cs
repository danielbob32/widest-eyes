using UnityEngine;


public class SplitStereoWebcamStream : MonoBehaviour
{

    public Material leftEyeMaterial;   
    public Material rightEyeMaterial;  


    private WebCamTexture webCamTexture;      
    private RenderTexture leftRenderTexture;  
    private RenderTexture rightRenderTexture; 

    [SerializeField] private int cameraIndex = 2;          
    [SerializeField] private int requestedWidth = 2560;    // Width of the full stereo frame (both eyes combined)
    [SerializeField] private int requestedHeight = 720;   
    [SerializeField] private int requestedFPS = 30;    

    void Start()
    {
        // Get the list of available webcam devices
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)  // If no webcams are detected, log an error and exit
        {
            Debug.LogError("No webcam found on this device.");
            return;
        }

        // Validate and adjust the camera index if it's out of range
        if (cameraIndex >= devices.Length)
        {
            Debug.LogError($"Camera index {cameraIndex} is out of range. Defaulting to first camera.");
            cameraIndex = 0;  // Default to the first camera if the specified index is invalid
        }

        // Initialize WebCamTexture with specific camera, resolution, and frame rate
        webCamTexture = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight, requestedFPS);
        webCamTexture.Play();  // Start capturing from the selected webcam

        // Set up RenderTextures for each eye, splitting the requested width in half for stereo view
        leftRenderTexture = new RenderTexture(requestedWidth / 2, requestedHeight, 24, RenderTextureFormat.Default);
        rightRenderTexture = new RenderTexture(requestedWidth / 2, requestedHeight, 24, RenderTextureFormat.Default);

        // Assign RenderTextures to the materials to be displayed for each eye
        leftEyeMaterial.mainTexture = leftRenderTexture;
        rightEyeMaterial.mainTexture = rightRenderTexture;
    }

    void Update()
    {
        if (webCamTexture.isPlaying)  
        {
            // Render the left half of the webcam feed to the left eye texture
            Graphics.Blit(webCamTexture, leftRenderTexture, new Vector2(0.5f, 1), new Vector2(0, 1));

            // Render the right half of the webcam feed to the right eye texture
            Graphics.Blit(webCamTexture, rightRenderTexture, new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        }
    }

    private void OnApplicationQuit()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
