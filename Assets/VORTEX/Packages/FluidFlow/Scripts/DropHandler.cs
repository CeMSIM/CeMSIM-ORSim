using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidSimulation
{
    [RequireComponent(typeof(FluidSimulator))]
    public class DropHandler : MonoBehaviour
    {
        public GameObject dropPrefab;
        public float dropLifeTime = 1f;

        private FluidSimulator simulator;

        private void Awake()
        {
            simulator = GetComponent<FluidSimulator>();
            if (simulator)
            {
                // listen for fluid drips
                simulator.OnFluidDrip.AddListener(OnDrip);
            }
        }

        public void OnDrip(Vector3 position, float amount)
        {
            // create fluid drop prefab at drip position
            GameObject bloodInstance = Instantiate(dropPrefab, position, Quaternion.identity);
            FluidDrop drop = bloodInstance.GetComponent<FluidDrop>();

            if (drop)
                drop.SetAmount(amount);

            Destroy(bloodInstance, dropLifeTime);
        }
    }
}
