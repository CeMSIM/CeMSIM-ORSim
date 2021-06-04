using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medication : MonoBehaviour
{
    public enum Drugs : int
    {
        Epinephrine,
        Succinylcholine,
        Propofol,
        Rocuronium
    }

    public Drugs medication;
}
