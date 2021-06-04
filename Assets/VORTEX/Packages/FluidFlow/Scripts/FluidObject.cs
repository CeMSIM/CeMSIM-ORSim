using System.Collections;
using UnityEngine;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluidSimulation
{
    [PreferBinarySerialization]
    public class FluidObject : ScriptableObject
    {
        public int TextureSize { get { return textureSize; } }
        [SerializeField]
        private int textureSize;

        public UV UVChanel { get { return uvChanel; } }
        [SerializeField]
        private UV uvChanel;

        public int SubmeshMask { get { return submeshMask; } }
        [SerializeField]
        private int submeshMask;

        public int UnpaddedTriangleCount { get { return unpaddedTriangleCount; } }
        [SerializeField]
        private int unpaddedTriangleCount;

        public int PaddedTriangleCount { get { return paddedTriangleCount; } }
        [SerializeField]
        private int paddedTriangleCount;

        public BoundingBox MeshBounds { get { return meshBounds; } }
        [SerializeField]
        private BoundingBox meshBounds;

        public string BaseMeshName { get { return baseMeshName; } }
        [SerializeField]
        private string baseMeshName;

        // skeleton data
        [SerializeField]
        private Matrix4x4[] baseSkeletonPoseL2W;
        [SerializeField]
        private Matrix4x4[] baseSkeletonPoseW2L;
        [SerializeField]
        private Vector3[] baseSkeletonPoseBonePositions;
        [SerializeField]
        private bool fromSkinnedMesh = false;

        // compression
        [SerializeField]
        private CompressionState compressionState = CompressionState.Decompressed;

        // uncompressed data
        [SerializeField]
        private byte[] tangents;
        [SerializeField]
        private byte[] texelMapRaw;
        [SerializeField]
        private byte[] trianglesPacked;

        // compressed data
        [SerializeField]
        private byte[] texelMapRawCompressed;
        [SerializeField]
        private byte[] tangentsCompressed;
        [SerializeField]
        private byte[] trianglesPackedCompressed;

        // temporary reference to uncompressed data; only used when data has benn decompressed at runtime
        [System.NonSerialized]
        private CompressionState compressionStateTmp = CompressionState.Compressed;
        [System.NonSerialized]
        private byte[] texelMapTmp;
        [System.NonSerialized]
        private byte[] tangentsTmp;
        [System.NonSerialized]
        private byte[] trianglesPackedTmp;

        #region Public

        /// <summary>
        /// was this FluidObject generated from a SkinnedMeshRenderer
        /// </summary>
        /// <returns></returns>
        public bool hasSkeletonData()
        {
            return fromSkinnedMesh;
        }
        
        /// <summary>
        /// Transforms a point (worldspace) from the current skeleton pose back to the 'default' pose (when creating the Fluid Object)
        /// </summary>
        /// <param name="bone">Transform of the bone closest to the position</param>
        /// <param name="boneId">Id of the bone in the SkinnedMeshRenderer closest to the position</param>
        /// <param name="worldPosition">Point in world space</param>
        /// <returns>Point on the 'Fluid Object' (local space)</returns>
        public Vector3 TransformPointToDefaultPose(Transform bone, int boneId, Vector3 worldPosition)
        {
            if (!hasSkeletonData())
            {
                Debug.LogWarning("Fluid Object has no skeleton data");
                return worldPosition;
            }

            if (boneId < 0 || boneId >= baseSkeletonPoseL2W.Length)
            {
                Debug.LogWarning("Invalid bone Id!");
                return worldPosition;
            }

            // world to bone coordinate space
            Vector3 pos = bone.InverseTransformPoint(worldPosition);

            // bone to world coordinate space at default pose
            return baseSkeletonPoseL2W[boneId].MultiplyPoint(pos);
        }

        /// <summary>
        /// Transforms a direction (worldspace) from the current skeleton pose back to the 'default' pose (when creating the FluidObject)
        /// </summary>
        /// <param name="bone">Transform of the bone closest to the position</param>
        /// <param name="boneId">Id of the bone in the SkinnedMeshRenderer you want to base the transformation on</param>
        /// <param name="direction">Direction in world space</param>
        /// <returns>Direction relative to the default pose of the Fluid Object (local space)</returns>
        public Vector3 TransformDirectionToDefaultPose(Transform bone, int boneId, Vector3 direction)
        {
            if (!hasSkeletonData())
            {
                Debug.LogWarning("Fluid Object has no skeleton data");
                return direction;
            }

            if (boneId < 0 || boneId >= baseSkeletonPoseL2W.Length)
            {
                Debug.LogWarning("Invalid bone Id!");
                return direction;
            }

            // world to bone coordinate space
            Vector3 dir = bone.InverseTransformDirection(direction);

            // bone to world coordinate space at default pose
            return baseSkeletonPoseL2W[boneId].MultiplyVector(dir);
        }

        
        /// <summary>
        /// Get the Id of the closest bone in the baked default pose
        /// </summary>
        /// <param name="position"></param>
        /// <returns>ID of the closest bone</returns>
        public int FindClosestBoneDefaultPose(Vector3 position)
        {
            if (!hasSkeletonData())
            {
                Debug.LogWarning("Fluid Object has no skeleton data");
                return 0;
            }

            float closestDist = float.MaxValue;
            int closestID = 0;
            for (int i = 0; i < baseSkeletonPoseBonePositions.Length; i++)
            {
                float dist = (baseSkeletonPoseBonePositions[i] - position).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestID = i;
                }
            }
            return closestID;
        }

        /// <summary>
        /// Transform point from baked default pose to worldspace when using a SkinnedMeshRenderer
        /// </summary>
        /// <param name="bone">renderer used to render the object</param>
        /// <param name="boneId">Id of the closest bone to the position in the SkinnedMeshRenderer.bones array</param>
        /// <param name="position">position on the default pose</param>
        /// <returns>Position in worldspace</returns>
        public Vector3 TransformPointFromDefaultPose(Transform bone, int boneId, Vector3 position)
        {
            if (!hasSkeletonData())
            {
                Debug.LogWarning("Fluid Object has no skeleton data");
                return position;
            }

            if (boneId < 0 || boneId >= baseSkeletonPoseW2L.Length)
            {
                Debug.LogWarning("Invalid bone Id!");
                return position;
            }

            position = baseSkeletonPoseW2L[boneId].MultiplyPoint(position);
            return bone.TransformPoint(position);
        }

        public ComputeBuffer GetTriangleBuffer()
        {
            if(compressionState != CompressionState.Decompressed && compressionStateTmp != CompressionState.Decompressed)
            {
                Debug.LogError("FluidObject has to be decompressed before use!");
                return null;
            }

            ComputeBuffer triBuffer = new ComputeBuffer(unpaddedTriangleCount, sizeof(uint) * 8);
            triBuffer.SetData(compressionState == CompressionState.Decompressed ? trianglesPacked : trianglesPackedTmp);
            return triBuffer;
        }

        public ComputeBuffer GetRawTextureBuffer()
        {
            if (compressionState != CompressionState.Decompressed && compressionStateTmp != CompressionState.Decompressed)
            {
                Debug.LogError("FluidObject has to be decompressed before use!");
                return null;
            }

            ComputeBuffer rawBuffer = new ComputeBuffer(textureSize * textureSize, sizeof(uint) * 2);
            rawBuffer.SetData(compressionState == CompressionState.Decompressed ? texelMapRaw : texelMapTmp);
            return rawBuffer;
        }

        public ComputeBuffer GetTangentsBuffer()
        {
            if (compressionState != CompressionState.Decompressed && compressionStateTmp != CompressionState.Decompressed)
            {
                Debug.LogError("FluidObject has to be decompressed before use!");
                return null;
            }

            ComputeBuffer tangentBuffer = new ComputeBuffer(unpaddedTriangleCount, sizeof(float) * 4);
            tangentBuffer.SetData(compressionState == CompressionState.Decompressed ? tangents : tangentsTmp);
            return tangentBuffer;
        }

        public CompressionState GetCompressionState()
        {
            return compressionState;
        }

        public IEnumerator DecompressDataRuntime()
        {
            if (compressionState == CompressionState.Compressed && compressionStateTmp == CompressionState.Compressed)
            {
                compressionStateTmp = CompressionState.Decompressing;
                Thread decompressThread = new Thread(() => decompress());
                decompressThread.Start();
                while (decompressThread.IsAlive)
                {
                    yield return null;
                }
                compressionStateTmp = CompressionState.Decompressed;
            }
            else
            {
                // already being decompressed
                // wait for completion
                while(compressionStateTmp != CompressionState.Decompressed)
                {
                    yield return null;
                }
            }
        }

        private void decompress()
        {
            texelMapTmp = CLZF2.Decompress(texelMapRawCompressed);
            tangentsTmp = CLZF2.Decompress(tangentsCompressed);
            trianglesPackedTmp = CLZF2.Decompress(trianglesPackedCompressed);
        }

        #endregion

        #region FluidObject Generation (Editor only)
#if UNITY_EDITOR

        [System.Serializable]
        public struct TriangleData
        {
            public Vector4 a;
            public Vector4 b;
            public Vector4 c;

            public TriangleData(Vector3 a, Vector3 b, Vector3 c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        [System.Serializable]
        public struct UVData
        {
            public Vector2 a;
            public Vector2 b;
            public Vector2 c;
            public Vector2 padding;

            public UVData(Vector2 a, Vector2 b, Vector2 c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
                padding = Vector2.zero;
            }

            public UVData(Vector2 a)
            {
                this.a = a;
                this.b = a;
                this.c = a;
                padding = Vector2.zero;
            }
        }


        private UVData[] trianglesUV;
        private TriangleData[] trianglesVert;
        private int[] neighbours;

        private const float MERGE_DIST_SQR = 0.00000001f;      //if the distance between two vertices (squared) is less than this, they will be merged
        private ComputeShader preparationShader { get { return Resources.Load<ComputeShader>("FluidObjectCreation"); } }

        // methods to track generation progess in editor
        private string progressState = "";
        public string getProgressState()
        {
            return progressState;
        }

        private float progress = 0f;
        public float getProgress()
        {
            return progress;
        }

        public void updateProgress(float p, string state)
        {
            progress = p;
            progressState = state;
        }
        private void updateProgress(float p)
        {
            progress = p;
        }
        private void updateProgress(string state)
        {
            progressState = state;
        }
        private void resetProgress()
        {
            progress = 0f;
            progressState = "";
        }

        public void GenerateMeshdata(int[] triangles, Vector3[] verts, Vector2[] uvs, UV uvChanel, int submeshMask, Bounds meshBounds, string meshName)
        {
            resetProgress();
            updateProgress("gernerate mesh data..");

            this.baseMeshName = meshName;
            this.uvChanel = uvChanel;
            this.submeshMask = submeshMask;
            this.meshBounds = new BoundingBox(meshBounds);
            addPadding(triangles, uvs, verts, out trianglesUV, out trianglesVert);

            int[] seamlessTris = generateSeamlessMesh(triangles, verts);
            neighbours = getNeighbours(seamlessTris);
        }

        public void GenerateFlowData(int textureSize)
        {
            this.textureSize = textureSize;
            ComputeShader compute = preparationShader;

            ComputeBuffer trianglesUVBuffer = new ComputeBuffer(trianglesUV.Length, sizeof(float) * 8);
            trianglesUVBuffer.SetData(trianglesUV);
            ComputeBuffer trianglesVertBuffer = new ComputeBuffer(trianglesVert.Length, sizeof(float) * 12);
            trianglesVertBuffer.SetData(trianglesVert);
            ComputeBuffer trianglesNeighbourBuffer = new ComputeBuffer(unpaddedTriangleCount, sizeof(int) * 4);
            trianglesNeighbourBuffer.SetData(neighbours);

            setupTangents(compute, trianglesUVBuffer, trianglesVertBuffer);
            setupMaps(compute, trianglesUVBuffer, trianglesNeighbourBuffer);

            trianglesUVBuffer.Release();
            trianglesVertBuffer.Release();
            trianglesNeighbourBuffer.Release();
            trianglesUV = null;
            trianglesVert = null;
            neighbours = null;
        }

        /// <summary>
        /// set the pose used for this Fluid Object (called by the Fluid Object Creator when 'From Pose' is enabled)
        /// </summary>
        /// <param name="skinnedMesh"></param>
        public void setSkeletonDefaultPose(SkinnedMeshRenderer skinnedMesh)
        {
            Matrix4x4 baseTransform = skinnedMesh.worldToLocalMatrix;
            Transform[] bones = skinnedMesh.bones;

            baseSkeletonPoseL2W = new Matrix4x4[bones.Length];
            baseSkeletonPoseW2L = new Matrix4x4[bones.Length];
            baseSkeletonPoseBonePositions = new Vector3[bones.Length];
            for (int i = 0; i < baseSkeletonPoseL2W.Length; i++)
            {
                //save conversion matrices for current pose
                baseSkeletonPoseL2W[i] = baseTransform * bones[i].localToWorldMatrix;
                baseSkeletonPoseW2L[i] = bones[i].worldToLocalMatrix * skinnedMesh.localToWorldMatrix;

                //average bone position
                Vector3 position = bones[i].position;
                int connectedBoneCount = 1;
                for (int n = 0; n < bones[i].childCount; n++)
                {
                    Transform child = bones[i].GetChild(n);
                    if (isPartOfSkeleton(child, bones))
                    {
                        position += child.position;
                        connectedBoneCount++;
                    }
                }
                baseSkeletonPoseBonePositions[i] = baseTransform.MultiplyPoint(position / connectedBoneCount);
            }
            fromSkinnedMesh = true;
        }

        private bool isPartOfSkeleton(Transform bone, Transform[] bones)
        {
            foreach (Transform cbone in bones)
            {
                if (bone == cbone)
                    return true;
            }
            return false;
        }

        private void addPadding(int[] tris, Vector2[] uvs, Vector3[] verts, out UVData[] uvPadded, out TriangleData[] trianglesVert, int multipleOf = 64)
        {
            updateProgress(.01f, "pad to multiple of 64");

            unpaddedTriangleCount = tris.Length / 3;
            paddedTriangleCount = (unpaddedTriangleCount / multipleOf + 1) * multipleOf;

            uvPadded = new UVData[paddedTriangleCount];
            trianglesVert = new TriangleData[unpaddedTriangleCount];

            for (int i = 0; i < unpaddedTriangleCount; i++)
            {
                uvPadded[i] = new UVData(uvs[tris[i * 3]], uvs[tris[i * 3 + 1]], uvs[tris[i * 3 + 2]]);
                trianglesVert[i] = new TriangleData(verts[tris[i * 3]], verts[tris[i * 3 + 1]], verts[tris[i * 3 + 2]]);
            }

            for (int i = unpaddedTriangleCount; i < paddedTriangleCount; i++)
            {
                uvPadded[i] = new UVData(new Vector2(-1, -1));
            }
        }

        private int[] generateSeamlessMesh(int[] originalTris, Vector3[] originalVerts)
        {
            updateProgress("remove mesh seams");

            int vertLength = originalVerts.Length;
            int[] vertexIDMap = new int[vertLength];

            // fill linear 0, 1, 2, ..., n
            for (int i = 0; i < vertLength; i++)
            {
                vertexIDMap[i] = i;
            }

            float lengthInv = 1f / vertLength;

            for (int o = 0; o < vertLength; o++)
            {
                // this vertex has same position as one before it
                if (vertexIDMap[o] != o)
                    continue;

                Vector3 vertexPos = originalVerts[o];
                for (int i = o + 1; i < vertLength; i++)
                {
                    // any other vertex with same position
                    if (Vector3.SqrMagnitude(originalVerts[i] - vertexPos) < MERGE_DIST_SQR)
                    {
                        vertexIDMap[i] = o;
                        updateProgress(Mathf.Lerp(.01f, .4f, o * lengthInv));
                    }
                }
            }

            // get difference after removing references
            int val = 0;
            for (int i = 0; i < vertLength; i++)
            {
                // non default vertex id?
                if (i != vertexIDMap[i])
                {
                    // increase merged vertices count
                    val++;
                    // set my id to the (maybe) moved id of the vertex I merged with
                    vertexIDMap[i] = vertexIDMap[vertexIDMap[i]];
                }
                else
                {
                    vertexIDMap[i] -= val;
                }
            }

            // regenerate 'seamless' triangle array
            int[] newTris = new int[originalTris.Length];
            for (int i = 0; i < originalTris.Length; i++)
            {
                newTris[i] = vertexIDMap[originalTris[i]];
            }

            return newTris;
        }

        private int[] getNeighbours(int[] seamlessTris)
        {
            // wich triangle lies to each side of each triangle
            updateProgress("find connected triangles");
            int[] NEXT = new int[] { 1, 2, 0 };

            int[] neighbours = new int[unpaddedTriangleCount * 4];
            bool[] joined = new bool[unpaddedTriangleCount * 3];
            float lengthInv = 1f / unpaddedTriangleCount;
            for (int t = 0; t < unpaddedTriangleCount; t++)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (joined[t * 3 + i]) continue;

                    int currentVertexID = seamlessTris[t * 3 + i];
                    int nextVertexID = seamlessTris[t * 3 + NEXT[i]];

                    bool hasFoundNeighbour = false;
                    for (int s = t + 1; s < unpaddedTriangleCount && !hasFoundNeighbour; s++)
                    {
                        for (int n = 0; n < 3 && !hasFoundNeighbour; n++)
                        {
                            if (currentVertexID == seamlessTris[s * 3 + NEXT[n]] && nextVertexID == seamlessTris[s * 3 + n])
                            {
                                neighbours[t * 4 + i] = s * 3 + NEXT[n];
                                neighbours[s * 4 + n] = t * 3 + NEXT[i];

                                hasFoundNeighbour = true;

                                joined[t * 3 + i] = true;
                                joined[s * 3 + n] = true;
                            }
                        }
                    }
                    if (!hasFoundNeighbour)
                        neighbours[t * 4 + i] = -1;
                }
                updateProgress(Mathf.Lerp(.4f, .9f, t * lengthInv));
            }
            return neighbours;
        }

        private void setupTangents(ComputeShader compute, ComputeBuffer uvBuffer, ComputeBuffer vertBuffer)
        {
            ComputeBuffer tangentBuffer = new ComputeBuffer(unpaddedTriangleCount, sizeof(float) * 4);
            ComputeBuffer triCompressedBuffer = new ComputeBuffer(unpaddedTriangleCount, sizeof(uint) * 8);
            ComputeBuffer boundsBuffer = new ComputeBuffer(2, sizeof(float) * 4);

            Vector4 sizeInv = MeshBounds.size;
            sizeInv = new Vector4(1f / sizeInv.x, 1f / sizeInv.y, 1f / sizeInv.z, 0);
            boundsBuffer.SetData(new Vector4[] { sizeInv, MeshBounds.min });

            int kernelIndex = compute.FindKernel("setupTangents");
            compute.SetBuffer(kernelIndex, "tangents", tangentBuffer);
            compute.SetBuffer(kernelIndex, "triDataCompressed", triCompressedBuffer);
            compute.SetBuffer(kernelIndex, "bounds", boundsBuffer);
            compute.SetBuffer(kernelIndex, "trianglesUV", uvBuffer);
            compute.SetBuffer(kernelIndex, "trianglesVert", vertBuffer);
            compute.SetInt("TRISCOUNT_UNPADDED", unpaddedTriangleCount);

            compute.Dispatch(kernelIndex, paddedTriangleCount / 64, 1, 1);

            tangents = new byte[unpaddedTriangleCount * sizeof(float) * 4];
            tangentBuffer.GetData(tangents);

            trianglesPacked = new byte[unpaddedTriangleCount * sizeof(uint) * 8];
            triCompressedBuffer.GetData(trianglesPacked);

            tangentBuffer.Release();
            boundsBuffer.Release();
            triCompressedBuffer.Release();
        }

        private void setupMaps(ComputeShader compute, ComputeBuffer uvBuffer, ComputeBuffer neighbourBuffer)
        {
            ComputeBuffer output = new ComputeBuffer(textureSize * textureSize, sizeof(int) * 2);

            int kernelIndex = compute.FindKernel("setupMaps");
            compute.SetBuffer(kernelIndex, "trianglesUV", uvBuffer);
            compute.SetBuffer(kernelIndex, "trianglesNeighbours", neighbourBuffer);
            compute.SetBuffer(kernelIndex, "outputBuffer", output);
            compute.SetFloat("TexturesizeInverse", 1f / (float)textureSize);
            compute.SetInt("TEXSIZE", textureSize);
            compute.SetInt("TRISCOUNT_UNPADDED", unpaddedTriangleCount);

            int divisions = 4;
            int size = textureSize / (divisions * 8);
            int offset = textureSize / divisions;
            for (int y = 0; y < divisions; y++)
            {
                for (int x = 0; x < divisions; x++)
                {
                    compute.SetInts("startOffset", x * offset, y * offset);
                    compute.Dispatch(kernelIndex, size, size, 1);
                    GL.Flush();
                }
            }

            byte[] raw = new byte[textureSize * textureSize * 2 * sizeof(uint)];
            output.GetData(raw);
            
            texelMapRaw = raw;

            output.Release();
        }

        public void CompressEditor()
        {
            if (compressionState == CompressionState.Decompressed)
            {
                if (texelMapRaw == null || tangents == null || trianglesPacked == null)
                {
                    Debug.LogError("All FluidObject data has to be assigned before compression!");
                    return;
                }

                texelMapRawCompressed = CLZF2.Compress(texelMapRaw);
                tangentsCompressed = CLZF2.Compress(tangents);
                trianglesPackedCompressed = CLZF2.Compress(trianglesPacked);
                texelMapRaw = null;
                tangents = null;
                trianglesPacked = null;

                compressionState = CompressionState.Compressed;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                
            }
            else
            {
                Debug.LogWarning("FluidObject already compressed!");
            }
        }

        public void DecompressEditor()
        {
            if (compressionState == CompressionState.Compressed)
            {
                if (texelMapRawCompressed == null || tangentsCompressed == null || trianglesPackedCompressed == null)
                {
                    Debug.LogError("Error! Unable to read compressed data.");
                    return;
                }

                texelMapRaw = CLZF2.Decompress(texelMapRawCompressed);
                tangents = CLZF2.Decompress(tangentsCompressed);
                trianglesPacked = CLZF2.Decompress(trianglesPackedCompressed);
                texelMapRawCompressed = null;
                tangentsCompressed = null;
                trianglesPackedCompressed = null;

                compressionState = CompressionState.Decompressed;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();

            }
            else
            {
                Debug.LogWarning("FluidObject already decompressed!");
            }
        }
#endif
        #endregion
    }

    [System.Serializable]
    public struct BoundingBox
    {
        public Vector4 min;
        public Vector4 size;

        public BoundingBox(Bounds b)
        {
            this.min = b.min;
            this.size = b.size;
        }
    }

    public enum UV
    {
        UV1 = 0,
        UV2 = 1
    }

    public enum CompressionState
    {
        Decompressed,
        Decompressing,
        Compressed
    }
}
