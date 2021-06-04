using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluidSimulation;

public class NeedleBleedingBehavior : MonoBehaviour
{
    public FluidSimulator simulator;
    public float radius = .02f;
    public float amount = 5f;
    public Vector3 contactPoint;
    public Collider collisionCollider;
    private float timer;
    private float timeLimit = 1;
    private bool collisionInitiated = false;
    public LayerMask mask;

    private void Update()
    {
        if(collisionInitiated)
        {
            if(timer < timeLimit)
            {
                timer += Time.deltaTime * 3f;
            }

            else if(timer >= timeLimit)
            {
                collisionCollider.isTrigger = true;
                timer = 0;
            }
        }
        else
        {
            collisionCollider.isTrigger = false;
        }
    }



    public void OnCollisionEnter(Collision c)
    {
        if((mask.value & 1<<c.gameObject.layer) == 1<<c.gameObject.layer)
        {
            collisionInitiated = true;

            ContactPoint contact = c.GetContact(0);
            contactPoint = contact.point;
            Debug.Log("collided with " + c.gameObject.name);
        }
    }


    public void OnTriggerExit(Collider other)
    {

        if((mask.value & 1<<other.gameObject.layer) == 1<<other.gameObject.layer)
        {
            simulator.DrawSphere(contactPoint, radius, amount);
            StartCoroutine(DelayedEnableCollider());
        }
        
    }
    IEnumerator DelayedEnableCollider()
    {
        yield return new WaitForSecondsRealtime(.2f);

        collisionInitiated = false;
    }
}