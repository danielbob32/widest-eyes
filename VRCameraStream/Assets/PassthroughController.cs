using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class PassthroughController : MonoBehaviour
{
    private InputDevice leftController;

    private void Start()
    {
        // Initialize the XR Management system
        XRGeneralSettings.Instance.Manager.InitializeLoader();

        // Get the left controller device
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, inputDevices);
        if (inputDevices.Count > 0)
        {
            leftController = inputDevices[0];
        }
    }

    private void Update()
    {
        CheckPassthroughAvailability();
        HandlePassthroughToggle();
    }

    private void CheckPassthroughAvailability()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader.name == "OpenXR Loader")
        {
            // Passthrough is available, proceed with enabling it
        }
        else
        {
            // Passthrough is not available, handle accordingly
        }
    }

    private void HandlePassthroughToggle()
    {
        if (CheckControllerInteraction(CommonUsages.primaryTouch))
        {
            TogglePassthrough();
        }
    }

    private bool CheckControllerInteraction(InputFeatureUsage<bool> feature)
    {
        bool buttonPressed = false;
        leftController.TryGetFeatureValue(feature, out buttonPressed);
        return buttonPressed;
    }

    private void TogglePassthrough()
    {
        var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
        if (displaySubsystem != null)
        {
            if (displaySubsystem.running)
            {
                displaySubsystem.Stop();
            }
            else
            {
                displaySubsystem.Start();
            }
        }
    }
}

// fix one eye
// blacken each eye
// compare for one eye via pathrough
// fix second eye to align 
// add option to scale / move together