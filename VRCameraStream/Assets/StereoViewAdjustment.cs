using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;
using TMPro;

public class StereoQuadAdjustment : MonoBehaviour
{
    [Header("Quad References")]
    [SerializeField] private Transform leftQuadParent;
    [SerializeField] private Transform rightQuadParent;

    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;

    [Header("Adjustment Settings")]
    [SerializeField] private float positionSpeed = 0.001f;
    [SerializeField] private float rotationSpeed = 30f; // Degrees per second
    [SerializeField] private float scaleSpeed = 0.01f;

    private InputDevice leftController;
    private InputDevice rightController;
    private bool leftAdjustmentActive = false;
    private bool rightAdjustmentActive = false;

    // Variables to detect button presses
    private bool previousLeftPrimaryButtonState = false;
    private bool previousRightPrimaryButtonState = false;

    void Start()
    {
        InitializeControllers();
        CreateStatusText();
        UpdateStatusText();
    }

    void CreateStatusText()
    {
        if (statusText == null)
        {
            // Create a canvas
            GameObject canvasObj = new GameObject("Status Canvas");
            canvasObj.transform.parent = transform;

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Set layer to "UI" or any layer rendered by your VR cameras
            int uiLayer = LayerMask.NameToLayer("UI");
            canvasObj.layer = uiLayer;

            // Position it in front of the user
            canvas.transform.localPosition = new Vector3(0, 0.5f, 2f);
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * 0.01f;

            // Add a canvas scaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.scaleFactor = 1;
            scaler.dynamicPixelsPerUnit = 100;

            // Create text object
            GameObject textObj = new GameObject("Status Text");
            textObj.transform.parent = canvasObj.transform;
            textObj.layer = uiLayer; // Ensure text is on the same layer

            statusText = textObj.AddComponent<TextMeshProUGUI>();
            statusText.fontSize = 16;
            statusText.color = Color.white;
            statusText.alignment = TextAlignmentOptions.Center;

            // Set size and position
            RectTransform rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(600, 200);
            rectTransform.localPosition = Vector3.zero;
        }
    }

    void UpdateStatusText()
    {
        if (statusText != null)
        {
            string status = "";

            if (leftAdjustmentActive)
                status += "Left Eye Adjustment Active\n";
            if (rightAdjustmentActive)
                status += "Right Eye Adjustment Active\n";

            if (!leftAdjustmentActive && !rightAdjustmentActive)
                status = "Press X (Left) or A (Right) to Enable Eye Adjustment\n" +
                         "Thumbstick: Move\n" +
                         "Trigger + Thumbstick X: Rotate\n" +
                         "Grip + Thumbstick Y: Scale";

            statusText.text = status;
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

        // Check button states
        leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButton);
        rightController.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightPrimaryButton);

        // Toggle left adjustment mode
        if (leftPrimaryButton && !previousLeftPrimaryButtonState)
        {
            leftAdjustmentActive = !leftAdjustmentActive;
            UpdateStatusText();
        }

        // Toggle right adjustment mode
        if (rightPrimaryButton && !previousRightPrimaryButtonState)
        {
            rightAdjustmentActive = !rightAdjustmentActive;
            UpdateStatusText();
        }

        // Update previous button states
        previousLeftPrimaryButtonState = leftPrimaryButton;
        previousRightPrimaryButtonState = rightPrimaryButton;

        // Left Eye Adjustment
        if (leftAdjustmentActive)
        {
            AdjustQuad(leftController, leftQuadParent);
        }

        // Right Eye Adjustment
        if (rightAdjustmentActive)
        {
            AdjustQuad(rightController, rightQuadParent);
        }

        // Reset both quads if both adjustment modes are active and both primary buttons are pressed
        if (leftAdjustmentActive && rightAdjustmentActive && leftPrimaryButton && rightPrimaryButton)
        {
            ResetQuads();
        }
    }

    private void AdjustQuad(InputDevice controller, Transform quadParent)
    {
        // Movement with thumbstick
        if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick))
        {
            bool triggerPressed = controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool isTriggerPressed) && isTriggerPressed;
            bool gripPressed = controller.TryGetFeatureValue(CommonUsages.gripButton, out bool isGripPressed) && isGripPressed;

            if (triggerPressed)
            {
                // Rotate with thumbstick X
                quadParent.Rotate(0, 0, -thumbstick.x * rotationSpeed * Time.deltaTime);
            }
            else if (gripPressed)
            {
                // Scale with thumbstick Y
                float scaleChange = thumbstick.y * scaleSpeed;
                Vector3 newScale = quadParent.localScale + new Vector3(scaleChange, scaleChange, scaleChange);

                // Prevent scale from becoming zero or negative
                if (newScale.x > 0.1f && newScale.y > 0.1f && newScale.z > 0.1f)
                {
                    quadParent.localScale = newScale;
                }
            }
            else
            {
                // Move with thumbstick
                quadParent.localPosition += new Vector3(
                    thumbstick.x * positionSpeed,
                    thumbstick.y * positionSpeed,
                    0);
            }
        }
    }

    private void ResetQuads()
    {
        Vector3 defaultScale = new Vector3(1, 1, 1);
        Vector3 defaultRotation = new Vector3(0, 180, 0);

        leftQuadParent.localPosition = new Vector3(0.02f, -0.09f, 1f);
        leftQuadParent.localRotation = Quaternion.Euler(defaultRotation);
        leftQuadParent.localScale = defaultScale;

        rightQuadParent.localPosition = new Vector3(0.02f, -0.09f, 1f);
        rightQuadParent.localRotation = Quaternion.Euler(defaultRotation);
        rightQuadParent.localScale = defaultScale;

        UpdateStatusText();
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
        {
            leftController = device;
        }
        else if ((device.characteristics & InputDeviceCharacteristics.Right) != 0)
        {
            rightController = device;
        }
    }
}
