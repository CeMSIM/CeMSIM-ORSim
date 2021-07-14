using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

// Included on the avatar prefab and avatar swapper if the simulation is multiplayer and avatars are synchronized
[Serializable]
public class AvatarNetworkedComponents : MonoBehaviour
{
    public Transform leftController;
    public Transform rightController;
    public Transform hmd;
    [Tooltip("Reference to the calibration script that sends the scale change event.")]
    public AvatarHeightCalibration calibration;

    public void DeepCopy(AvatarNetworkedComponents components)
    {
        leftController = components.leftController;
        rightController = components.rightController;
        hmd = components.hmd;
        calibration = components.calibration;
    }
}
