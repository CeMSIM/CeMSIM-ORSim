using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pulse.CDM;

public enum Scenario : int
{
    LaparascopicCholecystectomy
}

public class ScenarioManager : MonoBehaviour
{
    public Scenario sceneScenario;
    [Header("== Only READBLE ==")]
    public float timeElapsed=0f; //HideinInspector once UI displays
    [HideInInspector]
    public bool isRunning = false;

    [Header("Additional Events")]
    public bool tensionPneumothorax=false;
    public float pneumothoraxSeverity = 0;

    [Header("Tension Pneumothorax Details")]
    public float timeNeedleDecompFail = 120f;
    public float timeChestTubeFail = 300f;
    public bool eventNeedleDecompression = false;
    public bool eventChestTubeInsertion = false; //TO DO: Implement Chest Tube events

    private static ScenarioManager _instance;
    public static ScenarioManager Instance { get { return _instance; } }

    protected virtual void Awake()
    {
        if (_instance = null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        PatientEvents.Instance.NeedleDecompression += OnNeedleDecompression;
        // StartScenario();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            timeElapsed += Time.deltaTime;

            if(tensionPneumothorax)
            {
                if (!eventNeedleDecompression)
                {
                    if (timeElapsed > timeNeedleDecompFail)
                    {
                        Debug.Log("Failed scenario: Did not perform Needle Decompression in time");
                        PauseScenario();
                    }
                }
                else if(timeElapsed > timeChestTubeFail)
                {
                    Debug.Log("Failed scenario: Did not perform Chest Tube insertion in time");
                    PauseScenario();
                }
            }

            PatientManager.Instance.CheckTime(4);
        }
    }

    public void StartScenario()
    {
        timeElapsed = 0f;
        isRunning = true;
        PatientEvents.Instance.TriggerPatientPneumothorax();
    }

    public void PauseScenario()
    {
        isRunning = false;
    }
    public void UnPauseScenario()
    {
        isRunning = true;
    }

    private void OnNeedleDecompression()
    {
        eventNeedleDecompression = true;
    }
}
