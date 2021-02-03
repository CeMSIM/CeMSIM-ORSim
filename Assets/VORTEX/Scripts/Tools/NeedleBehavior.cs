using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NeedleBehavior : MonoBehaviour
{
    public bool freezeRotation=false;
    public Transform needleTip;

    XRGrabInteractable _xrInteractable;
    Rigidbody _rb;

    //Constants from XRGrabInteractable
    const float k_VelocityPredictionFactor = 0.6f;
    const float k_AngularVelocityDamping = 0.95f;

    // Start is called before the first frame update
    void Start()
    {
        _xrInteractable = this.GetComponent<XRGrabInteractable>();
        _rb = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void NeedleInserted(bool isInserted)
    {
        if(isInserted)
        {
            _xrInteractable.gravityOnDetach = false;
            FreezeRotationUpdate(true);
        }
        else
        {
            _xrInteractable.gravityOnDetach = true;
            FreezeRotationUpdate(false);
        }
    }

    private void FreezeRotationUpdate(bool _freeze)
    {
        //Stop rotation control from XR interactable object
        _xrInteractable.trackRotation = !_freeze;
        _xrInteractable.trackPosition = !_freeze;

        //Freeze Rotation
        _rb.freezeRotation = _freeze;

       
    }
}
