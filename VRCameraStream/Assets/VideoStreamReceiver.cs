using UnityEngine;

public class SplitStereoWebcamStream : MonoBehaviour
{
    public Material LeftEyeMaterial;
    public Material RightEyeMaterial;
    private WebCamTexture webCamTexture;

    [SerializeField] private int cameraIndex = 1;
    [SerializeField] private int requestedWidth = 2560;    // Full stereo frame width (both eyes combined)
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;

    void Start()
    {
        
        // Get the list of available webcam devices
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("No webcam found on this device.");
            return;
        }

        // Validate and adjust the camera index if it's out of range
        if (cameraIndex >= devices.Length)
        {
            Debug.LogError($"Camera index {cameraIndex} is out of range. Defaulting to first camera.");
            cameraIndex = 0;
        }

        // Initialize WebCamTexture with specific camera, resolution, and frame rate
        webCamTexture = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight, requestedFPS);
        webCamTexture.Play();  // Start capturing from the selected webcam

        // Assign webCamTexture to the materials for each eye
        LeftEyeMaterial.mainTexture = webCamTexture;
        RightEyeMaterial.mainTexture = webCamTexture;
        // Left Eye Material Properties
        LeftEyeMaterial.SetFloat("_K1", 0f);
        LeftEyeMaterial.SetFloat("_K2", 0f);
        LeftEyeMaterial.SetFloat("_P1", 0f);
        LeftEyeMaterial.SetFloat("_P2", 0f);
        LeftEyeMaterial.SetFloat("_CenterX", 0.5f);
        LeftEyeMaterial.SetFloat("_CenterY", 0.5f);

        // Right Eye Material Properties
        RightEyeMaterial.SetFloat("_K1", 0f);
        RightEyeMaterial.SetFloat("_K2", 0f);
        RightEyeMaterial.SetFloat("_P1", 0f);
        RightEyeMaterial.SetFloat("_P2", 0f);
        RightEyeMaterial.SetFloat("_CenterX", 0.5f);
        RightEyeMaterial.SetFloat("_CenterY", 0.5f);

    }

    private void OnApplicationQuit()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}
