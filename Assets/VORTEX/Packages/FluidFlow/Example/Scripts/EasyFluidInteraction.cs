using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluidSimulation;

public class EasyFluidInteraction : MonoBehaviour
{
    public Camera cam;
    public FluidSimulator simulator;
    public float radius = .02f;
    public float amount = 5f;

    void Update()
    {
        // shoot raycast from camera when clicking
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100f))
            {
                // draw sphere of fluid at hit position
                simulator.DrawSphere(hit.point, radius, amount);
            }
        }
    }
}
