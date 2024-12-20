using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;

public class StereoQuadAdjustment : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leftQuadParent;
    [SerializeField] private Transform rightQuadParent;
    [SerializeField] private Camera mainCamera;

    [Header("Reset Positions")]
    [SerializeField] private Vector3 defaultPosition = new Vector3(0.020019f, -0.090027f, 1f);
    [SerializeField] private Vector3 defaultRotation = new Vector3(0f, 180f, 0f);
    [SerializeField] private Vector3 defaultScale = new Vector3(0.0005599085f, 0.0005599085f, 0.0005599085f);

    [Header("HUD Settings")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private float hudScale = 0.003f;
    [SerializeField] private Vector3 hudOffset = new Vector3(0.15f, 0.06f, 0.5f); // Updated HUD position
    private TextMeshProUGUI hudText;
    private GameObject hudObj;

    [Header("Adjustment Settings")]
    [SerializeField] private float rotationSpeed = 0.1f;
    [SerializeField] private float positionSpeed = 1.0f;
    [SerializeField] private float scaleSpeed = 0.1f;
    [SerializeField] private float depthAdjustSpeed = 0.00001f;
    [SerializeField] private float thumbstickDeadzone = 0.1f;

    private InputDevice leftController;
    private InputDevice rightController;

    // Adjustment Modes
    public enum AdjustmentMode
    {
        Position,
        Rotation,
        Depth,
        Speed, // Mode for adjusting speeds
        None // No adjustments
    }

    private AdjustmentMode currentAdjustmentMode = AdjustmentMode.Position;

    // Button States
    private bool previousRightBButtonState = false;
    private bool previousRightAButtonState = false;
    private bool previousLeftXButtonState = false;
    private bool previousLeftYButtonState = false;
    private bool previousRightThumbstickButtonState = false;
    private bool previousLeftThumbstickButtonState = false;
    private bool previousLeftTriggerPressed = false;
    private bool previousRightTriggerPressed = false;

    // Profile Management
    private List<StereoAdjustmentProfile> profiles = new List<StereoAdjustmentProfile>();
    private int currentProfileIndex = 0;
    private string profilesDirectory;

    // HUD Visibility
    private bool isHUDVisible = true;
    private bool showInstructions = false;

    // Grab Manipulation
    private bool isLeftQuadGrabbed = false;
    private bool isRightQuadGrabbed = false;
    private Vector3 leftQuadGrabOffset;
    private Vector3 rightQuadGrabOffset;
    private Quaternion leftQuadGrabRotationOffset;
    private Quaternion rightQuadGrabRotationOffset;
    private Transform freezePoint;
    // Freeze Frames
    private bool framesFrozen = false;
    private Transform leftQuadOriginalParent;
    private Transform rightQuadOriginalParent;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GameObject.Find("LeftEyeCamera")?.GetComponent<Camera>();
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
                if (mainCamera == null)
                {
                    Debug.LogError("No camera found in scene! Please assign a camera in the inspector.");
                    return;
                }
            }
        }

        InitializeControllers();
        CreateHUD();

        // Set profiles directory
        profilesDirectory = Path.Combine(Application.persistentDataPath, "Profiles");

        LoadDefaultProfiles();
        LoadProfilesFromDisk();

        if (profiles.Count > 0)
        {
            LoadProfile(profiles[currentProfileIndex]);
        }
        else
        {
            ResetQuads();
        }
            // Create a freeze point in the scene
        freezePoint = new GameObject("FreezePoint").transform;
        freezePoint.position = mainCamera.transform.position + mainCamera.transform.forward * 1.0f; // 1 meter in front of the camera
        freezePoint.rotation = Quaternion.identity;
    }

    void CreateHUD()
    {
        if (mainCamera == null) return;

        if (hudObj != null)
        {
            Destroy(hudObj);
        }

        hudObj = new GameObject("StatusHUD");
        hudObj.layer = LayerMask.NameToLayer("UI");

        hudObj.transform.parent = mainCamera.transform;
        hudObj.transform.localPosition = hudOffset;
        hudObj.transform.localRotation = Quaternion.identity;
        hudObj.transform.localScale = Vector3.one * hudScale;

        Canvas canvas = hudObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;

        CanvasScaler scaler = hudObj.AddComponent<CanvasScaler>();
        scaler.scaleFactor = 1;
        scaler.dynamicPixelsPerUnit = 100;

        GameObject textObj = new GameObject("HUDText");
        textObj.transform.SetParent(hudObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");

        hudText = textObj.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 8; // Adjusted font size
        hudText.color = activeColor;
        hudText.font = TMP_Settings.defaultFontAsset;
        hudText.alignment = TextAlignmentOptions.Center; // Adjusted alignment
        hudText.text = "STEREO ADJUSTMENT\nInitializing...";

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 400);
    }

    void UpdateHUD()
    {
        if (hudText == null) return;
        if (!isHUDVisible)
        {
            hudText.text = string.Empty;
            return;
        }

        if (showInstructions)
        {
            string instructions = "<color=#00FF00><b>Instructions</b></color>\n";
            instructions += "Press <color=#00FF00>Y Button (Left)</color> to toggle instructions.\n";
            instructions += "Press <color=#00FF00>B Button (Right)</color> to cycle modes.\n";
            instructions += "Press <color=#00FF00>A Button (Right)</color> to save profile.\n";
            instructions += "Press <color=#00FF00>X Button (Left)</color> to reset quads.\n";
            instructions += "Click <color=#00FF00>Left Thumbstick</color> to toggle HUD.\n";
            instructions += "Click <color=#00FF00>Right Thumbstick</color> to create new profile.\n";
            instructions += "Hold <color=#00FF00>Grip</color> to grab quads.\n";
            instructions += "Press <color=#00FF00>Left Trigger</color> to cycle profiles.\n";
            instructions += "Press <color=#00FF00>Right Trigger</color> to freeze/unfreeze frames.\n";
            instructions += "\nPress <color=#00FF00>Y Button (Left)</color> to exit instructions.";
            hudText.text = instructions;
        }
        else
        {
            string status = "<color=#00FF00>STEREO ADJ</color>\n";
            status += $"Profile: {profiles[currentProfileIndex].profileName}\n";
            status += $"Mode: <color=#00FF00>{currentAdjustmentMode}</color>\n";

            if (currentAdjustmentMode == AdjustmentMode.Speed)
            {
                status += $"<align=left>\n";
                status += $"Pos Speed: {positionSpeed:F3}\n";
                status += $"Rot Speed: {rotationSpeed:F3}\n";
                status += $"Scale Speed: {scaleSpeed:F3}\n";
                status += $"Depth Speed: {depthAdjustSpeed:F6}\n";
                status += $"</align>";
            }
            else
            {
                status += $"<align=left>\n";
                status += $"Left Eye:\n";
                status += $"P:{leftQuadParent.localPosition:F3}\n";
                status += $"R:{leftQuadParent.localRotation.eulerAngles:F1}\n";
                status += $"S:{leftQuadParent.localScale:F6}\n";
                status += $"\nRight Eye:\n";
                status += $"P:{rightQuadParent.localPosition:F3}\n";
                status += $"R:{rightQuadParent.localRotation.eulerAngles:F1}\n";
                status += $"S:{rightQuadParent.localScale:F6}\n";
                status += $"</align>";
            }

            status += "\nPress <color=#00FF00>Y Button (Left)</color> for Instructions.";
            hudText.text = status;
        }
    }

    void InitializeControllers()
    {
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftHandDevices);
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightHandDevices);

        if (leftHandDevices.Count > 0) leftController = leftHandDevices[0];
        if (rightHandDevices.Count > 0) rightController = rightHandDevices[0];
    }

    void Update()
    {
        if (!leftController.isValid || !rightController.isValid)
        {
            InitializeControllers();
            return;
        }

        // Handle input to cycle adjustment modes
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBButton); // B Button

        if (rightBButton && !previousRightBButtonState)
        {
            if (showInstructions)
            {
                // Do nothing
            }
            else
            {
                // Cycle adjustment mode
                currentAdjustmentMode = (AdjustmentMode)(((int)currentAdjustmentMode + 1) % System.Enum.GetNames(typeof(AdjustmentMode)).Length);
            }
            UpdateHUD();
        }

        previousRightBButtonState = rightBButton;

        // Toggle HUD visibility
        leftController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool leftThumbstickButton);

        if (leftThumbstickButton && !previousLeftThumbstickButtonState)
        {
            isHUDVisible = !isHUDVisible;
            UpdateHUD();
        }

        previousLeftThumbstickButtonState = leftThumbstickButton;

        // Toggle Instructions
        leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftYButton); // Y Button

        if (leftYButton && !previousLeftYButtonState)
        {
            showInstructions = !showInstructions;
            UpdateHUD();
        }

        previousLeftYButtonState = leftYButton;

        // Handle freezing frames using Right Trigger
        rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue);

        if (rightTriggerValue > 0.8f && !previousRightTriggerPressed)
        {
            Debug.Log("Right trigger pressed - toggling freeze frames");
            ToggleFreezeFrames();
        }

        previousRightTriggerPressed = rightTriggerValue > 0.8f;

        // Handle cycling profiles using Left Trigger
        leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue);

        if (leftTriggerValue > 0.8f && !previousLeftTriggerPressed)
        {
            Debug.Log("Left trigger pressed - cycling profiles");
            CycleProfiles();
        }

        previousLeftTriggerPressed = leftTriggerValue > 0.8f;

        // Adjust quads or speeds
        if (currentAdjustmentMode == AdjustmentMode.Speed)
        {
            AdjustSpeeds();
        }
        else if (currentAdjustmentMode != AdjustmentMode.None)
        {
            AdjustQuad(leftController, leftQuadParent);
            AdjustQuad(rightController, rightQuadParent);
        }

        // Handle saving adjustments
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightAButton); // A Button

        if (rightAButton && !previousRightAButtonState)
        {
            SaveCurrentProfile();
        }

        previousRightAButtonState = rightAButton;

        // Handle resetting adjustments
        leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftXButton); // X Button

        if (leftXButton && !previousLeftXButtonState)
        {
            ResetQuads();
        }

        previousLeftXButtonState = leftXButton;

        // Create new profile
        rightController.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool rightThumbstickButton);

        if (rightThumbstickButton && !previousRightThumbstickButtonState)
        {
            CreateNewProfile();
        }

        previousRightThumbstickButtonState = rightThumbstickButton;

        // Manipulate quads with grip
        ManipulateQuadWithGrip(leftController, leftQuadParent, ref isLeftQuadGrabbed, ref leftQuadGrabOffset, ref leftQuadGrabRotationOffset);
        ManipulateQuadWithGrip(rightController, rightQuadParent, ref isRightQuadGrabbed, ref rightQuadGrabOffset, ref rightQuadGrabRotationOffset);
    }

    private void AdjustQuad(InputDevice controller, Transform quadParent)
    {
        if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            // Apply deadzone
            if (Mathf.Abs(thumbstick.x) < thumbstickDeadzone) thumbstick.x = 0;
            if (Mathf.Abs(thumbstick.y) < thumbstickDeadzone) thumbstick.y = 0;

            switch (currentAdjustmentMode)
            {
                case AdjustmentMode.Position:
                    Vector3 movement = new Vector3(thumbstick.x, thumbstick.y, 0) * (positionSpeed * 0.001f);
                    quadParent.localPosition += movement;
                    break;
                case AdjustmentMode.Rotation:
                    Vector3 currentRotation = quadParent.localRotation.eulerAngles;
                    float zRotationDelta = thumbstick.x * rotationSpeed;
                    currentRotation.z += zRotationDelta;
                    quadParent.localRotation = Quaternion.Euler(currentRotation);
                    break;
                case AdjustmentMode.Depth:
                    float depthChange = thumbstick.y * depthAdjustSpeed;
                    Vector3 currentPosition = quadParent.localPosition;
                    Vector3 currentScale = quadParent.localScale;

                    currentPosition.z += depthChange;

                    float scaleAdjustment = -depthChange * scaleSpeed;
                    Vector3 newScale = currentScale + new Vector3(scaleAdjustment, scaleAdjustment, 0);

                    if (newScale.x >= 0.0001f && newScale.y >= 0.0001f)
                    {
                        quadParent.localPosition = currentPosition;
                        quadParent.localScale = newScale;
                    }

                    float lateralMove = thumbstick.x * positionSpeed * 0.001f;
                    quadParent.localPosition += new Vector3(lateralMove, 0, 0);
                    break;
            }
        }

        UpdateHUD();
    }

    private void AdjustSpeeds()
    {
        // Left controller adjusts Position and Rotation speeds
        if (leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstick))
        {
            // Apply deadzone
            if (Mathf.Abs(leftThumbstick.x) < thumbstickDeadzone) leftThumbstick.x = 0;
            if (Mathf.Abs(leftThumbstick.y) < thumbstickDeadzone) leftThumbstick.y = 0;

            positionSpeed += leftThumbstick.y * 0.01f;
            rotationSpeed += leftThumbstick.x * 0.01f;

            positionSpeed = Mathf.Clamp(positionSpeed, 0.001f, 10f);
            rotationSpeed = Mathf.Clamp(rotationSpeed, 0.01f, 10f);
        }

        // Right controller adjusts Scale and Depth speeds
        if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightThumbstick))
        {
            // Apply deadzone
            if (Mathf.Abs(rightThumbstick.x) < thumbstickDeadzone) rightThumbstick.x = 0;
            if (Mathf.Abs(rightThumbstick.y) < thumbstickDeadzone) rightThumbstick.y = 0;

            scaleSpeed += rightThumbstick.y * 0.01f;
            depthAdjustSpeed += rightThumbstick.x * 0.000001f;

            scaleSpeed = Mathf.Clamp(scaleSpeed, 0.001f, 10f);
            depthAdjustSpeed = Mathf.Clamp(depthAdjustSpeed, 0.000001f, 0.1f);
        }

        UpdateHUD();
    }

    private void ResetQuads()
    {
        if (leftQuadParent != null)
        {
            leftQuadParent.localPosition = defaultPosition;
            leftQuadParent.localRotation = Quaternion.Euler(defaultRotation);
            leftQuadParent.localScale = defaultScale;
            Debug.Log($"Reset left quad to - Position: {leftQuadParent.localPosition:F6}, Rotation: {leftQuadParent.localRotation.eulerAngles:F6}, Scale: {leftQuadParent.localScale:F10}");
        }

        if (rightQuadParent != null)
        {
            rightQuadParent.localPosition = defaultPosition;
            rightQuadParent.localRotation = Quaternion.Euler(defaultRotation);
            rightQuadParent.localScale = defaultScale;
            Debug.Log($"Reset right quad to - Position: {rightQuadParent.localPosition:F6}, Rotation: {rightQuadParent.localRotation.eulerAngles:F6}, Scale: {rightQuadParent.localScale:F10}");
        }

        currentAdjustmentMode = AdjustmentMode.Position;
        UpdateHUD();
    }

    private void SaveCurrentProfile()
    {
        StereoAdjustmentProfile profile = new StereoAdjustmentProfile
        {
            profileName = profiles[currentProfileIndex].profileName,
            leftQuadPosition = leftQuadParent.localPosition,
            leftQuadRotation = leftQuadParent.localRotation.eulerAngles,
            leftQuadScale = leftQuadParent.localScale,

            rightQuadPosition = rightQuadParent.localPosition,
            rightQuadRotation = rightQuadParent.localRotation.eulerAngles,
            rightQuadScale = rightQuadParent.localScale,

            positionSpeed = this.positionSpeed,
            rotationSpeed = this.rotationSpeed,
            scaleSpeed = this.scaleSpeed,
            depthAdjustSpeed = this.depthAdjustSpeed
        };

        profiles[currentProfileIndex] = profile;
        SaveProfileToFile(profile);

        Debug.Log($"Profile '{profile.profileName}' saved.");
    }

    private void SaveProfileToFile(StereoAdjustmentProfile profile)
    {
        string json = JsonUtility.ToJson(profile, true);
        string path = Path.Combine(profilesDirectory, $"{profile.profileName}.json");
        Directory.CreateDirectory(profilesDirectory);
        File.WriteAllText(path, json);
        Debug.Log($"Profile '{profile.profileName}' saved to {path}");
    }

    private void LoadProfilesFromDisk()
    {
        profiles.Clear();
        Directory.CreateDirectory(profilesDirectory);
        string[] files = Directory.GetFiles(profilesDirectory, "*.json");
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            StereoAdjustmentProfile profile = JsonUtility.FromJson<StereoAdjustmentProfile>(json);
            profiles.Add(profile);
            Debug.Log($"Loaded profile '{profile.profileName}'");
        }
    }

    private void LoadProfile(StereoAdjustmentProfile profile)
    {
        leftQuadParent.localPosition = profile.leftQuadPosition;
        leftQuadParent.localRotation = Quaternion.Euler(profile.leftQuadRotation);
        leftQuadParent.localScale = profile.leftQuadScale;

        rightQuadParent.localPosition = profile.rightQuadPosition;
        rightQuadParent.localRotation = Quaternion.Euler(profile.rightQuadRotation);
        rightQuadParent.localScale = profile.rightQuadScale;

        this.positionSpeed = profile.positionSpeed;
        this.rotationSpeed = profile.rotationSpeed;
        this.scaleSpeed = profile.scaleSpeed;
        this.depthAdjustSpeed = profile.depthAdjustSpeed;

        UpdateHUD();
    }

    private void CreateNewProfile()
    {
        string newProfileName = $"Profile_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
        StereoAdjustmentProfile newProfile = new StereoAdjustmentProfile
        {
            profileName = newProfileName,
            leftQuadPosition = leftQuadParent.localPosition,
            leftQuadRotation = leftQuadParent.localRotation.eulerAngles,
            leftQuadScale = leftQuadParent.localScale,

            rightQuadPosition = rightQuadParent.localPosition,
            rightQuadRotation = rightQuadParent.localRotation.eulerAngles,
            rightQuadScale = rightQuadParent.localScale,

            positionSpeed = this.positionSpeed,
            rotationSpeed = this.rotationSpeed,
            scaleSpeed = this.scaleSpeed,
            depthAdjustSpeed = this.depthAdjustSpeed
        };

        profiles.Add(newProfile);
        currentProfileIndex = profiles.Count - 1;
        SaveProfileToFile(newProfile);

        UpdateHUD();
        Debug.Log($"Created new profile '{newProfile.profileName}'");
    }

    private void CycleProfiles()
    {
        if (profiles.Count == 0)
        {
            Debug.LogWarning("No profiles available to cycle through.");
            return;
        }

        currentProfileIndex = (currentProfileIndex + 1) % profiles.Count;
        LoadProfile(profiles[currentProfileIndex]);
        Debug.Log($"Switched to profile '{profiles[currentProfileIndex].profileName}'");
    }

    [Serializable]
    public class StereoAdjustmentProfile
    {
        public string profileName;

        public Vector3 leftQuadPosition;
        public Vector3 leftQuadRotation;
        public Vector3 leftQuadScale;

        public Vector3 rightQuadPosition;
        public Vector3 rightQuadRotation;
        public Vector3 rightQuadScale;

        public float positionSpeed;
        public float rotationSpeed;
        public float scaleSpeed;
        public float depthAdjustSpeed;
    }

    private void ManipulateQuadWithGrip(InputDevice controller, Transform quadParent, ref bool isQuadGrabbed, ref Vector3 grabOffset, ref Quaternion grabRotationOffset)
    {
        controller.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton);

        if (gripButton)
        {
            if (!isQuadGrabbed)
            {
                // Begin grabbing
                if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition) &&
                    controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
                {
                    isQuadGrabbed = true;
                    // Calculate offset between controller and quad
                    grabOffset = quadParent.position - controllerPosition;
                    grabRotationOffset = Quaternion.Inverse(controllerRotation) * quadParent.rotation;
                }
            }
            else
            {
                // Continue grabbing
                if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition) &&
                    controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
                {
                    quadParent.position = controllerPosition + grabOffset;
                    quadParent.rotation = controllerRotation * grabRotationOffset;
                }
            }
        }
        else
        {
            isQuadGrabbed = false;
        }
    }

    private void ToggleFreezeFrames()
    {
        framesFrozen = !framesFrozen;

        if (framesFrozen)
        {
            // Store original parent
            leftQuadOriginalParent = leftQuadParent.parent;
            rightQuadOriginalParent = rightQuadParent.parent;

            // Set freeze point to current position of quads
            freezePoint.position = leftQuadParent.position;
            freezePoint.rotation = leftQuadParent.rotation;

            // Reparent quads to freeze point
            leftQuadParent.parent = freezePoint;
            rightQuadParent.parent = freezePoint;

            Debug.Log("Frames frozen. Quads are now attached to freeze point.");
        }
        else
        {
            // Reattach quads to their original parent
            leftQuadParent.parent = leftQuadOriginalParent;
            rightQuadParent.parent = rightQuadOriginalParent;

            Debug.Log("Frames unfrozen. Quads are now attached to the camera.");
        }
    }


    void OnEnable()
    {
        InputDevices.deviceConnected += RegisterDevice;
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= RegisterDevice;
    }

    private void RegisterDevice(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.Left) != 0)
            leftController = device;
        else if ((device.characteristics & InputDeviceCharacteristics.Right) != 0)
            rightController = device;
    }

    private void OnValidate()
    {
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (defaultScale.x == 1f)
        {
            defaultScale = new Vector3(0.0005599085f, 0.0005599085f, 0.0005599085f);
            Debug.Log("Corrected default scale values");
        }
    }

    private void LoadDefaultProfiles()
    {
        // Copy default profiles from Assets folder to persistent data path on first run
        string defaultProfilesPath = Path.Combine(Application.streamingAssetsPath, "Profiles");
        if (Directory.Exists(defaultProfilesPath))
        {
            Directory.CreateDirectory(profilesDirectory);

            string[] files = Directory.GetFiles(defaultProfilesPath, "*.json");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(profilesDirectory, fileName);
                if (!File.Exists(destFile))
                {
                    File.Copy(file, destFile);
                    Debug.Log($"Copied default profile '{fileName}' to '{destFile}'");
                }
            }
        }
        else
        {
            Debug.LogWarning("No default profiles found in StreamingAssets/Profiles");
        }
    }
}
