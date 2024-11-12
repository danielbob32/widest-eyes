using UnityEngine;

public class SplitStereoWebcamStream : MonoBehaviour
{
    public Material EyeMaterial;   
    private WebCamTexture webCamTexture;      

    [SerializeField] private int cameraIndex = 1;          
    [SerializeField] private int requestedWidth = 2560;    // Width of the full stereo frame (both eyes combined)
    [SerializeField] private int requestedHeight = 720;   
    [SerializeField] private int requestedFPS = 30;    

    // Camera calibration parameters - using the right eye calibration for both
    private readonly float k1 = -0.23657366f;
    private readonly float k2 = 0.04110457f;
    private readonly float k3 = -0.00263397f;
    private readonly float p1 = -0.00055951f;
    private readonly float p2 = -0.00124154f;
    private readonly float fx = 497.50002164f;
    private readonly float fy = 502.86627903f;
    private readonly float cx = 687.2781095f;
    private readonly float cy = 388.52699115f;

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

        // Set up the material with distortion parameters
        if (EyeMaterial != null)
        {
            SetupMaterial();
        }
        else
        {
            Debug.LogError("Eye material not assigned!");
        }
    }

    private void SetupMaterial()
    {
        // Set main texture
        EyeMaterial.SetTexture("_MainTex", webCamTexture);

        // Set distortion coefficients
        EyeMaterial.SetFloat("_K1", k1);
        EyeMaterial.SetFloat("_K2", k2);
        EyeMaterial.SetFloat("_K3", k3);
        EyeMaterial.SetFloat("_P1", p1);
        EyeMaterial.SetFloat("_P2", p2);

        // Set camera matrix parameters
        EyeMaterial.SetFloat("_Fx", fx);
        EyeMaterial.SetFloat("_Fy", fy);
        EyeMaterial.SetFloat("_Cx", cx);
        EyeMaterial.SetFloat("_Cy", cy);
    }

    private void OnApplicationQuit()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }
    }
}