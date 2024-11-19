using UnityEngine;
using System.IO;
using System.Xml;

public class SplitStereoWebcamStream : MonoBehaviour
{
    public Material EyeMaterial;   
    private WebCamTexture webCamTexture;      

    [Header("Camera Settings")]
    [SerializeField] private int cameraIndex = 1;          
    [SerializeField] private int requestedWidth = 2560;    // Width of the full stereo frame (both eyes combined)
    [SerializeField] private int requestedHeight = 720;   
    [SerializeField] private int requestedFPS = 30;    

    [Header("Calibration")]
    [SerializeField] private string calibrationFilePath = "stereoMap.xml";
    [SerializeField] private bool useCalibrationFile = false;

    // Camera calibration parameters - using the right eye calibration for both
    private float k1 = -0.23657366f;
    private float k2 = 0.04110457f;
    private float k3 = -0.00263397f;
    private float p1 = -0.00055951f;
    private float p2 = -0.00124154f;
    private float fx = 497.50002164f;
    private float fy = 502.86627903f;
    private float cx = 687.2781095f;
    private float cy = 388.52699115f;

    private Texture2D leftRectMap;
    private Texture2D rightRectMap;

    void Start()
    {
        if (useCalibrationFile)
        {
            LoadCalibrationData();
        }

        InitializeCamera();
    }

    private void LoadCalibrationData()
    {
        string fullPath = Path.Combine(Application.dataPath, calibrationFilePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"Calibration file not found at: {fullPath}, using default parameters");
            return;
        }

        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fullPath);

            // Load rectification maps
            var stereoMapLx = doc.SelectSingleNode("//stereoMapL_x");
            var stereoMapRx = doc.SelectSingleNode("//stereoMapR_x");

            if (stereoMapLx != null && stereoMapRx != null)
            {
                leftRectMap = CreateRectificationTexture(stereoMapLx);
                rightRectMap = CreateRectificationTexture(stereoMapRx);
                Debug.Log("Rectification maps loaded successfully");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading calibration data: {e.Message}");
        }
    }

    private Texture2D CreateRectificationTexture(XmlNode mapNode)
    {
        const int width = 1280;
        const int height = 720;

        // Create a new texture with RGFloat format for high precision
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGFloat, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        // Parse the values from XML
        string[] values = mapNode.InnerText.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                if (idx < values.Length && float.TryParse(values[idx], out float value))
                {
                    // Store the rectification coordinate in R and G channels
                    pixels[idx] = new Color(value, value, 0, 1);
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void InitializeCamera()
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

        // Set rectification maps if available
        if (useCalibrationFile && leftRectMap != null && rightRectMap != null)
        {
            EyeMaterial.SetTexture("_RectMapL", leftRectMap);
            EyeMaterial.SetTexture("_RectMapR", rightRectMap);
            EyeMaterial.SetFloat("_UseRectificationMaps", 1.0f);
            Debug.Log("Calibration maps applied to material");
        }
        else
        {
            EyeMaterial.SetFloat("_UseRectificationMaps", 0.0f);
        }
    }

    private void OnApplicationQuit()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
        {
            webCamTexture.Stop();
        }

        // Clean up textures
        if (leftRectMap != null)
            Destroy(leftRectMap);
        if (rightRectMap != null)
            Destroy(rightRectMap);
    }
}