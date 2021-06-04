using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pulse.CDM;

public class PulseEventManager : MonoBehaviour
{
    private PulseActionManager actionManager;

    public delegate void TriggerAction();
    public static event TriggerAction triggerAction;

    public delegate void AdministerDrug();
    public static event AdministerDrug administerDrug;

   // private void OnEnable()
   // {
   //     PatientEvents.Instance.PatientPneumothorax += TriggerPulseAction;
   // }
   // private void OnDisable()
   // {
   //     PatientEvents.Instance.PatientPneumothorax -= TriggerPulseAction;
   // }

    private void Start()
    {
        actionManager = this.GetComponent<PulseActionManager>();
    }

    public void TriggerPulseAction(Pulse.CDM.PulseAction action)
    {
        actionManager.action = action;

        triggerAction?.Invoke();
    }

    public void TriggerPulseAction(Pulse.CDM.PulseAction action, float severity)
    {
        actionManager.action = action;
        actionManager.pneumothoraxSeverity = severity;
        ScenarioManager.Instance.pneumothoraxSeverity = severity;

        triggerAction?.Invoke();
    }

   public void AdministerMedication(string medicationName)
   {
       switch (medicationName)
       {
           case "Epinephrine":
               {
                   actionManager.drug = Pulse.CDM.Drug.StartEpinephrineInfusion;
                   break;
               }
           case "Succinylcholine":
               {
                   actionManager.drug = Pulse.CDM.Drug.StartSuccinylcholineInfusion;
                   break;
               }
           case "Propofol":
               {
                   actionManager.drug = Pulse.CDM.Drug.StartPropofolInfusion;
                   break;
               }
           case "Rocuronium":
               {
                   actionManager.drug = Pulse.CDM.Drug.StartRocuroniumInfusion;
                   break;
               }
           default:
               {
                   break;
               }
       }

        administerDrug?.Invoke();
    }    
}