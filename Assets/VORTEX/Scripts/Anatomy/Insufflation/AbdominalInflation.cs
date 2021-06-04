using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbdominalInflation : MonoBehaviour
{
    [SerializeField]
    private int shapeKeyIndex = 0;
    public float percentInflated = 0;
    private SkinnedMeshRenderer meshRenderer;
    public InsufflationPressure insufflationPressure;
    private int lastPressure = 0;

    void Awake()
    {
        meshRenderer = this.GetComponent<SkinnedMeshRenderer>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int p = insufflationPressure.GetPercentInflated();
        if(p != lastPressure)
        {
            SetPercentInflated(p);
        }

        lastPressure = p;
    }

    public void SetPercentInflated(float p)
    {
        // if(p > 1)
        // {
        //     p = p/100;
        // }
        percentInflated = p;
        meshRenderer.SetBlendShapeWeight(shapeKeyIndex, p);
    }
}
