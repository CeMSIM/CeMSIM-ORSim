using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using FluidSimulation;

public class FluidInteractionDemo : MonoBehaviour
{
    public FluidSimulator[] fluidSimulators;

    public Dropdown brushSelection;

    public RawImage image;
    public Slider sizeSlider;
    public Slider amountSlider;
    public Slider angleSlider;
    public Slider depthSlider;
    public Toggle continuousToggle;
    public Toggle subtractToggle;

    public GameObject angleSliderGO;
    public GameObject depthSliderGO;
    public GameObject textureGO;
    public GameObject computeSupportGO;

    public Transform brushPreview;
    public GameObject texBrushPreview;
    public GameObject sphereBrushPreview;
    public GameObject discBrushPreview;

    public Texture2D[] brushTextures;
    private int brushTexID = 0;

    public Camera cam;

    private int currentSimulator = 0;
    

    private void Start()
    {
        OnChangeBrush(0);
        ChangeBrushTexture(0);
        SwitchSimulators();

        computeSupportGO.SetActive(!SystemInfo.supportsComputeShaders);
    } 

    private void Update()
    {
        bool click = (Input.GetMouseButton(0) && continuousToggle.isOn || Input.GetMouseButtonDown(0) && !continuousToggle.isOn) && !Input.GetKey(KeyCode.LeftControl);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit raycastHit;
        brushPreview.gameObject.SetActive(false);

        if (Physics.Raycast(ray, out raycastHit, 100))
        {
            if (raycastHit.transform.root == getCurrentSimulator().transform.root)
            {
                brushPreview.gameObject.SetActive(true);
                FluidSimulator simulator = getCurrentSimulator();
                float amount = ((subtractToggle.isOn ^ Input.GetKey(KeyCode.LeftShift)) ? -1 : 1) * amountSlider.value;
                if (brushSelection.value == 0)
                {
                    Vector3 up = Quaternion.LookRotation(ray.direction, Vector3.up) * Vector3.up;
                    Vector3 fwd = -raycastHit.normal;
                    Vector3 pos = raycastHit.point + raycastHit.normal * .02f;

                    setBrushPreviewPosition(pos, fwd, sizeSlider.value, .1f);
                    if (click)
                    {

                        if (simulator.isSkinned())
                            simulator.SkinnedDrawTexture(raycastHit.transform, pos, fwd, up, brushTextures[brushTexID], sizeSlider.value, .1f, amount);
                        else
                            simulator.DrawTexture(pos, fwd, up, brushTextures[brushTexID], sizeSlider.value, .1f, amount);
                    }
                }
                else if(brushSelection.value == 1)
                {
                    setBrushPreviewPosition(raycastHit.point, Vector3.forward, sizeSlider.value, sizeSlider.value);
                    if (click)
                    {
                        if (simulator.isSkinned())
                            simulator.SkinnedDrawSphere(raycastHit.transform, raycastHit.point, sizeSlider.value, amount);
                        else
                            simulator.DrawSphere(raycastHit.point, sizeSlider.value, amount);
                    }
                }
                else if(brushSelection.value == 2)
                {
                    Vector3 normal = Quaternion.AngleAxis(angleSlider.value, raycastHit.normal) * Vector3.up;

                    setBrushPreviewPosition(raycastHit.point, normal, sizeSlider.value, depthSlider.value);
                    if (click)
                    {
                        if (simulator.isSkinned())
                            simulator.SkinnedDrawDisc(raycastHit.transform, raycastHit.point, normal, sizeSlider.value, depthSlider.value, amount);
                        else
                            simulator.DrawDisc(raycastHit.point, normal, sizeSlider.value, depthSlider.value, amount);
                    }
                }
            }
        }
    }

    private FluidSimulator getCurrentSimulator()
    {
        return fluidSimulators[currentSimulator];
    }

    public void SwitchSimulators()
    {
        getCurrentSimulator().transform.root.gameObject.SetActive(false);
        currentSimulator = currentSimulator == 0 ? 1 : 0;
        getCurrentSimulator().transform.root.gameObject.SetActive(true);
    }

    public void ResetFluid()
    {
        getCurrentSimulator().ResetFluid();
    }

    public void OnChangeBrush(int brushID)
    {
        textureGO.SetActive(brushID == 0);
        depthSliderGO.SetActive(brushID == 2);
        angleSliderGO.SetActive(brushID == 2);
        UpdateBrushPreview(brushID);
    }

    public void ChangeBrushTexture(int d)
    {
        brushTexID += d;
        if(brushTexID < 0)
        {
            brushTexID = brushTextures.Length - 1;
        }
        if(brushTexID >= brushTextures.Length)
        {
            brushTexID = 0;
        }
        image.texture = brushTextures[brushTexID];
    }
    
    private void UpdateBrushPreview(int brushID)
    {
        texBrushPreview.SetActive(brushID == 0);
        sphereBrushPreview.SetActive(brushID == 1);
        discBrushPreview.SetActive(brushID == 2);
    }

    private void setBrushPreviewPosition(Vector3 position, Vector3 fwd, float size, float depth)
    {
        brushPreview.position = position;
        brushPreview.rotation = Quaternion.LookRotation(fwd);
        brushPreview.localScale = new Vector3(size, size, depth);
    }
}
