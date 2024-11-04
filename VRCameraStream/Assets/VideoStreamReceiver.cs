using UnityEngine;


public class SplitStereoWebcamStream : MonoBehaviour
{

    public Material EyeMaterial;   
    private WebCamTexture webCamTexture;      

    [SerializeField] private int cameraIndex = 1;          
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

        // Assign webCamTexture to the materials to be displayed for each eye
        EyeMaterial.mainTexture = webCamTexture;

    }

    private void OnApplicationQuit()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
