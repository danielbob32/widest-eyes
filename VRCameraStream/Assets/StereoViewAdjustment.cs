using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;
using TMPro;
using System.IO;
using System;
using System.Collections;
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
    [SerializeField] private float hudScale = 0.002f;
    [SerializeField] private Vector3 hudOffset = new Vector3(0.25f, 0.02f, 0.5f);
    [SerializeField] private Color hudTitleColor = new Color(0.1f, 0.1f, 1f); // Cyan-ish
    [SerializeField] private Color hudHighlightColor = new Color(1f, 0.85f, 0.4f); // Warm yellow
    [SerializeField] private Color hudTextColor = new Color(0.6f, 0.8f, 1f); // Almost white
    [SerializeField] private Color hudSubtitleColor = new Color(0.1f, 0.1f, 1f); // Light blue
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

    [Header("Calibration Settings")]
    [SerializeField] private Color calibrationTextColor = new Color(1f, 0.5f, 0.5f); // Pinkish for calibration
    [SerializeField] private Vector3 defaultCalibrationStartPosition = new Vector3(0.025f, -0.090027f, 1f);
    [SerializeField] private float eyeSeparationOffset = 0.05f; // How far apart to move eyes initially

    // Adjustment Modes
    public enum AdjustmentMode
    {
        Move,
        Rotate,
        Depth,
        None
    }

    private AdjustmentMode leftEyeMode = AdjustmentMode.None;
    private AdjustmentMode rightEyeMode = AdjustmentMode.None;

    // Button States
    private bool isInCalibration = false;
    private bool showingCalibrationInstructions = false;
    private bool previousLeftTriggerPressed = false;
    private bool previousRightTriggerPressed = false;
    private bool previousLeftXButtonState = false;
    private bool previousLeftYButtonState = false;
    private bool previousRightAButtonState = false;
    private bool previousRightBButtonState = false;

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
                    Debug.LogError("No camera found in scene!");
                    return;
                }
            }
        }

        InitializeControllers();


        // Set profiles directory
        profilesDirectory = Path.Combine(Application.persistentDataPath, "Profiles");
        Directory.CreateDirectory(profilesDirectory);

        LoadProfilesFromDisk();
        if (profiles.Count == 0)
        {
            CreateDefaultProfile();
        }
        CreateHUD();
        LoadProfile(profiles[currentProfileIndex]);
    }

    private void CreateDefaultProfile()
    {
        StereoAdjustmentProfile defaultProfile = new StereoAdjustmentProfile
        {
            profileName = "Default",
            leftQuadPosition = defaultPosition,
            leftQuadRotation = defaultRotation,
            leftQuadScale = defaultScale,
            rightQuadPosition = defaultPosition,
            rightQuadRotation = defaultRotation,
            rightQuadScale = defaultScale,
            positionSpeed = this.positionSpeed,
            rotationSpeed = this.rotationSpeed,
            scaleSpeed = this.scaleSpeed,
            depthAdjustSpeed = this.depthAdjustSpeed
        };

        profiles.Add(defaultProfile);
        currentProfileIndex = 0;  
        SaveProfileToFile(defaultProfile);
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
        hudObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
        hudObj.transform.localScale = Vector3.one * hudScale;

        Canvas canvas = hudObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;

        // Create shadow text (background)
        GameObject shadowObj = new GameObject("ShadowText");
        shadowObj.transform.SetParent(hudObj.transform, false);
        shadowObj.layer = LayerMask.NameToLayer("UI");

        var shadowText = shadowObj.AddComponent<TextMeshProUGUI>();
        shadowText.fontSize = 8;
        shadowText.color = Color.black;
        shadowText.font = TMP_Settings.defaultFontAsset;
        shadowText.alignment = TextAlignmentOptions.Left;
        shadowText.enableWordWrapping = true;

        // Main text
        GameObject textObj = new GameObject("HUDText");
        textObj.transform.SetParent(hudObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");

        hudText = textObj.AddComponent<TextMeshProUGUI>();
        hudText.fontSize = 8;
        hudText.color = hudTextColor;
        hudText.font = TMP_Settings.defaultFontAsset;
        hudText.alignment = TextAlignmentOptions.Left;
        hudText.enableWordWrapping = true;

        // Position shadow slightly offset
        var shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.sizeDelta = new Vector2(330, 180);
        shadowRect.anchoredPosition = new Vector2(0.5f, -0.5f);

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(330, 180);
        textRect.anchoredPosition = Vector2.zero;

        UpdateHUD();
    }
    void UpdateHUD()
    {
        if (hudText == null || !isHUDVisible)
        {
            if (hudText != null) hudText.text = string.Empty;
            return;
        }

        string coloredText;
        string plainText;

        if (showingCalibrationInstructions)
        {
            coloredText = $"<color=#{ColorUtility.ToHtmlStringRGB(calibrationTextColor)}>CALIBRATION INSTRUCTIONS</color>\n\n";
            coloredText += "1. Place three identical circles on a wall\n";
            coloredText += "   in a horizontal line\n\n";
            coloredText += "2. Stand about 2 meters away from the wall\n";
            coloredText += "3. You will see two offset views\n";
            coloredText += "4. Align them until the circles overlap perfectly\n\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Press A to begin calibration</color>\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Press B to cancel</color>";

            plainText = coloredText.Replace($"<color=#{ColorUtility.ToHtmlStringRGB(calibrationTextColor)}>", "")
                                .Replace($"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>", "")
                                .Replace("</color>", "");
        }
        else if (isInCalibration)
        {
            coloredText = $"<color=#{ColorUtility.ToHtmlStringRGB(calibrationTextColor)}>CALIBRATION IN PROGRESS</color>\n\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Left Eye:</color> {leftEyeMode}\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Right Eye:</color> {rightEyeMode}\n\n";
            coloredText += "1. Use triggers to change modes\n";
            coloredText += "2. Use thumbsticks to adjust\n";
            coloredText += "3. Align the circles to overlap\n\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Press A when alignment is perfect</color>\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Press B to cancel</color>";

            plainText = coloredText.Replace($"<color=#{ColorUtility.ToHtmlStringRGB(calibrationTextColor)}>", "")
                                .Replace($"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>", "")
                                .Replace("</color>", "");
        }
        else
        {
            // Your existing normal HUD code here
            coloredText = $"<color=#{ColorUtility.ToHtmlStringRGB(hudTitleColor)}>STEREO ADJUSTMENT</color>\n\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Left Eye:</color> {leftEyeMode}\n";
            coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Right Eye:</color> {rightEyeMode}\n";
            coloredText += $"\n<color=#{ColorUtility.ToHtmlStringRGB(hudTextColor)}>Hold Both Triggers for Controls</color>\n";
            coloredText += $"\n<color=#{ColorUtility.ToHtmlStringRGB(hudHighlightColor)}>Hold Y to start calibration</color>";

            plainText = "STEREO ADJUSTMENT\n\n";
            plainText += $"Left Eye: {leftEyeMode}\n";
            plainText += $"Right Eye: {rightEyeMode}\n";
            plainText += "\nHold Both Triggers for Controls\n";
            plainText += "\nHold Y to start calibration";
        }

        hudText.text = coloredText;
        var shadowText = hudObj.transform.Find("ShadowText")?.GetComponent<TextMeshProUGUI>();
        if (shadowText != null)
        {
            shadowText.text = plainText;
        }
    }

    void InitializeControllers()
    {
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, 
            leftHandDevices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, 
            rightHandDevices);

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

        // Handle Mode Changes
        HandleModeChanges();

        // Handle adjustments
        if (leftEyeMode != AdjustmentMode.None)
        {
            AdjustQuad(leftController, leftQuadParent, leftEyeMode);
        }
        
        if (rightEyeMode != AdjustmentMode.None)
        {
            AdjustQuad(rightController, rightQuadParent, rightEyeMode);
        }

        // Check for both triggers pressed to show instructions
        leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue);
        rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue);
        
        if (leftTriggerValue > 0.8f && rightTriggerValue > 0.8f)
        {
            if (!showInstructions)
            {
                showInstructions = true;
                UpdateHUD();
            }
        }
        else if (showInstructions)
        {
            showInstructions = false;
            UpdateHUD();
        }

        // Handle additional controls
        HandleAdditionalControls();
    }

    private void HandleModeChanges()
    {
        // Left Eye Mode Change
        leftController.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue);
        if (leftTriggerValue > 0.8f && !previousLeftTriggerPressed)
        {
            leftEyeMode = (AdjustmentMode)(((int)leftEyeMode + 1) % (int)AdjustmentMode.None);
            UpdateHUD();
        }
        previousLeftTriggerPressed = leftTriggerValue > 0.8f;

        // Right Eye Mode Change
        rightController.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue);
        if (rightTriggerValue > 0.8f && !previousRightTriggerPressed)
        {
            rightEyeMode = (AdjustmentMode)(((int)rightEyeMode + 1) % (int)AdjustmentMode.None);
            UpdateHUD();
        }
        previousRightTriggerPressed = rightTriggerValue > 0.8f;
    }


    private void HandleAdditionalControls()
    {
        // Get all button states
        leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftYButton);
        leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftXButton);
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightAButton);
        rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBButton);

        if (!isInCalibration && !showingCalibrationInstructions)
        {
            // Normal mode controls
            
            // Toggle Instructions with long press for calibration
            if (leftYButton && !previousLeftYButtonState)
            {
                StartCoroutine(CheckForCalibrationStart());
            }

            // Save Profile
            if (rightAButton && !previousRightAButtonState)
            {
                SaveCurrentProfile();
            }

            // Reset Position
            if (leftXButton && !previousLeftXButtonState)
            {
                ResetQuads();
            }

            // New Profile
            if (rightBButton && !previousRightBButtonState)
            {
                CreateNewProfile();
            }
        }
        else if (showingCalibrationInstructions)
        {
            // Calibration instructions mode
            if (rightAButton && !previousRightAButtonState)
            {
                BeginActualCalibration();
            }
            else if (rightBButton && !previousRightBButtonState)
            {
                showingCalibrationInstructions = false;
                UpdateHUD();
            }
        }
        else if (isInCalibration)
        {
            // Active calibration mode
            if (rightAButton && !previousRightAButtonState)
            {
                FinishCalibration();
            }
            else if (rightBButton && !previousRightBButtonState)
            {
                isInCalibration = false;
                ResetQuads();
                UpdateHUD();
            }
        }

        // Update previous button states
        previousLeftYButtonState = leftYButton;
        previousLeftXButtonState = leftXButton;
        previousRightAButtonState = rightAButton;
        previousRightBButtonState = rightBButton;

        // Handle quad grabbing (always active)
        ManipulateQuadWithGrip(leftController, leftQuadParent, ref isLeftQuadGrabbed, 
            ref leftQuadGrabOffset, ref leftQuadGrabRotationOffset);
        ManipulateQuadWithGrip(rightController, rightQuadParent, ref isRightQuadGrabbed, 
            ref rightQuadGrabOffset, ref rightQuadGrabRotationOffset);
    }



    // Add this coroutine to check for long press
    private IEnumerator CheckForCalibrationStart()
    {
        float holdTime = 0f;
        bool buttonStillHeld = true;

        while (holdTime < 1.0f && buttonStillHeld) // 1 second hold time
        {
            holdTime += Time.deltaTime;
            leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out buttonStillHeld);
            yield return null;
        }

        if (buttonStillHeld)
        {
            // Long press detected - start calibration
            StartCalibration();
        }
        else
        {
            // Short press - toggle instructions
            showInstructions = !showInstructions;
            UpdateHUD();
        }
    }

    private void AdjustQuad(InputDevice controller, Transform quadParent, AdjustmentMode mode)
    {
        if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            // Apply deadzone
            if (Mathf.Abs(thumbstick.x) < thumbstickDeadzone) thumbstick.x = 0;
            if (Mathf.Abs(thumbstick.y) < thumbstickDeadzone) thumbstick.y = 0;

            switch (mode)
            {
                case AdjustmentMode.Move:
                    Vector3 movement = new Vector3(thumbstick.x, thumbstick.y, 0) * (positionSpeed * 0.001f);
                    quadParent.localPosition += movement;
                    break;

                case AdjustmentMode.Rotate:
                    Vector3 currentRotation = quadParent.localRotation.eulerAngles;
                    float zRotationDelta = thumbstick.x * rotationSpeed;
                    currentRotation.z += zRotationDelta;
                    quadParent.localRotation = Quaternion.Euler(currentRotation);
                    break;

                case AdjustmentMode.Depth:
                    float scaleChange = thumbstick.y * scaleSpeed * 0.0001f; // Made slower
                    Vector3 newScale = quadParent.localScale + new Vector3(scaleChange, scaleChange, 0);
                    if (newScale.x >= 0.00001f && newScale.y >= 0.00001f)
                    {
                        quadParent.localScale = newScale;
                        // Also adjust position to maintain apparent size
                        float depthChange = -scaleChange * 10f; // Proportional depth adjustment
                        quadParent.localPosition += new Vector3(0, 0, depthChange);
                    }

                    // Allow horizontal movement while in depth mode
                    float lateralMove = thumbstick.x * positionSpeed * 0.001f;
                    quadParent.localPosition += new Vector3(lateralMove, 0, 0);
                    break; ;
            }
            UpdateHUD();
        }
    }

    private void ManipulateQuadWithGrip(InputDevice controller, Transform quadParent, ref bool isQuadGrabbed, 
        ref Vector3 grabOffset, ref Quaternion grabRotationOffset)
    {
        controller.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButton);
        controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        bool grabActive = gripButton && triggerValue > 0.8f;

        if (grabActive)
        {
            if (!isQuadGrabbed)
            {
                if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 controllerPosition) &&
                    controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion controllerRotation))
                {
                    isQuadGrabbed = true;
                    grabOffset = quadParent.position - controllerPosition;
                    grabRotationOffset = Quaternion.Inverse(controllerRotation) * quadParent.rotation;
                }
            }
            else
            {
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

    private void ResetQuads()
    {
        if (leftQuadParent != null)
        {
            leftQuadParent.localPosition = defaultPosition;
            leftQuadParent.localRotation = Quaternion.Euler(defaultRotation);
            leftQuadParent.localScale = defaultScale;
        }

        if (rightQuadParent != null)
        {
            rightQuadParent.localPosition = defaultPosition;
            rightQuadParent.localRotation = Quaternion.Euler(defaultRotation);
            rightQuadParent.localScale = defaultScale;
        }

        leftEyeMode = AdjustmentMode.Move;
        rightEyeMode = AdjustmentMode.Move;
        UpdateHUD();
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

    private void SaveCurrentProfile()
    {
        if (profiles.Count == 0 || currentProfileIndex >= profiles.Count)
        {
            CreateDefaultProfile();
        }
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

        positionSpeed = profile.positionSpeed;
        rotationSpeed = profile.rotationSpeed;
        scaleSpeed = profile.scaleSpeed;
        depthAdjustSpeed = profile.depthAdjustSpeed;

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

    private void StartCalibration()
    {
        showingCalibrationInstructions = true;
        UpdateHUD();
    }

    private void BeginActualCalibration()
    {
        showingCalibrationInstructions = false;
        isInCalibration = true;
        
        // Set eyes apart for calibration
        leftQuadParent.localPosition = defaultCalibrationStartPosition + Vector3.left * eyeSeparationOffset;
        rightQuadParent.localPosition = defaultCalibrationStartPosition + Vector3.right * eyeSeparationOffset;
        
        // Reset rotations
        leftQuadParent.localRotation = Quaternion.Euler(defaultRotation);
        rightQuadParent.localRotation = Quaternion.Euler(defaultRotation);
        
        // Set initial modes
        leftEyeMode = AdjustmentMode.Move;
        rightEyeMode = AdjustmentMode.Move;
        
        UpdateHUD();
    }

    private void FinishCalibration()
    {
        isInCalibration = false;
        CreateNewProfile();
        UpdateHUD();
    }
}