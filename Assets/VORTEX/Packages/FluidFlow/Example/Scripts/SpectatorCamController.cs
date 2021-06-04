using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorCamController : MonoBehaviour {
    public float RotSpeed;
    public float ZoomSpeed;
    public float Dampen;

    public float Distance;
    public Vector2 MinMaxDist;

    public Transform Target;
    private Transform me;

    private enum MouseEvent{
        none,
        left,
        right
    }
    private MouseEvent currentMouseEvent;

    private Vector2 deltaRot;
    private float deltaZoom;
    private Vector2 currentRotation;

    private void Awake()
    {
        me = this.transform;
        currentMouseEvent = MouseEvent.none;
        currentRotation = new Vector2(275f, 45f);
    }

    private void Update()
    {
        if (currentMouseEvent != MouseEvent.left)
        {
            deltaRot = Vector2.Lerp(deltaRot, Vector2.zero, Dampen * Time.deltaTime);
        }
        if (currentMouseEvent != MouseEvent.right)
        {
            deltaZoom = Mathf.Lerp(deltaZoom, 0, Dampen * Time.deltaTime);
        }

        if (currentMouseEvent == MouseEvent.none)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                currentMouseEvent = MouseEvent.left;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                currentMouseEvent = MouseEvent.right;
            }
        }
        else
        {
            if (currentMouseEvent == MouseEvent.left)
            {
                deltaRot = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
            }
            else
            {
                deltaZoom = -Input.GetAxis("Mouse Y");
            }

            if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetMouseButtonUp(1))
            {
                currentMouseEvent = MouseEvent.none;
            }
        }

        
        currentRotation.y = Mathf.Clamp(currentRotation.y + deltaRot.y * RotSpeed, -85f, 85f);
        currentRotation.x = (currentRotation.x + deltaRot.x * RotSpeed) % 360f;
        Distance = Mathf.Clamp(Distance + deltaZoom * ZoomSpeed, MinMaxDist.x, MinMaxDist.y);

        Vector3 pos = Target.position + Quaternion.Euler(0, currentRotation.x, 0) * (Quaternion.Euler(0, 0, currentRotation.y) * Vector3.right * Distance);

        me.position = pos;
        me.LookAt(Target);
    }

}
