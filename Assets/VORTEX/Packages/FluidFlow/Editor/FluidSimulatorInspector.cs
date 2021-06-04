using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FluidSimulation
{
    [CustomEditor(typeof(FluidSimulator))]
    [CanEditMultipleObjects]
    public class FluidSimulatorInspector : Editor
    {
        private FluidSimulator simulator;

        private SerializedProperty fluidObjectProp;
        private SerializedProperty rendererProp;
        private SerializedProperty timeoutProp;
        private SerializedProperty speedProp;
        private SerializedProperty normalMapProp;
        private SerializedProperty normalProp;
        private SerializedProperty slopeProp;
        private SerializedProperty roughnessProp;
        private SerializedProperty dryTimeProp;
        private SerializedProperty useEvapProp;
        private SerializedProperty evapAmountProp;
        private SerializedProperty dripCheckProp;
        private SerializedProperty minDripProp;
        private SerializedProperty dropSkipProp;
        private SerializedProperty onDropPorp;

        private void OnEnable()
        {
            fluidObjectProp = serializedObject.FindProperty("fluidObject");
            rendererProp = serializedObject.FindProperty("fluidRenderer");
            timeoutProp = serializedObject.FindProperty("timeout");
            speedProp = serializedObject.FindProperty("speed");
            normalMapProp = serializedObject.FindProperty("normalMap");
            normalProp = serializedObject.FindProperty("normal");
            slopeProp = serializedObject.FindProperty("slope");
            roughnessProp = serializedObject.FindProperty("roughness");
            dryTimeProp = serializedObject.FindProperty("dryTime");
            useEvapProp = serializedObject.FindProperty("useFluidEvapoation");
            evapAmountProp = serializedObject.FindProperty("evaporationPerSecond");
            dripCheckProp = serializedObject.FindProperty("checkForDrops");
            minDripProp = serializedObject.FindProperty("minimumDripAmount");
            dropSkipProp = serializedObject.FindProperty("dropUpdateSkipAmount");
            onDropPorp = serializedObject.FindProperty("OnFluidDrip");

            if(rendererProp.exposedReferenceValue == null)
            {
                simulator = (FluidSimulator)target;
                Renderer renderer = simulator.GetComponent<Renderer>();
                if(renderer != null)
                {
                    rendererProp.objectReferenceValue = renderer;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);

            EditorGUI.indentLevel = 1;
            EditorGUILayout.PropertyField(fluidObjectProp, new GUIContent("Fluid Object:", "Fluid data for the object"));
            EditorGUILayout.PropertyField(rendererProp, new GUIContent("Renderer:", "Renderer used to render the fluid"));
            EditorGUILayout.PropertyField(timeoutProp, new GUIContent("Timeout:", "Fluid Updates until timeout"));
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Flow", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(speedProp, new GUIContent("Flowspeed:", "Flowspeed of the fluid. Values >1.5 might cause errors."));
            EditorGUILayout.PropertyField(roughnessProp, new GUIContent("Roughness:", "How much fluid does one texel hold"));
            EditorGUILayout.PropertyField(normalMapProp, new GUIContent("Normalmap:", "Surface structure of the object. Using the UV chanel set in the fluid mesh."));
            if (normalMapProp.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(normalProp, new GUIContent("Normalmap Amount:", "How much does the surface structure affect fluid flow."));
            }
            EditorGUILayout.PropertyField(slopeProp, new GUIContent("Slope Amount:", "How much does the slope affect fluid height."));
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                simulator.UpdateFlowMap();
            }
            EditorGUILayout.PropertyField(dryTimeProp, new GUIContent("Dry Time:", "How fast does the fluid dry. Drying will only update as long as the Simulator is active!"));
            EditorGUI.indentLevel = 0;
            EditorGUILayout.Space();

            FontStyle tmp = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = FontStyle.Bold;
            useEvapProp.boolValue = EditorGUILayout.Toggle("Evaporation", useEvapProp.boolValue, EditorStyles.toggle);
            EditorStyles.label.fontStyle = tmp;

            if (useEvapProp.boolValue)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(evapAmountProp, new GUIContent("Amount:", "Fluid evaporation per second."));
            }
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            EditorStyles.label.fontStyle = FontStyle.Bold;
            dripCheckProp.boolValue = EditorGUILayout.Toggle("Check for Drops", dripCheckProp.boolValue, EditorStyles.toggle);
            EditorStyles.label.fontStyle = tmp;
            
            if (dripCheckProp.boolValue)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(minDripProp, new GUIContent("Minimum Fluid Amount:", "How much fluid is needed for one drop"));
                EditorGUILayout.PropertyField(dropSkipProp, new GUIContent("Updates Skipped:", "Check for drops every x fluid updates"));
                EditorGUILayout.PropertyField(onDropPorp, new GUIContent("OnDrop", "Called when fluid drips off the object."));
                EditorGUI.indentLevel = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}