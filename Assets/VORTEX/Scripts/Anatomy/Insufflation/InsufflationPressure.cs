using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InsufflationPressure : MonoBehaviour
{
    // [SerializeField]
    [Range(0,100)]
    public int percentInflated = 0;
    [SerializeField]
    private float minPressure = 0;
    [SerializeField]
    private float maxPressure = 15;
    [SerializeField]
    private float currentPressure = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SetPercentInflated((int)((currentPressure/maxPressure) * 100));         //TODO: MH 5/20/21 | move this functionality elsewhere; should only update when there is a value change from insufflation control
    }


    public void SetPressure(float pressure)
    {
            currentPressure = pressure;
    }

    public float GetPressure()
    {
        return currentPressure;
    }

    public int GetPercentInflated()
    {
        return percentInflated;
    }
    public void SetPercentInflated(int p)
    {
        if(p < 100)
        {
            percentInflated = p;
        }
    }
    
}
