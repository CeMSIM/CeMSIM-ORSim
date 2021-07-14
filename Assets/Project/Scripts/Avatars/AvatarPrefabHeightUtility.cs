using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Finds the height of the avatar's prefab for height calibration
public class AvatarPrefabHeightUtility : MonoBehaviour
{
    [Tooltip("The highest level gameobject in the hierarchy that determines the floor level.")]
    public Transform avatarFloor;
    public Transform avatarEyes;

    public float height;

    public float CalculateHeight()
    {
        height = avatarEyes.position.y - avatarFloor.position.y;
        return height;
    }
}

[CustomEditor(typeof(AvatarPrefabHeightUtility))]
public class AvatarPrefabHeightUtilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        AvatarPrefabHeightUtility heightUtility = target as AvatarPrefabHeightUtility;

        if (!Application.isPlaying && heightUtility.avatarFloor && heightUtility.avatarEyes && GUILayout.Button("Calculate Height"))
        {
            Undo.RecordObject(heightUtility, "Changed avatar height");
            heightUtility.CalculateHeight();
            PrefabUtility.RecordPrefabInstancePropertyModifications(heightUtility);
        }
    }
}