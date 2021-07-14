using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RootMotion.FinalIK;

public class UserHeightUtility : MonoBehaviour
{
    [Tooltip("The object that is located at the floor level.")]
    public Transform floor;
    [Tooltip("The object that follows the user camera.")]
    public new Transform camera;

    public float height;

    public void CalculateUserHeight()
    {
        height = camera.position.y - floor.position.y;
    }
}

[CustomEditor(typeof(UserHeightUtility))]
public class UserHeightUtilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UserHeightUtility userHeightUtility = target as UserHeightUtility;

        if (Application.isPlaying && userHeightUtility.floor && userHeightUtility.camera && GUILayout.Button("Calculate Height"))
            (target as UserHeightUtility).CalculateUserHeight();
    }
}
