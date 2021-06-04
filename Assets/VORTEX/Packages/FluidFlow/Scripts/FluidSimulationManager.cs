using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    public class FluidSimulationManager : MonoBehaviour
    {

        #region Public
        public enum UpdateMode { Frame, Time};
        public UpdateMode updateMode = UpdateMode.Frame;

        public int skipFrames = 1;
        public int cyclesPerSecond = 40;
        public int maximumUpdatesPerCycle = 4;
        public bool onlyVisible = false;
        /// <summary>
        /// The amount of fluid that has to evaporate before the texture is updated. When the simulator is inactive
        /// </summary>
        public float minimumEvaporationDelta = 0.01f;

        public const int MULTI = 1000;

        public bool COMPUTESHADER_SUPPORT { get; private set; }
        public bool ASYNCGPUREADBACK_SUPPORT { get; private set; }

        // only editable through inspector; disable for mobile support
        public bool USE_INTEGER_RENDERTEXTURE { get { return useIntegerRendertexture; } }
        [SerializeField]
        private bool useIntegerRendertexture = true;

        //Load the compute shader
        private ComputeShader _simulationShader;
        public ComputeShader simulationShader
        {
            get
            {
                if (_simulationShader)
                {
                    return _simulationShader;
                }
                _simulationShader = (ComputeShader)Resources.Load("FluidSimulation");
                cacheShaderIDs(_simulationShader);
                return _simulationShader;
            }
        }

        /// <summary>
        /// Enqueue in for fluid simulation
        /// </summary>
        public void EnqueueSimulator(FluidSimulator sim)
        {
            updateQueue.Enqueue(sim);
        }

        /// <summary>
        /// Remove simulator from queue (e.g. when destroyed)
        /// </summary>
        /// <param name="sim"></param>
        public void RemoveSimulator(FluidSimulator sim)
        {
            if (updateQueue.Contains(sim))
            {
                Queue<FluidSimulator> tmpQueue = new Queue<FluidSimulator>();
                foreach (FluidSimulator simulator in updateQueue)
                {
                    if (simulator != sim)
                    {
                        tmpQueue.Enqueue(simulator);
                    }
                }
                updateQueue = tmpQueue;
            }
        }

        /// <summary>
        /// Check if the current system supports Compute Shaders and Async GPU Readback
        /// Results are stored in: COMPUTESHADER_SUPPORT; ASYNCGPUREADBACK_SUPPORT; INTEGERRENDERTEXTURE_SUPPORT
        /// </summary>
        public void CheckComputeShaderSupport()
        {
            COMPUTESHADER_SUPPORT = SystemInfo.supportsComputeShaders;

#if UNITY_2018_2_OR_NEWER
            ASYNCGPUREADBACK_SUPPORT = SystemInfo.supportsAsyncGPUReadback;
#endif

            if (!COMPUTESHADER_SUPPORT)
            {
                Debug.LogWarning("Fluid Simulation not possible! Your system does not support Compute Shaders.");
            }
            if (!ASYNCGPUREADBACK_SUPPORT)
            {
                Debug.LogWarning("Your system does not support Async GPU Readback. Simulation will not be able to calculate fluid drips!");
            }
        }

        public event System.Action SimulationCycle;

        #endregion

        #region Private

        private Queue<FluidSimulator> updateQueue = new Queue<FluidSimulator>();

        
        private UpdateMode lastUpdateMode;
        private int currentCyclesPerSecond = -1;
        private int skippedFrames = 0;

        private void UpdateInvokeTime()
        {
            if (IsInvoking())
            {
                CancelInvoke("simulationCycle");
            }
            cyclesPerSecond = Mathf.Max(1, cyclesPerSecond);
            InvokeRepeating("simulationCycle", 0, 1f / cyclesPerSecond);
            currentCyclesPerSecond = cyclesPerSecond;
        }

        #endregion

        #region Singleton

        private static FluidSimulationManager _instance;
        public static FluidSimulationManager instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<FluidSimulationManager>();

                    if (_instance)
                    {
                        _instance.CheckComputeShaderSupport();
                        return _instance;
                    }

                    GameObject go = new GameObject("FluidSimulationManager");
                    _instance = go.AddComponent<FluidSimulationManager>();
                    _instance.CheckComputeShaderSupport();
                }

                return _instance;
            }
        }

        #endregion

        #region Shared Simulation Data

        /// <summary>
        /// Data shared between multiple fluid simulations with the same mesh as base
        /// </summary>
        public struct SharedSimulationData
        {
            public RenderTexture texelMap;
            public ComputeBuffer texelBuffer;
            public ComputeBuffer tangentBuffer;
            public ComputeBuffer boundsBuffer;

            public SharedSimulationData(RenderTexture texelMap, ComputeBuffer texelBuffer, ComputeBuffer tangentBuffer, ComputeBuffer boundsBuffer)
            {
                this.texelMap = texelMap;
                this.texelBuffer = texelBuffer;
                this.tangentBuffer = tangentBuffer;
                this.boundsBuffer = boundsBuffer;
            }
        }

        private Dictionary<FluidObject, SharedSimulationData> sharedSimulationData = new Dictionary<FluidObject, SharedSimulationData>();

        public SharedSimulationData GetSharedData(FluidObject fluidObject)
        {
            SharedSimulationData returnData;
            if (sharedSimulationData.TryGetValue(fluidObject, out returnData))
            {
                return returnData;
            }

            return new SharedSimulationData();
        }

        public bool HasSharedData(FluidObject mesh)
        {
            return sharedSimulationData.ContainsKey(mesh);
        }

        public void EnterSharedData(FluidObject fluidObject, SharedSimulationData simData)
        {
            if (!sharedSimulationData.ContainsKey(fluidObject))
            {
                sharedSimulationData.Add(fluidObject, simData);
            }
        }

        public void ReleaseSharedData(FluidObject fluidObject)
        {
            if (HasSharedData(fluidObject))
            {
                SharedSimulationData simData = GetSharedData(fluidObject);
                simData.boundsBuffer.Release();
                simData.tangentBuffer.Release();
                if (USE_INTEGER_RENDERTEXTURE)
                    simData.texelMap.Release();
                else
                    simData.texelBuffer.Release();
                sharedSimulationData.Remove(fluidObject);
            }
        }

        #endregion

        #region Cache Shader IDs
        // kernels
        public int loadTexelMapKernelID { get; private set; }
        public int updateGravVecKernelID { get; private set; }
        public int updateGravMapNormalKernelID { get; private set; }
        public int updateGravMapKernelID { get; private set; }
        public int updateFluidKernelID { get; private set; }
        public int calcDripKernelID { get; private set; }
        public int textureBurshKernelID { get; private set; }
        public int sphereBurshKernelID { get; private set; }
        public int discBrushKernelID { get; private set; }
        public int resetFluidKernelID { get; private set; }
        public int evapFluidKernelID { get; private set; }

        // textures
        public int texelTexRID { get; private set; }
        public int texelTexWID { get; private set; }
        public int fluidTexRID { get; private set; }
        public int fluidTexWID { get; private set; }
        public int flowTexRID { get; private set; }
        public int flowTexWID { get; private set; }
        public int brushTexID { get; private set; }
        public int normalTexID { get; private set; }
        public int finalTexID { get; private set; }

        // buffers
        public int texelBufferID { get; private set; }
        public int fluidBufferID { get; private set; }
        public int tangentBufferID { get; private set; }
        public int gravityVecBufferID { get; private set; }
        public int dropsBufferID { get; private set; }
        public int boundsBufferID { get; private set; }
        public int texelInputBufferID { get; private set; }
        public int triangleDataBufferID { get; private set; }

        // values
        public int gravityValueID { get; private set; }
        public int amountValueID { get; private set; }
        public int fluidSpeedValueID { get; private set; }
        public int roughnessValueID { get; private set; }
        public int texSizeValueID { get; private set; }
        public int texSizeInvValueID { get; private set; }
        public int trisCountValueID { get; private set; }
        public int sqrRadiusValueID { get; private set; }
        public int brushDepthValueID { get; private set; }
        public int brushSizeInvValueID { get; private set; }
        public int minDripAmtValueID { get; private set; }
        public int dryAmountValueID { get; private set; }
        public int decayAmountValueID { get; private set; }
        public int slopeAmountValueID { get; private set; }
        public int normalAmountValueID { get; private set; }
        public int drawDataID { get; private set; }

        private void cacheShaderIDs(ComputeShader compute)
        {
            loadTexelMapKernelID = compute.FindKernel("loadTexelMap");
            updateGravVecKernelID = compute.FindKernel("updateGravityVectors");
            updateGravMapNormalKernelID = compute.FindKernel("updateGravityMapNormal");
            updateGravMapKernelID = compute.FindKernel("updateGravityMap");
            updateFluidKernelID = compute.FindKernel("updateFluid");
            calcDripKernelID = compute.FindKernel("calculateDrip");
            textureBurshKernelID = compute.FindKernel("textureBrush");
            sphereBurshKernelID = compute.FindKernel("sphereBrush");
            discBrushKernelID = compute.FindKernel("discBrush");
            resetFluidKernelID = compute.FindKernel("resetFluid");
            evapFluidKernelID = compute.FindKernel("evaporateFluid");

            texelTexRID = Shader.PropertyToID("texelTexR");
            texelTexWID = Shader.PropertyToID("texelTexW");
            fluidTexRID = Shader.PropertyToID("fluidTexR");
            fluidTexWID = Shader.PropertyToID("fluidTexW");
            flowTexRID = Shader.PropertyToID("flowR");
            flowTexWID = Shader.PropertyToID("flowW");
            brushTexID = Shader.PropertyToID("brushTex");
            normalTexID = Shader.PropertyToID("normalTex");
            finalTexID = Shader.PropertyToID("finalTex");

            texelBufferID = Shader.PropertyToID("texelBuf");
            fluidBufferID = Shader.PropertyToID("fluidBuf");
            tangentBufferID = Shader.PropertyToID("tangents");
            gravityVecBufferID = Shader.PropertyToID("gravityVectors");
            dropsBufferID = Shader.PropertyToID("drops");
            boundsBufferID = Shader.PropertyToID("bounds");
            texelInputBufferID = Shader.PropertyToID("texelInput");
            triangleDataBufferID = Shader.PropertyToID("triangleData");

            gravityValueID = Shader.PropertyToID("gravity");
            amountValueID = Shader.PropertyToID("amount");
            fluidSpeedValueID = Shader.PropertyToID("speed");
            roughnessValueID = Shader.PropertyToID("roughness");
            texSizeValueID = Shader.PropertyToID("textureSize");
            texSizeInvValueID = Shader.PropertyToID("textureSizeInverse");
            trisCountValueID = Shader.PropertyToID("triangleCountUnpadded");
            sqrRadiusValueID = Shader.PropertyToID("sqrRadius");
            brushDepthValueID = Shader.PropertyToID("brushDepth");
            brushSizeInvValueID = Shader.PropertyToID("brushSizeInv");
            minDripAmtValueID = Shader.PropertyToID("minimumDripAmount");
            dryAmountValueID = Shader.PropertyToID("dryAmount");
            decayAmountValueID = Shader.PropertyToID("decayAmount");
            slopeAmountValueID = Shader.PropertyToID("slopeAmount");
            normalAmountValueID = Shader.PropertyToID("normalAmount");
            drawDataID = Shader.PropertyToID("drawData");
        }
        #endregion

        #region Update

        private void Awake()
        {
            // init as opposite
            lastUpdateMode = updateMode == UpdateMode.Frame ? UpdateMode.Time : UpdateMode.Frame;
        }

        private void Update()
        {
            if(updateMode == UpdateMode.Time)
            {
                // cycles per second changed or mode changed?
                if(currentCyclesPerSecond != cyclesPerSecond || lastUpdateMode != updateMode)
                {
                    UpdateInvokeTime();
                }
            }
            else
            {
                // mode changed?
                if(lastUpdateMode != updateMode)
                {
                    CancelInvoke("simulationCycle");
                    skippedFrames = skipFrames;
                }

                // enough frames skipped?
                if (skippedFrames >= skipFrames)
                {
                    simulationCycle();

                    skippedFrames = 0;
                }
                else
                {
                    skippedFrames++;
                }
            }
            lastUpdateMode = updateMode;
        }

        private void simulationCycle()
        {
            if (COMPUTESHADER_SUPPORT)
            {
                if (SimulationCycle != null)
                    SimulationCycle.Invoke();

                updateFluidSimulators();
            }
        }

        private void updateFluidSimulators()
        {
            int updateCount = Mathf.Min(updateQueue.Count, maximumUpdatesPerCycle);
            for (int updates = updateCount; updates > 0; updates--)
            {
                updateQueue.Dequeue().SimulateFlow();
            }
        }

        #endregion

        #region Destroy
        private void OnDestroy()
        {
            // release shared simulation data
            foreach (KeyValuePair<FluidObject, SharedSimulationData> sharedData in sharedSimulationData)
            {
                sharedData.Value.boundsBuffer.Release();
                sharedData.Value.tangentBuffer.Release();
                if (USE_INTEGER_RENDERTEXTURE)
                    sharedData.Value.texelMap.Release();
                else
                    sharedData.Value.texelBuffer.Release();
            }
        }
        #endregion
    }
}



