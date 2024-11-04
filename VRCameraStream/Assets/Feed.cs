
using UnityEngine;

using UnityEngine.UI;

public class Feed : MonoBehaviour
{
    [SerializeField] private RawImage img1 = default;
    [SerializeField] private RawImage img2 = default;
    
    [SerializeField] private int cameraIndex = 1;          
    [SerializeField] private int requestedWidth = 2560;    // Width of the full stereo frame (both eyes combined)
    [SerializeField] private int requestedHeight = 720;   
    [SerializeField] private int requestedFPS = 30;    

    private WebCamTexture webCam;
    // Start is called before the first frame update
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
        webCam = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight, requestedFPS);
        if(!webCam.isPlaying) webCam.Play();
        img1.texture = webCam;
        img2.texture = webCam;
    }

}

