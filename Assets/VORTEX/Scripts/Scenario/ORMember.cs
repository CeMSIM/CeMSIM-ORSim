using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace CEMSIM
{
    public class ORMember : MonoBehaviour
    {
        [Header("Tool Ownership")]
        public GameObject leftTool;
        public GameObject rightTool;

        [Header("References")]
        public XRManager xRManager; //TO DO: Make XRManager a singleton (AR 4/7/21)
    
        /// <summary>
        /// Equips a given game object in the OR Member's hand based on the selected interactable.
        /// </summary>
        /// <param name="right"></param>
        /// <param name="tool"></param>
        public void EquipTool(bool right)
        {          
            if (right)
            {
                //TO DO: Update to work with SteamVR and XR Manager (AR 4/7/21)
                XRDirectInteractor controller = xRManager.RightHandController.GetComponent<XRDirectInteractor>();
                Debug.Log(controller.selectTarget.gameObject);
                rightTool = controller.selectTarget.gameObject;
            }
            else
            {
                //TO DO: Update to work with SteamVR and XR Manager (AR 4/7/21)
                XRDirectInteractor controller = xRManager.LeftHandController.GetComponent<XRDirectInteractor>();
                leftTool = controller.selectTarget.gameObject;
            }
        }

        /// <summary>
        /// Unequips the tool being held by the OR Member in a given hand.
        /// </summary>
        /// <param name="right">True if targetting the right hand.</param>
        public void UnequipTool(bool right)
        {
            if(right)
            {
                rightTool = null;
            }
            else
            {
                leftTool = null;
            }
        }
    }
}
