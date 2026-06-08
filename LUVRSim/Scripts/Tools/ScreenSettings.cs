using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ScreenSettings : NetworkBehaviour
{
    private static readonly int Brightness = Shader.PropertyToID("_Brightness");
    [SerializeField] private GameObject _probe;
    [SerializeField] private Material _screenMat;
    private float minDepthX;
    private float minDepthZ;
    private float maxDepthX;
    private float maxDepthZ;
    
    public void DepthSetting(Slider depthSlider)
    {
        float newXScale = Mathf.Lerp(minDepthX, maxDepthX, depthSlider.value);
        float newZScale = Mathf.Lerp(minDepthZ, maxDepthZ, depthSlider.value);

        var slicingPlane = _probe.GetComponent<Transform>().GetChild(0);
        slicingPlane.localScale = new Vector3(newXScale, transform.localScale.y, newZScale);
    }
    
    public void GainSetting(Slider gainSlider)
    {
        _screenMat.SetFloat(Brightness, gainSlider.value);
    }

    public void SetProbe(GameObject probe)
    {
        // not necessary if all probes have the same plane
        minDepthX = probe.GetComponent<Transform>().GetChild(0).localPosition.x;
        minDepthZ = probe.GetComponent<Transform>().GetChild(0).localPosition.z;
        maxDepthX = minDepthX + 0.026f;
        maxDepthZ = minDepthZ + 0.010f;
        
        _probe = probe;
    }
}
