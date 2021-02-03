using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinDynamics : MonoBehaviour
{
    [Header("Needle Dynamics")]
    public GameObject deformableLayerNeedle;
    public float tipTolerance = .02f;

    [Header("Needle debugging")]
    public bool needleInserted = false; //TO DO: Expand to handle more than one needle being inserted (AR 12.8.20)
    public NeedleBehavior needleObject;
    public GameObject needleEntry;
    public Vector3 needleEntryPoint;
    public Vector3 needleDirection;

    //[Header("Cutting Dynamics")]


    public bool inSpace = false;

    //Needle Dynamics Privates
    private Vector3 tipProjected;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(needleInserted)
        {
            CheckTipPosition();
        }
    }

    private void CheckTipPosition()
    {
        var _needleTip = needleObject.needleTip.position;
        tipProjected = Vector3.Project(_needleTip - needleEntryPoint, needleDirection);

        //Check if tip exceeds threhold along negative projection (TO DO: Check distance from line)
        //Debug.Log(tipProjected.magnitude);
        var sign = 1;
        if (Vector3.Dot(tipProjected, needleDirection) < 0)
            sign = -1;

        //Debug.Log(tipProjected);
        if(tipProjected.magnitude*sign < -tipTolerance)
        {
            Destroy(needleEntry);
            needleInserted = false;
            //Changes the Tool physics layer
            needleObject.gameObject.layer = LayerMask.NameToLayer("Tool");
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        var _object = collision.gameObject;
        if (_object.tag == "Tool")
        {
            //Debug.Log("Tool collide");

            //The tool is a needle
            if (_object.GetComponent<NeedleBehavior>() && !needleInserted)
            {
                //Needle is inserted
                needleInserted = true;
                needleObject = _object.GetComponent<NeedleBehavior>();

                //Stores the needle entry
                needleEntryPoint = collision.GetContact(0).point;

                //Instantiates the needle entry
                //needleEntry = Instantiate(deformableLayerNeedle, needleEntryPoint, Quaternion.identity);
                needleEntry = Instantiate(deformableLayerNeedle, needleEntryPoint, needleObject.transform.rotation);

                //Obtains the diretion of needle insertion
                needleDirection = needleObject.transform.right;
                /*Debug.DrawLine(collision.GetContact(0).point, collision.GetContact(0).point - needleDirection * 5f, Color.blue, 20f);*/

                //Changes the Tool physics layer
                needleObject.gameObject.layer = LayerMask.NameToLayer("InternalTool");

                //Obtain tip position projected onto needle direction
                var _needleTip = needleObject.needleTip.position;
                tipProjected = Vector3.Project(_needleTip - needleEntryPoint, needleDirection);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Tool")
        {
            if (!inSpace)
            {
                inSpace = true;
                //Debug.Log("Tool entered");

                var colliderLoc = other.transform.position;

                //Instantiate(deformableLayerNeedle, )
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Tool")
        {
            if (inSpace)
            {
                inSpace = false;
                //Debug.Log("Tool exited");
            }
        }
    }

}
