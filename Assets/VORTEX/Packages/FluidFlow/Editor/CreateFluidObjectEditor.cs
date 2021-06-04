using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading;

namespace FluidSimulation
{
    public class CreateFluidObjectEditor : EditorWindow
    {

        [MenuItem("Window/Fluid Object Creator")]
        static void Init()
        {
            CreateFluidObjectEditor editorWindow = GetWindow<CreateFluidObjectEditor>("FO Creator");
            editorWindow.Show();
        }

        private Vector2 scrollPosition;
        private SkinnedMeshRenderer skinnedRenderer;
        private bool fromPose;
        private Mesh baseMesh;
        private int submeshMask = 1;
        private string[] maskOptions;
        private UV currentUVChannel;
        private int textureSize = 512;
        private FluidObject fluidObj;
        private Mesh currentMesh;
        private bool autoCompress = false;

        //Thread
        private Thread generateThread;
        private bool isThreadRunning;
        private bool finishedMeshProcessing;

        private float threadProgress;
        private float lastProgress;

        private IEnumerator coroutine;

        void OnGUI()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fluid Object Creator", EditorStyles.largeLabel);
            EditorGUILayout.Space();

            if (!isThreadRunning)
            {
                drawFluidObjectCreator();
            }
            else
            {
                drawProgressBar();
            }

            GUILayout.EndScrollView();
        }

        private int submeshCount()
        {
            if (fromPose)
            {
                return (skinnedRenderer != null && skinnedRenderer.sharedMesh != null) ? skinnedRenderer.sharedMesh.subMeshCount : 0;
            }
            else
            {
                return (baseMesh != null) ? baseMesh.subMeshCount : 0;
            }
        }

        // FluidObject creation tab
        private void drawFluidObjectCreator()
        {
            EditorGUILayout.LabelField("Texture Data", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            textureSize = Mathf.Clamp((EditorGUILayout.IntField("Texture Size:", textureSize) / 16) * 16, 64, 4096);
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Mesh Data", EditorStyles.boldLabel);
            EditorGUI.indentLevel = 1;
            fromPose = EditorGUILayout.Toggle("From Pose", fromPose);

            if (fromPose)
            {
                skinnedRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh:", skinnedRenderer, typeof(SkinnedMeshRenderer), true);
            }
            else
            {
                baseMesh = (Mesh)EditorGUILayout.ObjectField("Mesh:", baseMesh, typeof(Mesh), true);
            }

            int submeshes = submeshCount();
            if (submeshes > 1)
            {
                if (maskOptions == null || maskOptions.Length != submeshes)
                {
                    // update dropdown options
                    maskOptions = new string[submeshes];
                    for (int i = 0; i < maskOptions.Length; i++)
                        maskOptions[i] = i.ToString();
                    submeshMask = ~0;
                }
                submeshMask = EditorGUILayout.MaskField("Submesh Mask:", submeshMask, maskOptions);

                if (submeshMask == 0)   // prohibit selecting nothing
                    submeshMask = 1;
            }

            currentUVChannel = (UV)EditorGUILayout.EnumPopup("UV Channel:", currentUVChannel);

            EditorGUI.indentLevel = 0;
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            autoCompress = EditorGUILayout.Toggle("Auto Compress", autoCompress);

            if (GUILayout.Button("Generate FluidObject"))
            {
                currentMesh = null;
                if (!fromPose)
                {
                    currentMesh = baseMesh;
                }
                else if (skinnedRenderer != null)
                {
                    // bake current pose
                    currentMesh = new Mesh();
                    currentMesh.name = skinnedRenderer.sharedMesh.name + "_Skinned";
                    skinnedRenderer.BakeMesh(currentMesh);
                }
                else
                {
                    Debug.LogWarning("No SkinnedMeshRenderer assigned!");
                }

                if (currentMesh == null)
                {
                    Debug.LogWarning("No Mesh assigned!");
                }
                else
                {
                    processMesh(currentMesh);
                }
            }
            
        }

        private void drawProgressBar()
        {
            EditorGUILayout.Space();
            EditorGUI.indentLevel = 1;
            EditorGUILayout.LabelField("Generating Fluid Object...");
            EditorGUI.indentLevel = 0;

            EditorGUILayout.Space();

            Rect r = EditorGUILayout.BeginVertical();
            EditorGUI.ProgressBar(r, fluidObj.getProgress(), fluidObj.getProgressState());
            GUILayout.Space(25);
            EditorGUILayout.EndVertical();

            if (finishedMeshProcessing)
            {
                finishedMeshProcessing = false;
                EditorCoroutine.start(generateData());
            }
        }

        // start FluidObject creation
        private void processMesh(Mesh mesh)
        {
            List<Vector2> meshUVList = new List<Vector2>();
            mesh.GetUVs((int)currentUVChannel, meshUVList);
            
            if(meshUVList.Count == 0)
            {
                Debug.LogWarning("Mesh does not have " + currentUVChannel);
                return;
            }

            fluidObj = ScriptableObject.CreateInstance<FluidObject>();

            if (fromPose)
                // save the pose the FluidObject was baked for
                fluidObj.setSkeletonDefaultPose(skinnedRenderer);

            // build submesh
            uint count = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if ((submeshMask & (1 << i)) > 0)
                    count += mesh.GetIndexCount(i);
            }
            int[] meshTris = new int[count];
            uint startIndex = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                if ((submeshMask & (1 << i)) > 0)
                {
                    uint length = mesh.GetIndexCount(i);
                    System.Array.Copy(mesh.GetIndices(i), 0, meshTris, startIndex, length);
                    startIndex += length;
                }
            }

