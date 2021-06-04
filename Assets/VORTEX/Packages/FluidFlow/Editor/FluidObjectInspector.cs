using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FluidSimulation
{
    [CustomEditor(typeof(FluidObject))]
    public class FluidObjectInspector : Editor
    {
        private FluidObject fluidObject;

        private string submeshes;
        private int submeshMask;

        private void OnEnable()
        {
            fluidObject = (FluidObject)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fluid Object Data:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base mesh:");
            EditorGUILayout.LabelField(fluidObject.BaseMeshName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Texture Size");
            EditorGUILayout.LabelField(fluidObject.TextureSize.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UV Chanel");
            EditorGUILayout.LabelField(fluidObject.UVChanel.ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Triangle Count");
            EditorGUILayout.LabelField(fluidObject.UnpaddedTriangleCount.ToString());
            EditorGUILayout.EndHorizontal();

            if (submeshMask != fluidObject.SubmeshMask)
            {
                submeshes = MaskToString(fluidObject.SubmeshMask);
                submeshMask = fluidObject.SubmeshMask;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Submesh Mask");
            EditorGUILayout.LabelField(submeshes);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Skeleton Data");
            EditorGUILayout.LabelField(fluidObject.hasSkeletonData().ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Compression: ");
            CompressionState compState = fluidObject.GetCompressionState();
            EditorGUILayout.LabelField(compState == CompressionState.Compressed ? "Compressed" : "Decompressed");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            // Compress button
            if (compState == CompressionState.Compressed)
                GUI.enabled = false;
            if (GUILayout.Button("Compress"))
                fluidObject.CompressEditor();
            GUI.enabled = true;

            // Decompress button
            if (compState == CompressionState.Decompressed)
                GUI.enabled = false;
            if (GUILayout.Button("Decompress"))
                fluidObject.DecompressEditor();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        private string MaskToString(int mask)
        {
            if (mask == -1)
                return "Everything";
            if (mask == 0)
                return "Nothing";

            string res = "";
            bool isFirst = true;

            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) > 0)
                {
                    res += (isFirst ? "" : ", ") + i.ToString();
                    isFirst = false;
                }
            }
            return res;
        }

    }
}