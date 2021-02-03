using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatientEvents : MonoBehaviour
{
    private static PatientEvents _instance;
    public static PatientEvents Instance { get { return _instance; } }

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
    public event Action NeedleInserted;

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

    public void TriggerNeedleInserted()
    {
        if (NeedleInserted != null)
        {
            NeedleInserted();
        }
    }
}
