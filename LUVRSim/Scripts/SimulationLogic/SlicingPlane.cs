using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlicingPlane : MonoBehaviour
{      
    private MeshRenderer meshRendererPlane;
    public GameObject USSection;

    private void Awake() {
        meshRendererPlane = GetComponent<MeshRenderer>();
    }
    
    void Update()
    {
        meshRendererPlane.sharedMaterial.SetMatrix("_ParentInverseMat", USSection.transform.worldToLocalMatrix);
        meshRendererPlane.sharedMaterial.SetMatrix("_PlaneMat", transform.localToWorldMatrix);
    }
}
