/* Distributed under the Apache License, Version 2.0.
   See accompanying NOTICE file for details.*/

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using Pulse;
using Pulse.CDM;

// Component used to handle a PulseEngine object, advance its simulation time,
// and broadcast the resulting data
[ExecuteInEditMode]
public class PulseEngineDriver : PulseDataSource
{
    public TextAsset initialStateFile;  // Initial stable state to load
    public SerializationFormat serializationFormat; // state file format

    [Range(0.02f, 2.0f)]
    public double timeStep = 0.02;      // Simulation time step

    [NonSerialized]
    public PulseEngine engine;          // Pulse engine to drive
    public readonly string engineDataPath = "PulseDataFiles";

    float previousTime;                 // Used to sync simulation and app times

    // Data requests that will fill our data fields
    // TODO: Dynamically define the requests through the
    // PulseEngineDriver editor instead of hardcoding them
    readonly List<SEDataRequest> data_requests = new List<SEDataRequest>
    {
        SEDataRequest.CreateECGRequest("Lead3ElectricPotential", "mV"),
        SEDataRequest.CreatePhysiologyRequest("HeartRate", "1/min"),
        SEDataRequest.CreatePhysiologyRequest("ArterialPressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("MeanArterialPressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("SystolicArterialPressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("DiastolicArterialPressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("OxygenSaturation"),
        SEDataRequest.CreatePhysiologyRequest("EndTidalCarbonDioxidePressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("RespirationRate", "1/min"),
        SEDataRequest.CreatePhysiologyRequest("SkinTemperature", "degC"),
        SEDataRequest.CreateGasCompartmentSubstanceRequest("Carina", "CarbonDioxide", "PartialPressure", "mmHg"),
        SEDataRequest.CreatePhysiologyRequest("BloodVolume", "mL"),
        SEDataRequest.CreateLiquidCompartmentRequest("RightArmVasculature", "InFlow", "mL/min")
    };

    // Create a reference to a double[] that will contain the data returned from Pulse
    protected double[] data_values;

    // MARK: Monobehavior methods

    // Called when the inspector inputs are modified
    void OnValidate()
    {
        // Round down to closest factor of 0.02. Need to use doubles due to
        // issues with floats multiplication (0.1 -> 0.0999999)
        timeStep = Math.Round(timeStep / 0.02) * 0.02;
    }

    // Called when application or editor opens
    void Awake()
    {
        // Create our data container
        data = ScriptableObject.CreateInstance<PulseData>();

        // Store data field names
        // data_values[0] is always the simulation time in seconds
        // The rest of the data values are in order of the data_requests list
        data.fields = new string[data_requests.Count+1];
        data.fields[0] = "Simulation Time(s)";
        for (int i=1; i< data.fields.Length; i++)
            data.fields[i] = data_requests[i-1].ToString().Replace("/", "\u2215");

        // Allocate space for data times and values
        data.timeStampList = new FloatList();
        data.valuesTable = new List<FloatList>(data.fields.Length);
        for (int fieldId = 0; fieldId < data.fields.Length; ++fieldId)
            data.valuesTable.Add(new FloatList());
    }

    // Called at the first frame when the component is enabled
    void Start()
    {
        // Ensure we only read data if the application is playing
        // and we have a state file to initialize the engine with
        if (!Application.isPlaying || initialStateFile == null)
            return;

        // Allocate PulseEngine with path to logs and needed data files
        string dateAndTimeVar = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string logFilePath = Application.persistentDataPath + "/" +
                                        gameObject.name +
                                        dateAndTimeVar + ".log";
        string pulseDataPath = Application.streamingAssetsPath + "/" + engineDataPath;
        DirectoryInfo directoryInfo = new DirectoryInfo(pulseDataPath);
        if (!directoryInfo.Exists)
        {
            string error = "Data files for " + name + " not found. Expected at " + pulseDataPath + ".\n" +
            "Make sure you have copied them from the Pulse package inner 'StreamingAssets' folder.";
            throw new Exception(error);
        }
        engine = new PulseEngine(logFilePath, pulseDataPath);

        SEDataRequestManager data_mgr = new SEDataRequestManager(data_requests);

        // Initialize engine state from tje state file content
        engine.SerializeFromString(initialStateFile.text,
                                   data_mgr,
                                   Time.time,
                                   serializationFormat);

        previousTime = Time.time;
    }

    // Called before every frame
    void Update()
    {
        // Ensure we only broadcast data if the application is playing
        // and there a valid pulse engine to simulate data from
        if (!Application.isPlaying || engine == null)
            return;

        // Clear PulseData container
        data.timeStampList.Clear();
        for (int j = 0; j < data.valuesTable.Count; ++j)
            data.valuesTable[j].Clear();

        // Don't advance simulation if we have waited less than the time step
        float timeElapsed = Time.time - previousTime;
        if (timeElapsed < timeStep)
            return;

        // Iterate over multiple time steps if needed
        var numberOfDataPointsNeeded = Math.Round(timeElapsed / timeStep);
        for (int i = 0; i < numberOfDataPointsNeeded; ++i)
        {
            // Increment previousTime to currentTime (factored by the time step)
            previousTime += (float)timeStep;
            data.timeStampList.Add(previousTime);

            // Advance simulation by time step
            bool success = engine.AdvanceTime_s(timeStep);
            if (!success)
                continue;

            // Copy simulated data to data container
            data_values = engine.PullData();
            for (int j = 0; j < data_values.Length ; ++j)
                data.valuesTable[j].Add((float)data_values[j]);
        }
    }

    void OnApplicationQuit()
    {
        engine = null;
    }
}
