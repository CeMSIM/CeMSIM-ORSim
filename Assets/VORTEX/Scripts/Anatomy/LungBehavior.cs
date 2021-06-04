using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LungBehavior : MonoBehaviour
{
    public Material defaultMaterial;
    public Material enterMaterial;

    public bool inLung; //TO DO: Only handles the insertion of one tool, need to update for multiple tool tracking
    public bool syringeInLung;

    private PatientManager patient;
    private MeshRenderer meshRenderer;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = this.GetComponent<MeshRenderer>();
        patient = PatientManager.Instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Tool")
        {
            inLung = true;
            meshRenderer.material = enterMaterial;
            other.GetComponent<NeedleBehavior>().NeedleInserted(true);
            Debug.Log("Needle decompression event");
            PatientEvents.Instance.TriggerNeedleDecompression();           
        }

        if(other.tag == "Syringe")
        {
            syringeInLung = true;
            meshRenderer.material = enterMaterial;
            other.GetComponent<NeedleBehavior>().NeedleInserted(true);
            string med = other.GetComponent<Medication>().medication.ToString();
            Debug.Log(med + " administered");
            patient.pulseEventManager.AdministerMedication(med);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Tool")
        {
            inLung = false;
            meshRenderer.material = defaultMaterial;
            other.GetComponent<NeedleBehavior>().NeedleInserted(false);
        }
    }
}