            Vector3[] meshVerts = mesh.vertices;
            Bounds meshBounds = mesh.bounds;
            string meshName = mesh.name;

            isThreadRunning = true;
            finishedMeshProcessing = false;
            ThreadStart threadStart = () => {
                fluidObj.GenerateMeshdata(meshTris, meshVerts, meshUVList.ToArray(), currentUVChannel, submeshMask, meshBounds, meshName);
                finishedMeshProcessing = true;
            };
            generateThread = new Thread(threadStart);
            generateThread.Start();
        }

        private IEnumerator generateData()
        {
            fluidObj.updateProgress(.9f, "compute simulation data..");
            Repaint();
            yield return null;

            fluidObj.GenerateFlowData(textureSize);

            if (autoCompress)
            {
                fluidObj.updateProgress(.92f, "compressing");
                Repaint();
                yield return null;

                fluidObj.CompressEditor();
            }

            fluidObj.updateProgress(.95f, "done! writing to file..");
            Repaint();
            yield return null;

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (System.IO.Path.GetExtension(path) != "")
            {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/" + currentMesh.name + "_FluidData.asset");
            AssetDatabase.CreateAsset(fluidObj, assetPath);

            fluidObj.updateProgress(1f, "saving..");
            Repaint();
            yield return null;

            EditorUtility.SetDirty(fluidObj);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = fluidObj;

            isThreadRunning = false;
            Repaint();
        }

        //update progress bar
        void OnInspectorUpdate()
        {
            if (isThreadRunning)
            {
                threadProgress = fluidObj.getProgress();
                if (lastProgress != threadProgress)
                    Repaint();
                if (fluidObj != null)
                    lastProgress = fluidObj.getProgress();
            }
        }
    }

    public class EditorCoroutine
    {
        public static EditorCoroutine start(IEnumerator _routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(_routine);
            coroutine.start();
            return coroutine;
        }

        readonly IEnumerator routine;
        EditorCoroutine(IEnumerator _routine)
        {
            routine = _routine;
        }

        void start()
        {
            EditorApplication.update += update;
        }
        public void stop()
        {
            EditorApplication.update -= update;
        }

        void update()
        {
            if (!routine.MoveNext())
            {
                stop();
            }
        }
    }
}