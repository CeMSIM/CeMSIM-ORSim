using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidSimulation
{
    public class FluidSimulator : MonoBehaviour
    {
        #region Public
        public FluidObject fluidObject;
        public Renderer fluidRenderer;
        public int timeout = 200;

        /// <summary>
        /// maximum sample distance in pixels used for flow calculation
        /// </summary>
        [Range(0f, 2f)]
        public float speed = 1.2f;
        public Texture2D normalMap;
        [Range(0f, 1f)]
        public float normal = .1f;
        /// <summary>
        /// reduces amount of fluid sticking to sloped surfaces
        /// </summary>
        [Range(0f, 1f)]
        public float slope = .1f;
        /// <summary>
        /// amount of fluid sticking to the surface (not flowing)
        /// </summary>
        public float roughness = 1.1f;
        /// <summary>
        /// time until fresh fluid is dry
        /// </summary>
        public float dryTime = 1f;
        /// <summary>
        /// evaporate fluid over time
        /// </summary>
        public bool useFluidEvapoation = true;
        /// <summary>
        /// fluid amount evaporating per second
        /// </summary>
        public float evaporationPerSecond = .1f;

        /// <summary>
        /// enable fluid dripping of the surface
        /// </summary>
        public bool checkForDrops = false;
        public float minimumDripAmount = 2;
        public int dropUpdateSkipAmount = 20;

        [System.Serializable]
        public class DripEvent : UnityEngine.Events.UnityEvent<Vector3, float> { };
        /// <summary>
        /// Called when fluid drip is formed. First parameter position, second amount
        /// </summary>
        public DripEvent OnFluidDrip;

        public bool Initialized { get; private set; }

        /// <summary>
        /// Updates the fluid flow to match gravity (e.g. object rotated)
        /// </summary>
        /// <param name="gravity">Direction of gravity in world space</param>
        /// <param name="space">Space of the given position and direction data</param>
        public void UpdateGravity(Vector3 gravity, Space space = Space.World)
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            gravity.Normalize();
            if (space == Space.World)
            {
                gravity = rendererTransform.InverseTransformDirection(gravity);
            }
            localGravity = gravity;

            flowShader.SetBuffer(simManager.updateGravVecKernelID, simManager.tangentBufferID, tangentBuffer);
            flowShader.SetBuffer(simManager.updateGravVecKernelID, simManager.gravityVecBufferID, gravityBuffer);
            flowShader.SetInt(simManager.trisCountValueID, fluidObject.UnpaddedTriangleCount);
            flowShader.SetFloats(simManager.gravityValueID, gravity.x, gravity.y, gravity.z);
            flowShader.Dispatch(simManager.updateGravVecKernelID, fluidObject.PaddedTriangleCount / 64, 1, 1);

            UpdateFlowMap();
        }

        /// <summary>
        /// Update FlowMap used for the simulation, when speed, normal, slope or roughness values are changed.
        /// Called automatically when UpdateGravity is called.
        /// </summary>
        public void UpdateFlowMap()
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            RenderTexture tmp = null;
            bool useNormal = normalMap != null;

            int kernelID = useNormal ? simManager.updateGravMapNormalKernelID : simManager.updateGravMapKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                tmp = getTemporaryRT(fluidTexDescriptor);
                flowShader.SetTexture(kernelID, simManager.texelTexRID, texelTex);
                flowShader.SetTexture(kernelID, simManager.fluidTexRID, fluidTex);
                flowShader.SetTexture(kernelID, simManager.fluidTexWID, tmp);
            } else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
                flowShader.SetBuffer(kernelID, simManager.fluidBufferID, fluidBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetBuffer(kernelID, simManager.gravityVecBufferID, gravityBuffer);
            if (useNormal)
            {
                flowShader.SetTexture(kernelID, simManager.normalTexID, normalMap);
                flowShader.SetFloat(simManager.normalAmountValueID, normal);
            }
            flowShader.SetFloat(simManager.texSizeInvValueID, 1f / texSize);
            flowShader.SetFloat(simManager.fluidSpeedValueID, speed);
            flowShader.SetFloat(simManager.slopeAmountValueID, slope);
            flowShader.SetFloat(simManager.roughnessValueID, roughness * FluidSimulationManager.MULTI);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                RenderTexture.ReleaseTemporary(fluidTex);
                fluidTex = tmp;
            }

            updateCounter = timeout;
        }

        /// <summary>
        /// Clear the fluid texture
        /// </summary>
        public void ResetFluid()
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            RenderTexture fluidTexTmp = null;

            int kernelID = simManager.resetFluidKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                fluidTexTmp = getTemporaryRT(fluidTexDescriptor);
                flowShader.SetTexture(kernelID, simManager.fluidTexWID, fluidTexTmp);
                flowShader.SetTexture(kernelID, simManager.fluidTexRID, fluidTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.fluidBufferID, fluidBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetTexture(kernelID, simManager.flowTexWID, flowTex);
            flowShader.SetTexture(kernelID, simManager.finalTexID, finTex);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                RenderTexture.ReleaseTemporary(fluidTex);
                fluidTex = fluidTexTmp;
            }
        }

        /// <summary>
        /// Is the object animated and does the fluidObject have skeleton data?
        /// </summary>
        /// <returns></returns>
        public RenderTexture GetFluidTexture()
        {
            return finTex;
        }

        /// <summary>
        /// UV chanel the fluidObject is using
        /// </summary>
        /// <returns></returns>
        public UV GetUVChanel()
        {
            return fluidObject.UVChanel;
        }


        /// <summary>
        /// Returns true if renderer is skinned and fluidObject has skeleton data 
        /// </summary>
        /// <returns></returns>
        public bool isSkinned()
        {
            return fluidRenderer.GetType() == typeof(SkinnedMeshRenderer) && fluidObject != null && fluidObject.hasSkeletonData();
        }

        /// <summary>
        /// Assign the fluid texture to the materials of a renderer, if they are in the submesh mask of the FluidObject. Setting '_FluidTex' property, and enabling keyword '_FluidUV_UV1' / '_FluidUV_UV2'
        /// </summary>
        /// <param name="renderer"></param>
        public void SetMaterialData(Renderer renderer)
        {
            Material[] mats = renderer.materials;
            UV UVChanel = GetUVChanel();

            for (int i = 0; i < mats.Length; i++)
            {
                if ((fluidObject.SubmeshMask & (1 << i)) > 0)
                {
                    mats[i].SetTexture("_FluidTex", GetFluidTexture());
                    mats[i].EnableKeyword(UVChanel == UV.UV1 ? "_Fluid_UV1" : "_Fluid_UV2");
                    mats[i].DisableKeyword(UVChanel == UV.UV1 ? "_Fluid_UV2" : "_Fluid_UV1");
                }
            }
            renderer.materials = mats;
        }

        /// <summary>
        /// Returns if this simulator is active and not timed out
        /// </summary>
        /// <returns></returns>
        public bool isSimulatorActive()
        {
            return updateCounter < timeout && (!simManager.onlyVisible || fluidRenderer.isVisible) && isActiveAndEnabled;
        }

        #endregion

        #region Private
        private ComputeShader flowShader;
        private RenderTexture texelTex;
        private RenderTexture fluidTex;
        private RenderTexture finTex;
        private RenderTexture flowTex;
        private ComputeBuffer texelBuf;
        private ComputeBuffer fluidBuf;
        private ComputeBuffer tangentBuffer;
        private ComputeBuffer boundsBuffer;
        private ComputeBuffer gravityBuffer;
        private ComputeBuffer dropBuffer;
        private ComputeBuffer countBuffer;

        private RenderTextureDescriptor fluidTexDescriptor;
        private RenderTextureDescriptor flowTexDescriptor;

        private Transform rendererTransform;
        private Transform[] bones;
        private Dictionary<Transform, int> boneIdMap;
        private int texSize;
        private int groupCount;
        private int groupCountDrop;
        private FluidSimulationManager simManager;
        private int updateCounter;
        private int skippedDropUpdates = 0;
        private float deltaTime;
        private bool isInUpdateQueue;
        private Vector3 localGravity;
        private float totalMaxFluid = 0;
        private float evapAmount;
        private float timeTillDry;
        private const float HalfInv = 1f / 0xFFFF;
        #endregion

        #region Init

        private void Awake()
        {
            updateCounter = timeout;
            rendererTransform = fluidRenderer.transform;
            simManager = FluidSimulationManager.instance;
            flowShader = simManager.simulationShader;

            if (fluidObject == null)
            {
                Debug.LogWarning("No FluidObject assigned to '" + this.name + "'");
                return;
            }

            if (isSkinned())
            {
                cacheBoneIds(((SkinnedMeshRenderer)fluidRenderer).bones);
            }

            Init();
        }

        public void Init()
        {

            if (fluidObject.GetCompressionState() != CompressionState.Decompressed)
            {
                simManager.StartCoroutine(DecompressFluidObject());
            }
            else
            {
                initialize();
            }
        }

        private IEnumerator DecompressFluidObject()
        {
            yield return simManager.StartCoroutine(fluidObject.DecompressDataRuntime());
            initialize();
        }

        private void initialize()
        {
            if (simManager.COMPUTESHADER_SUPPORT)
            {
                simManager.SimulationCycle += simulationCycle;

                // create buffers and textures
                setupShaderData(simManager);

                // init gravity
                UpdateGravity(Vector3.down);

                // setup material data
                SetMaterialData(fluidRenderer);
            }

            Initialized = true;
        }

        private void setupShaderData(FluidSimulationManager simManager)
        {
            texSize = fluidObject.TextureSize;
            groupCount = texSize / 8;
            groupCountDrop = texSize / 16;

            if (simManager.HasSharedData(fluidObject))
            {
                // get shared simulation data
                FluidSimulationManager.SharedSimulationData sharedData = simManager.GetSharedData(fluidObject);
                texelTex = sharedData.texelMap;
                tangentBuffer = sharedData.tangentBuffer;
                boundsBuffer = sharedData.boundsBuffer;
            }
            else
            {
                // generate new shared data
                tangentBuffer = fluidObject.GetTangentsBuffer();

                boundsBuffer = new ComputeBuffer(1, sizeof(float) * 4);

                if (simManager.USE_INTEGER_RENDERTEXTURE)
                {
                    texelTex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGBInt, RenderTextureReadWrite.Linear);
                    texelTex.enableRandomWrite = true;
                    texelTex.autoGenerateMips = false;
                    texelTex.Create();
                } else
                {
                    texelBuf = new ComputeBuffer(texSize * texSize, sizeof(uint) * 4);
                }

                LoadTexelData();

                boundsBuffer.SetData(new Vector4[] { fluidObject.MeshBounds.size });
                simManager.EnterSharedData(fluidObject, new FluidSimulationManager.SharedSimulationData(texelTex, texelBuf, tangentBuffer, boundsBuffer));
            }

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                fluidTexDescriptor = new RenderTextureDescriptor(texSize, texSize, RenderTextureFormat.ARGBInt, 0);
                fluidTexDescriptor.enableRandomWrite = true;
                fluidTexDescriptor.autoGenerateMips = false;
                fluidTex = getTemporaryRT(fluidTexDescriptor);
            } else
            {
                fluidBuf = new ComputeBuffer(texSize * texSize, sizeof(uint) * 4);
            }

            flowTexDescriptor = new RenderTextureDescriptor(texSize, texSize, RenderTextureFormat.RFloat, 0);
            flowTexDescriptor.enableRandomWrite = true;
            flowTexDescriptor.autoGenerateMips = false;
            flowTex = getTemporaryRT(flowTexDescriptor);

            finTex = new RenderTexture(texSize, texSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            finTex.enableRandomWrite = true;
            finTex.Create();

            gravityBuffer = new ComputeBuffer(fluidObject.UnpaddedTriangleCount, sizeof(float) * 4);

            if (checkForDrops && simManager.ASYNCGPUREADBACK_SUPPORT)
            {
                dropBuffer = new ComputeBuffer(groupCountDrop * groupCountDrop, sizeof(uint) * 2, ComputeBufferType.Append);
                countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            }
        }

        private void LoadTexelData()
        {
            ComputeBuffer input = fluidObject.GetRawTextureBuffer();
            ComputeBuffer triangleBuffer = fluidObject.GetTriangleBuffer();

            int kernelID = simManager.loadTexelMapKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                flowShader.SetTexture(kernelID, simManager.texelTexWID, texelTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
            }
            flowShader.SetBuffer(kernelID, simManager.triangleDataBufferID, triangleBuffer);
            flowShader.SetBuffer(kernelID, simManager.boundsBufferID, boundsBuffer);
            flowShader.SetBuffer(kernelID, simManager.texelInputBufferID, input);
            flowShader.SetInt(simManager.texSizeValueID, texSize);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            input.Dispose();
            triangleBuffer.Dispose();
        }

        public void cacheBoneIds(Transform[] cacheBones)
        {
            bones = cacheBones;
            boneIdMap = new Dictionary<Transform, int>(cacheBones.Length);
            for (int i = 0; i < cacheBones.Length; i++)
            {
                boneIdMap.Add(cacheBones[i], i);
            }
        }

        public int getBoneId(Transform bone)
        {
            int boneId = -1;
            if (!boneIdMap.TryGetValue(bone, out boneId))
                Debug.LogWarning("Given bone not part of Skeleton");
            return boneId;
        }

        #endregion

        #region Simulation
        private void simulationCycle()
        {
            if (!isInUpdateQueue && isSimulatorActive())
            {
                simManager.EnqueueSimulator(this);
                isInUpdateQueue = true;
            }
        }

        /// <summary>
        /// Simulate one flow iteration (called by the 'FluidSimulationManager')
        /// </summary>
        public void SimulateFlow()
        {
            if (!isActiveAndEnabled || !Initialized)
            {
                isInUpdateQueue = false;
                return;
            }

#if UNITY_2018_2_OR_NEWER
            if (checkForDrops && simManager.ASYNCGPUREADBACK_SUPPORT)
            {
                if (skippedDropUpdates >= dropUpdateSkipAmount)
                {
                    skippedDropUpdates = 0;
                    calculateDrop();
                }
                skippedDropUpdates++;
            }
#endif

            updateCounter++;
            isInUpdateQueue = false;

            RenderTexture tmpFluidTex = null;
            RenderTexture tmpFlowTex = getTemporaryRT(flowTexDescriptor);

            int kernelID = simManager.updateFluidKernelID;

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                tmpFluidTex = getTemporaryRT(fluidTexDescriptor);
                flowShader.SetTexture(kernelID, simManager.fluidTexWID, tmpFluidTex);
                flowShader.SetTexture(kernelID, simManager.fluidTexRID, fluidTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.fluidBufferID, fluidBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }

            flowShader.SetTexture(kernelID, simManager.flowTexWID, tmpFlowTex);
            flowShader.SetTexture(kernelID, simManager.flowTexRID, flowTex);
            flowShader.SetTexture(kernelID, simManager.finalTexID, finTex);
            flowShader.SetFloat(simManager.texSizeInvValueID, 1f / texSize);
            flowShader.SetInt(simManager.dryAmountValueID, (int)((deltaTime / dryTime) * FluidSimulationManager.MULTI));

            evapAmount = useFluidEvapoation ? evapAmount + deltaTime * evaporationPerSecond * FluidSimulationManager.MULTI : 0;
            flowShader.SetInt(simManager.decayAmountValueID, (int)evapAmount);
            evapAmount -= (int)evapAmount;

            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                RenderTexture.ReleaseTemporary(fluidTex);   // release and swap rendertextures
                fluidTex = tmpFluidTex;
            }

            RenderTexture.ReleaseTemporary(flowTex);
            flowTex = tmpFlowTex;

            deltaTime = 0;
        }

