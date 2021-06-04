using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatientEvents : MonoBehaviour
{
    private static PatientEvents _instance;
    public static PatientEvents Instance { get { return _instance; } }

    //public PulseEventManager eventManager;

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

    public event Action PatientPneumothorax;
    public event Action NeedleDecompression;


    public void TriggerPatientPneumothorax()
    {
        if (PatientPneumothorax != null)
        {
            PatientPneumothorax();
        }
    }

    public void TriggerNeedleDecompression()
    {
        if (NeedleDecompression != null)
        {
            NeedleDecompression();
        }
    }

  // public void AdministerMedication(string medicationName)
  // {
  //     switch (medicationName)
  //     {
  //         case "Epinephrine":
  //             {
  //                 actionManager.drug = Pulse.CDM.Drug.StartEpinephrineInfusion;
  //                 break;
  //             }
  //         case "Succinylcholine":
  //             {
  //                 actionManager.drug = Pulse.CDM.Drug.StartSuccinylcholineInfusion;
  //                 break;
  //             }
  //         case "Propofol":
  //             {
  //                 actionManager.drug = Pulse.CDM.Drug.StartPropofolInfusion;
  //                 break;
  //             }
  //         case "Rocuronium":
  //             {
  //                 actionManager.drug = Pulse.CDM.Drug.StartRocuroniumInfusion;
  //                 break;
  //             }
  //         default:
  //             {
  //                 break;
  //             }
  //     }
  // }

}
