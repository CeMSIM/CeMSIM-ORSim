using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using UnityEngine.Events;

// Sets the scale of the avatar based on the standing height of the user
[RequireComponent(typeof(AvatarPrefabHeightUtility))]
public class AvatarHeightCalibration : MonoBehaviour
{
    public VRIK ik;
    public AvatarPrefabHeightUtility avatarHeightUtility;

    [Tooltip("A multiplier to the scale of the avatar. Used if the avatar is too short or tall after calibration.")]
    [Range(0.8f, 1.2f)] public float heightAdjustmentMultiplier = 1f;

    [HideInInspector] public UserHeightUtility userHeightUtility;
    private float calibrationScaleMultiplier = 1;
    private Vector3 startingScale;
    private float startingFootDistance;
    private float startingStepThreshold;

    public UnityEvent<float> OnAvatarHeightChanged; // Sends new avatar scale, foot distance, and step threshold 
                                                    // as a single multiplier when calibrated. Made to send client data to server

    void Start()
    {
        if(!avatarHeightUtility)
            avatarHeightUtility = GetComponentInChildren<AvatarPrefabHeightUtility>();

        // Store the initial data for later use
        startingScale = transform.localScale;
        startingFootDistance = ik.solver.locomotion.footDistance;
        startingStepThreshold = ik.solver.locomotion.stepThreshold;

        Calibrate();
    }

    public void Calibrate()
    {
        // New scale is based on the height difference between the user height and avatar height
        float newScale = userHeightUtility.height / avatarHeightUtility.height;

        calibrationScaleMultiplier = newScale;
        Calibrate(calibrationScaleMultiplier);

        OnAvatarHeightChanged.Invoke(calibrationScaleMultiplier);
    }

    // Use a given height scale to resize the avatar, used for resizing avatar on server/non-local client
    public void Calibrate(float scaleMultiplier)
    {
        calibrationScaleMultiplier = scaleMultiplier;
        float totalMultiplier = calibrationScaleMultiplier * heightAdjustmentMultiplier;

        transform.localScale = startingScale * totalMultiplier;
        ik.solver.locomotion.footDistance = startingFootDistance * totalMultiplier;
        ik.solver.locomotion.stepThreshold = startingStepThreshold * totalMultiplier;
    }
}