using System.Collections.Generic;
using System.Linq;
using HurricaneVR.Framework.Shared;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HurricaneVR.Framework.Core.UI
{
    public class HVRInputModule : PointerInputModule
    {
        [Tooltip("Button used to toggle presses.")]
        public HVRButtons PressButton;
        [Tooltip("Canvases for UI pointer interation.")]
        public List<Canvas> UICanvases = new List<Canvas>();

        public HVRGraphicRaycaster GraphicRaycaster;
        public List<HVRUIPointer> Pointers { get; private set; }


        public float AngleDragThreshold = 1f;

        protected override void Awake()
        {
            Pointers = FindObjectsOfType<HVRUIPointer>().ToList();

            foreach (var pointer in Pointers)
            {
                pointer.PointerEventData = new PointerEventData(eventSystem);
            }
        }

        public void AddPointer(HVRUIPointer pointer)
        {
            if (Pointers == null)
            {
                Pointers = new List<HVRUIPointer>();
            }

            if (!Pointers.Contains(pointer))
            {
                Pointers.Add(pointer);
                pointer.PointerEventData = new PointerEventData(eventSystem);
            }
        }

        public void RemovePointer(HVRUIPointer pointer)
        {
            if(Pointers == null)
                return;

            if (Pointers.Contains(pointer))
            {
                HandlePointerExitAndEnter(pointer.PointerEventData, null);
                RemovePointerData(pointer.PointerEventData);
                Pointers.Remove(pointer);

                if (Pointers.Count == 0)
                    Pointers = null;
            }
        }

        public override void Process()
        {
            for (var j = 0; j < Pointers.Count; j++)
            {
                var pointer = Pointers[j];
                if (!pointer || pointer.Camera == null || !pointer.isActiveAndEnabled)
                    continue;

                pointer.CurrentUIElement = null;

                for (var i = 0; i < UICanvases.Count; i++)
                {
                    var canvas = UICanvases[i];
                    canvas.worldCamera = pointer.Camera;
                }


                pointer.Process();

                eventSystem.RaycastAll(pointer.PointerEventData, m_RaycastResultCache);
                pointer.PointerEventData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);

                m_RaycastResultCache.Clear();
                pointer.CurrentUIElement = pointer.PointerEventData.pointerCurrentRaycast.gameObject;

                // Removed continue because it causes lingering hover after pointer moves off of raycast-able objects
                //if (!pointer.PointerEventData.pointerCurrentRaycast.isValid)
                //    continue;

                var screenPosition = (Vector2)pointer.Camera.WorldToScreenPoint(pointer.PointerEventData.pointerCurrentRaycast.worldPosition);
                var delta = screenPosition - pointer.PointerEventData.position;
                pointer.PointerEventData.position = screenPosition;
                pointer.PointerEventData.delta = delta;

                var buttonState = HVRController.GetButtonState(pointer.HandSide, PressButton);

                ProcessMove(pointer.PointerEventData);

                if (buttonState.JustActivated && pointer.PointerEventData.pointerCurrentRaycast.isValid)
                {
                    ProcessPress(pointer);
                }
                else if (buttonState.Active && pointer.PointerEventData.pointerCurrentRaycast.isValid)
                {
                    ProcessDrag(pointer.PointerEventData);
                }
                else if (buttonState.JustDeactivated)
                {
                    ProcessRelease(pointer);
                }
            }

            for (var i = 0; i < UICanvases.Count; i++)
            {
                var canvas = UICanvases[i];
                canvas.worldCamera = null;
            }
        }

        protected override void ProcessDrag(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
                return;

            if (!eventData.dragging)
            {
                var cameraPos = eventData.pressEventCamera.transform.position;
                var pressDir = (eventData.pointerPressRaycast.worldPosition - cameraPos).normalized;
                var currentDir = (eventData.pointerCurrentRaycast.worldPosition - cameraPos).normalized;
                var dragThresholdMet = Vector3.Dot(pressDir, currentDir) < Mathf.Cos(Mathf.Deg2Rad * (AngleDragThreshold));
                //if (!dragThresholdMet)
                //    return;

                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.beginDragHandler);
                eventData.dragging = true;
                return;
            }

            if (eventData.pointerPress != eventData.pointerDrag)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);
                eventData.eligibleForClick = false;
                eventData.pointerPress = null;
                eventData.rawPointerPress = null;
            }
            ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.dragHandler);
        }

        private void ProcessPress(HVRUIPointer pointer)
        {
            var eventData = pointer.PointerEventData;
            eventData.eligibleForClick = true;
            eventData.delta = Vector2.zero;
            eventData.dragging = false;
            eventData.useDragThreshold = true;
            eventData.pressPosition = eventData.position;
            eventData.pointerPressRaycast = eventData.pointerCurrentRaycast;

            DeselectIfSelectionChanged(pointer.CurrentUIElement, eventData);

            var pressed = ExecuteEvents.ExecuteHierarchy(pointer.CurrentUIElement, eventData, ExecuteEvents.pointerDownHandler);
            if (pressed == null)
            {
                pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointer.CurrentUIElement);
            }

            eventData.pointerPress = pressed;
            eventData.rawPointerPress = pointer.CurrentUIElement;
            eventData.clickTime = Time.unscaledTime;
            eventData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(pointer.CurrentUIElement);
            if (eventData.pointerDrag != null)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.initializePotentialDrag);
        }

        private void ProcessRelease(HVRUIPointer pointer)
        {
            var eventData = pointer.PointerEventData;

            ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerUpHandler);

            var handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointer.CurrentUIElement);
            if (eventData.pointerPress == handler && eventData.eligibleForClick)
            {
                ExecuteEvents.Execute(eventData.pointerPress, eventData, ExecuteEvents.pointerClickHandler);
            }
            else if (eventData.pointerDrag != null)
            {
                ExecuteEvents.ExecuteHierarchy(pointer.CurrentUIElement, eventData, ExecuteEvents.dropHandler);
            }

            if (eventData.pointerDrag != null && eventData.dragging)
                ExecuteEvents.Execute(eventData.pointerDrag, eventData, ExecuteEvents.endDragHandler);

            eventData.eligibleForClick = false;
            eventData.dragging = false;
            eventData.pointerDrag = null;
            eventData.pressPosition = Vector2.zero;
            eventData.pointerPress = null;
            eventData.rawPointerPress = null;

            if (pointer.CurrentUIElement != eventData.pointerEnter)
            {
                HandlePointerExitAndEnter(eventData, null);
                HandlePointerExitAndEnter(eventData, pointer.CurrentUIElement);
            }
        }

    }
}
