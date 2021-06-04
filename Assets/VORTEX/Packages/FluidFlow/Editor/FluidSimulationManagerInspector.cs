using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FluidSimulation
{
    [CustomEditor(typeof(FluidSimulationManager))]
    [CanEditMultipleObjects]
    public class FluidSimulationManagerInspector : Editor
    {
        private SerializedProperty updateModeProp;
        private SerializedProperty skipFramesProp;
        private SerializedProperty cyclesPerSecondProp;
        private SerializedProperty maxUpdatesPerCycleProp;
        private SerializedProperty onlyVisibleProp;
        private SerializedProperty minEvapProp;
        private SerializedProperty useIntRtProp;

        private void OnEnable()
        {
            updateModeProp = serializedObject.FindProperty("updateMode");
            skipFramesProp = serializedObject.FindProperty("skipFrames");
            cyclesPerSecondProp = serializedObject.FindProperty("cyclesPerSecond");
            maxUpdatesPerCycleProp = serializedObject.FindProperty("maximumUpdatesPerCycle");
            onlyVisibleProp = serializedObject.FindProperty("onlyVisible");
            minEvapProp = serializedObject.FindProperty("minimumEvaporationDelta");
            useIntRtProp = serializedObject.FindProperty("useIntegerRendertexture");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(updateModeProp, new GUIContent("Update Mode:", "Frame: Update every X frame; Time: X updates per second"));
            EditorGUI.indentLevel = 1;
            if (updateModeProp.enumValueIndex == (int)FluidSimulationManager.UpdateMode.Frame)
            {
                EditorGUILayout.PropertyField(skipFramesProp, new GUIContent("Skipped Frames:", "Skipped frames between updates"));
            }
            else
            {
                EditorGUILayout.PropertyField(cyclesPerSecondProp, new GUIContent("Cycles Per Second:", "Update cycles per second"));
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(maxUpdatesPerCycleProp, new GUIContent("Maximum Updates:", "Maximum amount of simulators updated in one cycle"));
            EditorGUILayout.PropertyField(onlyVisibleProp, new GUIContent("Only Visible:", "Only simulators visible to a camera will be updated"));
            EditorGUILayout.PropertyField(minEvapProp, new GUIContent("Minimum Evaporation:", "The amount of fluid that has to evaporate before the texture is updated. When the simulator is inactive."));

            GUI.enabled = !Application.isPlaying;
            EditorGUILayout.PropertyField(useIntRtProp, new GUIContent("Use Integer RenterTexture:", "Enable/disable integer RenderTextures for platforms, wich don`t support them. (mobile) This might have a small performance impact"));
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }
    }
}