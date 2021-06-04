using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JugularVenousDistension : MonoBehaviour
{
    [SerializeField]
    private int leftJVDKeyIndex = 0;
    [SerializeField]
    private int rightJVDKeyIndex = 0;
    [SerializeField]
    private int percentInflatedLeft = 0;
    [SerializeField]
    private int percentInflatedRight = 0;
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

    public void SetLeftDistension(int value)
    {
        if(value <= 100)
        {
            percentInflatedLeft = value;
            meshRenderer.SetBlendShapeWeight(leftJVDKeyIndex, percentInflatedLeft);
        }
    }

    public void SetRightDistension(int value)
    {
        if(value <= 100)
        {
            percentInflatedRight = value;
            meshRenderer.SetBlendShapeWeight(rightJVDKeyIndex, percentInflatedRight);
        }
    }
}
