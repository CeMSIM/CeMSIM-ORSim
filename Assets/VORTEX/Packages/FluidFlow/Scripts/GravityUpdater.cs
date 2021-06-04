using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    [RequireComponent(typeof(FluidSimulator))]
    public class GravityUpdater : MonoBehaviour
    {
        [Tooltip("Update gravitymaps when local gravity direction changed by x degrees")]
        public float DeadzoneAngle = 10;
        private Vector3 lastGrav;
        private FluidSimulator simulator;

        private Transform gravityRoot;

        private void Start()
        {
            simulator = GetComponent<FluidSimulator>();
            if (simulator.isSkinned())
                gravityRoot = ((SkinnedMeshRenderer)simulator.fluidRenderer).rootBone;
            else
                gravityRoot = simulator.fluidRenderer.transform;

            lastGrav = calculateLocalGravity(Physics.gravity);
        }

        private Vector3 calculateLocalGravity(Vector3 gravity)
        {
            if (simulator.isSkinned())
                return simulator.fluidObject.TransformDirectionToDefaultPose(gravityRoot, simulator.getBoneId(gravityRoot), gravity);
            else
                return gravityRoot.InverseTransformDirection(gravity);
        }

        void Update()
        {
            Vector3 gravity = calculateLocalGravity(Physics.gravity);

            if (Vector3.Angle(lastGrav, gravity) > DeadzoneAngle)
            {
                simulator.UpdateGravity(gravity, Space.Self);
                lastGrav = gravity;
            }
        }
    }
}
