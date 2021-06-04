using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidDrop : MonoBehaviour
{
    private float amount;

    public void SetAmount(float amount)
    {
        this.amount = amount;
    }

    public float GetAmount()
    {
        return this.amount;
    }
}
