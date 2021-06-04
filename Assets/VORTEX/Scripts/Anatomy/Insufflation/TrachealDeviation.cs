using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrachealDeviation : MonoBehaviour
{
    [SerializeField]
    private int deviationKeyIndex = 0;
    [SerializeField]
    private int deviationPercent = 0;
    private SkinnedMeshRenderer meshRenderer;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = this.GetComponent<SkinnedMeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetDeviationPercent(int value)
    {
        if(value <= 100)
        {
            deviationPercent = value;
            meshRenderer.SetBlendShapeWeight(deviationKeyIndex, deviationPercent);
        }
        
    }

}
