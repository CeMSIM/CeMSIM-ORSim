using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluidSimulation;

public class EasyFluidInteractionSkinned : MonoBehaviour
{
    public Camera cam;
    public FluidSimulator simulator;
    public float radius = .02f;
    public float amount = 5f;

    public Vector3 contactPoint;
    public Collider collisionCollider;

    // void Update()
    // {
    //     // shoot raycast from camera when clicking
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         RaycastHit hit;
    //         Ray ray = cam.ScreenPointToRay(Input.mousePosition);
    //         if (Physics.Raycast(ray, out hit, 100f))
    //         {
    //             // transform hit point to default pose (using hit boneId) and draw sphere of fluid
    //             //simulator.SkinnedDrawSphere(hit.transform, hit.point, radius, amount);
    //             Debug.Log("collided with " + hit.collider.gameObject.name);
    //             simulator.DrawSphere(hit.point, radius, amount);
    //         }
    //     }
    // }

    public void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        contactPoint = contact.point;
        collisionCollider.enabled = false;
        Debug.Log("collided with " + collision.gameObject.name);
    }

    public void OnTriggerExit(Collider other)
    {

        if(other.tag == "Patient")
        {
            simulator.DrawSphere(contactPoint, radius, amount);
            collisionCollider.enabled = true;
        }
        
    }
}
