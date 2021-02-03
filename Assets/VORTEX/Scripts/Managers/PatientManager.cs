using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pulse.CDM;

public class PatientManager : MonoBehaviour
{
    [HideInInspector]
    public PulseEngineDriver pulseEngineDriver;
    [HideInInspector]
    public PulseEventManager pulseEventManager;

    private static PatientManager _instance;
    public static PatientManager Instance { get { return _instance; } }

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

    void Start()
    {
        pulseEngineDriver = this.GetComponent<PulseEngineDriver>();
        pulseEventManager = this.GetComponent<PulseEventManager>();

        PatientEvents.Instance.NeedleDecompression += OnNeedleDecompression;
        PatientEvents.Instance.PatientPneumothorax += OnTriggerPneumothorax;
    }

    private void OnTriggerPneumothorax()
    {
        //Trigger Pneumothorax
        pulseEventManager.TriggerPulseAction(Pulse.CDM.PulseAction.TensionPneumothorax);
    }

    private void OnNeedleDecompression()
    {
        pulseEventManager.TriggerPulseAction(Pulse.CDM.PulseAction.NeedleDecompressions);
    }

    private void OnNeedleEnter()
    {
        Debug.Log("Needle inserted into patient");
    }
}