#if UNITY_2018_2_OR_NEWER

        private void calculateDrop()
        {
            dropBuffer.SetCounterValue(0);

            // check for drops
            int kernelID = simManager.calcDripKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                flowShader.SetTexture(kernelID, simManager.texelTexRID, texelTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetTexture(kernelID, simManager.flowTexWID, flowTex);
            flowShader.SetBuffer(kernelID, simManager.dropsBufferID, dropBuffer);
            flowShader.SetFloat(simManager.minDripAmtValueID, minimumDripAmount * FluidSimulationManager.MULTI);
            flowShader.SetFloats(simManager.gravityValueID, localGravity.x, localGravity.y, localGravity.z);
            flowShader.Dispatch(kernelID, groupCountDrop, groupCountDrop, 1);

            ComputeBuffer.CopyCount(dropBuffer, countBuffer, 0);

            AsyncGPUReadbackRequest requestCount = AsyncGPUReadback.Request(countBuffer, sizeof(int), 0);
            StartCoroutine(dropReadback(requestCount));
        }

        private struct uint2
        {
            public uint x;
            public uint y;

            public uint2(uint x, uint y)
            {
                this.x = x;
                this.y = y;
            }
        }

        IEnumerator dropReadback(AsyncGPUReadbackRequest requestCount)
        {
            // readback drop count
            yield return new WaitUntil(() => requestCount.done);
            if (requestCount.hasError)
            {
                Debug.LogWarning("Error reading drop count to cpu");

            }
            else
            {
                int count = requestCount.GetData<int>()[0];

                if (count != 0)
                {
                    // readback drop data
                    AsyncGPUReadbackRequest requestDrops = AsyncGPUReadback.Request(dropBuffer, count * sizeof(uint) * 2, 0);
                    yield return new WaitUntil(() => requestDrops.done);

                    if (!requestDrops.hasError)
                    {
                        // process drops
                        Unity.Collections.NativeArray<uint2> drops = requestDrops.GetData<uint2>();
                        for (int i = 0; i < drops.Length; i++)
                        {
                            // unpack drop data
                            Vector3 position = new Vector3((drops[i].x >> 16) * HalfInv * fluidObject.MeshBounds.size.x,
                                                           (drops[i].x & 0xFFFF) * HalfInv * fluidObject.MeshBounds.size.y,
                                                           (drops[i].y & 0xFFFF) * HalfInv * fluidObject.MeshBounds.size.z) + (Vector3)fluidObject.MeshBounds.min;
                            if (isSkinned())
                            {
                                int boneId = fluidObject.FindClosestBoneDefaultPose(position);
                                position = fluidObject.TransformPointFromDefaultPose(bones[boneId], boneId, position);
                            }
                            else
                            {
                                position = rendererTransform.TransformPoint(position);
                            }
                            OnFluidDrip.Invoke(position, (drops[i].y >> 16) * (1f / FluidSimulationManager.MULTI));
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Error reading drop data!");
                    }
                }
            }
        }
#endif

        // evaporate
        private void Update()
        {
            deltaTime += Time.deltaTime;
            if (!useFluidEvapoation || isSimulatorActive() || !isActiveAndEnabled || !Initialized)
            {
                return;
            }
            if (totalMaxFluid > 0)      // evaporate until all fluid is gone, when simulator is timed out
            {
                evapAmount += Time.deltaTime * evaporationPerSecond * FluidSimulationManager.MULTI;
                if ((int)evapAmount >= simManager.minimumEvaporationDelta * FluidSimulationManager.MULTI)
                {
                    evaporateFluid();
                }
            }
            else
            {
                evapAmount = 0;
            }
        }

        private void evaporateFluid()
        {
            RenderTexture tmpFluidTex = null;

            int evap = (int)evapAmount;
            int kernelID = simManager.evapFluidKernelID;

            if(simManager.USE_INTEGER_RENDERTEXTURE)
            {
                tmpFluidTex = getTemporaryRT(fluidTexDescriptor);
                flowShader.SetTexture(kernelID, simManager.fluidTexRID, fluidTex);
                flowShader.SetTexture(kernelID, simManager.fluidTexWID, tmpFluidTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.fluidBufferID, fluidBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }

            flowShader.SetTexture(kernelID, simManager.flowTexRID, flowTex);
            flowShader.SetTexture(kernelID, simManager.finalTexID, finTex);
            flowShader.SetInt(simManager.decayAmountValueID, evap);
            flowShader.SetInt(simManager.dryAmountValueID, (int)((deltaTime / dryTime) * FluidSimulationManager.MULTI));
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                RenderTexture.ReleaseTemporary(fluidTex);   // release and swap rendertextures
                fluidTex = tmpFluidTex;
            }

            totalMaxFluid -= evap;
            evapAmount -= evap;
            deltaTime = 0;
        }

        private RenderTexture getTemporaryRT(RenderTextureDescriptor desc)
        {
            RenderTexture rt = RenderTexture.GetTemporary(desc);
            if (!rt.IsCreated())
            {
                rt.Create();
            }
            return rt;
        }

        #endregion

        #region Brushes

        /// <summary>
        /// Projects a texture on the object orthogonally
        /// </summary>
        /// <param name="position">Position of the projection</param>
        /// <param name="forward">Direction of the projection</param>
        /// <param name="up">Up direction of the projection</param>
        /// <param name="brushTexture">Texture to project; alpha = intensity</param>
        /// <param name="brushSize">Size of the brush in units</param>
        /// <param name="brushDepth">Far clipping plane distance for the projection</param>
        /// <param name="amount">Fluid amount added at full intensity</param>
        /// <param name="space">Space of the given position and direction data</param>
        public void DrawTexture(Vector3 position, Vector3 forward, Vector3 up, Texture2D brushTexture, float brushSize, float brushDepth, float amount, Space space = Space.World)
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            if (space == Space.World)
            {
                position = rendererTransform.InverseTransformPoint(position);
                forward = rendererTransform.InverseTransformDirection(forward.normalized);
                up = rendererTransform.InverseTransformDirection(up.normalized);
            }
            position -= (Vector3)fluidObject.MeshBounds.min;
            Vector3 right = Vector3.Cross(up, forward);

            int kernelID = simManager.textureBurshKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                flowShader.SetTexture(kernelID, simManager.texelTexRID, texelTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetFloats(simManager.drawDataID, new float[] { position.x, position.y, position.z, 0, forward.x, forward.y, forward.z, 0, right.x, right.y, right.z, 0, up.x, up.y, up.z, 0 });
            flowShader.SetBuffer(kernelID, simManager.boundsBufferID, boundsBuffer);
            flowShader.SetTexture(kernelID, simManager.flowTexWID, flowTex);
            flowShader.SetTexture(kernelID, simManager.brushTexID, brushTexture);
            flowShader.SetFloat(simManager.brushSizeInvValueID, 2f / brushSize);
            flowShader.SetFloat(simManager.amountValueID, amount * FluidSimulationManager.MULTI);
            flowShader.SetFloat(simManager.brushDepthValueID, brushDepth);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            AddedNewFluid();
        }

        /// <summary>
        /// Project a texture orthogonally on the deformed object (FluidRenderer has to be a SkinnedMeshRenderer)
        /// </summary>
        /// <param name="bone">Closest bone to the brush position</param>
        /// <param name="position">Position of the projection (world space)</param>
        /// <param name="forward">Direction of the projection (world space)</param>
        /// <param name="up">Up direction of the projection (world space)</param>
        /// <param name="brushTexture">Texture to project; alpha = intensity</param>
        /// <param name="brushSize">Size of the brush in units</param>
        /// <param name="brushDepth">Far clipping plane distance for the projection</param>
        /// <param name="amount">Fluid amount added at full intensity</param>
        /// <param name="space">Space of the given position and direction data</param>
        public void SkinnedDrawTexture(Transform bone, Vector3 position, Vector3 forward, Vector3 up, Texture2D brushTexture, float brushSize, float brushDepth, float amount, Space space = Space.World)
        {
            int boneId = -1;
            if (isSkinned() && boneIdMap.TryGetValue(bone, out boneId))
            {
                if (space == Space.Self)
                {
                    position = rendererTransform.TransformPoint(position);
                    forward = rendererTransform.TransformDirection(forward.normalized);
                    up = rendererTransform.TransformDirection(up.normalized);
                }

                position = fluidObject.TransformPointToDefaultPose(bone, boneId, position);
                forward = fluidObject.TransformDirectionToDefaultPose(bone, boneId, forward);
                up = fluidObject.TransformDirectionToDefaultPose(bone, boneId, up);
                DrawTexture(position, forward, up, brushTexture, brushSize, brushDepth, amount, Space.Self);
            }
            else
            {
#if UNITY_EDITOR
                if (!isSkinned())
                    Debug.LogWarning("Not using a SkinnedMeshRenderer or FluidObject has no skeleton data!");
                if (!boneIdMap.ContainsKey(bone))
                    Debug.LogWarning("Given bone is not part of skeleton!");
#endif
                DrawTexture(position, forward, up, brushTexture, brushSize, brushDepth, amount, space);
            }
        }


        /// <summary>
        /// Sphere brush
        /// </summary>
        /// <param name="center">Center of the sphere</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="amount">Fluid amount added</param>
        /// <param name="space">Space of the given position data</param>
        public void DrawSphere(Vector3 center, float radius, float amount, Space space = Space.World)
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            if (space == Space.World)
            {
                center = rendererTransform.InverseTransformPoint(center);
            }

            center -= (Vector3)fluidObject.MeshBounds.min;

            int kernelID = simManager.sphereBurshKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                flowShader.SetTexture(kernelID, simManager.texelTexRID, texelTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetFloats(simManager.drawDataID, new float[] { center.x, center.y, center.z, 0 });
            flowShader.SetBuffer(kernelID, simManager.boundsBufferID, boundsBuffer);
            flowShader.SetTexture(kernelID, simManager.flowTexWID, flowTex);
            flowShader.SetFloat(simManager.sqrRadiusValueID, radius * radius * .25f);
            flowShader.SetFloat(simManager.amountValueID, amount * FluidSimulationManager.MULTI);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            AddedNewFluid();
        }

        /// <summary>
        /// Sphere brush on deformed object (FluidRenderer is a SkinnedMeshRenderer)
        /// </summary>
        /// <param name="bone">Closest bone to the brush position</param>
        /// <param name="center">Center of the sphere (world space)</param>
        /// <param name="radius">Radius of the sphere</param>
        /// <param name="amount">Fluid amount added</param>
        public void SkinnedDrawSphere(Transform bone, Vector3 center, float radius, float amount, Space space = Space.World)
        {
            int boneId = -1;
            if (isSkinned() && boneIdMap.TryGetValue(bone, out boneId))
            {
                if (space == Space.Self)
                {
                    center = rendererTransform.TransformPoint(center);
                }

                center = fluidObject.TransformPointToDefaultPose(bone, boneId, center);
                DrawSphere(center, radius, amount, Space.Self);
            }
            else
            {
#if UNITY_EDITOR
                if (!isSkinned())
                    Debug.LogWarning("Not using a SkinnedMeshRenderer or FluidObject has no skeleton data!");
                if (!boneIdMap.ContainsKey(bone))
                    Debug.LogWarning("Given bone is not part of skeleton!");
#endif
                DrawSphere(center, radius, amount, space);
            }
        }

        /// <summary>
        /// Disc brush
        /// </summary>
        /// <param name="center">Center point of the disc</param>
        /// <param name="normal">Normal direction</param>
        /// <param name="radius">Radius of the disc</param>
        /// <param name="depth">Thickness of the disc</param>
        /// <param name="amount">Fluid amount added</param>
        /// <param name="space">Space of the given position and direction data</param>
        public void DrawDisc(Vector3 center, Vector3 normal, float radius, float depth, float amount, Space space = Space.World)
        {
            if (!simManager.COMPUTESHADER_SUPPORT) return;

            if (space == Space.World)
            {
                center = rendererTransform.InverseTransformPoint(center);
                normal = rendererTransform.InverseTransformDirection(normal.normalized);
            }

            center -= (Vector3)fluidObject.MeshBounds.min;

            int kernelID = simManager.discBrushKernelID;
            if (simManager.USE_INTEGER_RENDERTEXTURE)
            {
                flowShader.SetTexture(kernelID, simManager.texelTexRID, texelTex);
            }
            else
            {
                kernelID++;
                flowShader.SetBuffer(kernelID, simManager.texelBufferID, texelBuf);
                flowShader.SetInt(simManager.texSizeValueID, texSize);
            }
            flowShader.SetFloats(simManager.drawDataID, new float[] { center.x, center.y, center.z, 0, normal.x, normal.y, normal.z, 0 });
            flowShader.SetBuffer(kernelID, simManager.boundsBufferID, boundsBuffer);
            flowShader.SetTexture(kernelID, simManager.flowTexWID, flowTex);
            flowShader.SetFloat(simManager.amountValueID, amount * FluidSimulationManager.MULTI);
            flowShader.SetFloat(simManager.brushDepthValueID, depth * depth * .25f);
            flowShader.SetFloat(simManager.sqrRadiusValueID, radius * radius * .25f);
            flowShader.Dispatch(kernelID, groupCount, groupCount, 1);

            AddedNewFluid();
        }

        /// <summary>
        /// Disc brush on deformed object (FluidRenderer is a SkinnedMeshRenderer)
        /// </summary>
        /// <param name="bone">Closest bone to the brush position</param>
        /// <param name="center">Center of the disc (world space)</param>
        /// <param name="normal">Normal direction (world space)</param>
        /// <param name="radius">Radius of the disc</param>
        /// <param name="depth">Thickness of the disc</param>
        /// <param name="amount">Fluid amount added</param>
        public void SkinnedDrawDisc(Transform bone, Vector3 center, Vector3 normal, float radius, float depth, float amount, Space space = Space.World)
        {
            int boneId = -1;
            if (isSkinned() && boneIdMap.TryGetValue(bone, out boneId))
            {
                if (space == Space.Self)
                {
                    center = rendererTransform.TransformPoint(center);
                    normal = rendererTransform.TransformDirection(normal.normalized);
                }

                center = fluidObject.TransformPointToDefaultPose(bone, boneId, center);
                normal = fluidObject.TransformDirectionToDefaultPose(bone, boneId, normal);
                DrawDisc(center, normal, radius, depth, amount, Space.Self);
            }
            else
            {
#if UNITY_EDITOR
                if (!isSkinned())
                    Debug.LogWarning("Not using a SkinnedMeshRenderer or FluidObject has no skeleton data!");
                if (!boneIdMap.ContainsKey(bone))
                    Debug.LogWarning("Given bone is not part of skeleton!");
#endif
                DrawDisc(center, normal, radius, depth, amount, space);
            }
        }

        private void AddedNewFluid()
        {
            updateCounter = 0;
            totalMaxFluid = roughness * FluidSimulationManager.MULTI;
        }

        #endregion

        #region Destroy
        private void OnDestroy()
        {

            if (!Initialized) return;

            //remove from queue
            if (isInUpdateQueue)
                simManager.RemoveSimulator(this);

            simManager.SimulationCycle -= simulationCycle;

            //release simulation data
            if (simManager.COMPUTESHADER_SUPPORT)
            {
                RenderTexture.ReleaseTemporary(flowTex);
                if (simManager.USE_INTEGER_RENDERTEXTURE)
                {
                    RenderTexture.ReleaseTemporary(fluidTex);
                }
                else
                {
                    fluidBuf.Release();
                }

                gravityBuffer.Release();
                if (simManager.ASYNCGPUREADBACK_SUPPORT && dropBuffer != null)
                {
                    dropBuffer.Release();
                    countBuffer.Release();
                }
            }
        }
        #endregion

    }
}
