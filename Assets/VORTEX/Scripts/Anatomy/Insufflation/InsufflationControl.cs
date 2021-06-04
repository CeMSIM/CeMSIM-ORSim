using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsufflationControl : MonoBehaviour
{
    [Range(0,20)]
    public int insufflationValue;
    [SerializeField]
    private int pneumothoraxThreshold = 12;
    private bool thresholdExceeded = false;
    private AbdominalInflation abdominalInflation;
    private InsufflationPressure insufflationPressure;
    private JugularVenousDistension jugularVenousDistension;
    private TrachealDeviation trachealDeviation;

    // Start is called before the first frame update
    void Start()
    {
        abdominalInflation = this.GetComponent<AbdominalInflation>();
        insufflationPressure = this.GetComponent<InsufflationPressure>();
        jugularVenousDistension = this.GetComponent<JugularVenousDistension>();
        trachealDeviation = this.GetComponent<TrachealDeviation>();
    }

    // Update is called once per frame
    void Update()
    {
        insufflationPressure.SetPressure(insufflationValue);
        int p = insufflationPressure.GetPercentInflated();

        if(p > 0)
        {
            float severity = ScenarioManager.Instance.pneumothoraxSeverity;
            jugularVenousDistension.SetLeftDistension((int)(p * (1.2f - severity)));
            jugularVenousDistension.SetRightDistension((int)(p *(1.2f - severity)));
            trachealDeviation.SetDeviationPercent((int)(p * (1.2f - severity)));

            abdominalInflation.SetPercentInflated(p);
            
        }

        if(!thresholdExceeded)
        {
            if(insufflationValue > pneumothoraxThreshold)
            {
                ScenarioManager.Instance.StartScenario();
                thresholdExceeded = true;
            }
        }
    }
}
