using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualOverrideVital : MonoBehaviour
{
    private PulseDataNumberRenderer vitalToChange;
    public PulseDataNumberRenderer vitalToMonitor;
    public bool over = false;
    public int threshold;

    private int delayBuffer = 3;
    private bool overrode = false;

    // Start is called before the first frame update
    void Start()
    {
        vitalToChange = this.GetComponent<PulseDataNumberRenderer>();
    }

    void LateUpdate()
    {
        //buffer is used to avoid setting vitalToChange to zero during intialization processes
        if(delayBuffer > 0)
        {
            delayBuffer--;
        }
        else if(!overrode)
        {
            over = vitalToMonitor.currentValue >= threshold ? true : false;

            if(!over)
            {
                overrode = true;
                vitalToChange.multiplier = 0;
                Debug.Log("under thresh");
            }
        }
       //else
       //{
           // if(vitalToMonitor.currentValue <= threshold)
           // {
           //     vitalToChange.multiplier = 0;
           // }
        //}
    }
}
