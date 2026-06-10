using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = UnityEngine.Random;

public class ProbeUltrasoundLogic : MonoBehaviour
{
    private static readonly int Width = Shader.PropertyToID("_Width");
    private static readonly int Height = Shader.PropertyToID("_Height");
    public Texture2D screenMask;
    private const int size = 128;
    private const int resize = 32;
    private const float whiteScale = 0.38f; // 0.38 for pow, 0.8f for difference
    private const float angleConvex = 10.0f;
    private const float anglePhased = 15.0f;
    private List<Vector2Int> selectedPixels;
    private Vector3 planeLastPosition;
    private float planeLastScaleX;
    [SerializeField] private RenderTexture planeRenderTexture;
    
    public Renderer bigScreenRenderer;

    // Materials used for ultrasound simulation
    private Material _sliceMaterial;
    private Material _differenceMat;
    private Material _artifactMat;
    // private Material _distortionMat;
    
    // RenderTextures on which intermediate steps are saved
    private RenderTexture _firstTex;
    private RenderTexture _combineTex;
    private RenderTexture _artifactTex;
    //private RenderTexture distortionTex;

    // this determines which artifacts will be shown
    private int _artifactCode;

    private bool _deactivateUltrasound;
    
    [SerializeField] ComputeShader pleuralCompute;
    [SerializeField] ComputeShader maxCompute;
    [SerializeField] ComputeShader shadowCompute;
    [SerializeField] ComputeShader noiseCompute;

    int kernel;
    ComputeBuffer groupBuffer;
    
    int kernelMax;
    ComputeBuffer groupBufferMax;

    int kernelShadow;
    RenderTexture renderTextureShadow;
    
    int kernelNoise;
    RenderTexture renderTextureNoise;
    
    private void Awake() {
        _sliceMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        selectedPixels = new List<Vector2Int>();
        
        _differenceMat = new Material(Shader.Find("Custom/DifferenceShader"));
        _artifactMat = new Material(Shader.Find("Custom/ArtifactShader"));
        
        planeLastPosition = transform.position;
        planeLastScaleX = transform.localScale.x;
        
        _firstTex = RenderTexture.GetTemporary(planeRenderTexture.width, planeRenderTexture.height, 0);
        _combineTex = RenderTexture.GetTemporary(planeRenderTexture.width, planeRenderTexture.height, 0);
        _artifactTex = RenderTexture.GetTemporary(planeRenderTexture.width, planeRenderTexture.height, 0);
        
        // Keep these textures persistent
        _firstTex.Create();
        _combineTex.Create();
        _artifactTex.Create();

        _artifactCode = 1; // See A-Lines

        planeRenderTexture.enableRandomWrite = true;

        // Compute shader setup
        kernel = pleuralCompute.FindKernel("CSMain");
        groupBuffer = new ComputeBuffer(64, sizeof(float) * 2); // 8x8 groups = 64
        pleuralCompute.SetInt(Width, 128); // Texture is 128x128.
        pleuralCompute.SetInt(Height, 128);
        pleuralCompute.SetTexture(kernel, "_Input", _combineTex);
        pleuralCompute.SetBuffer(kernel, "_GroupResults", groupBuffer);
        
        kernelMax = maxCompute.FindKernel("Max");
        groupBufferMax = new ComputeBuffer(1, sizeof(float)); // 8x8 groups = 64
        maxCompute.SetBuffer(kernelMax, "_Input", groupBuffer);
        maxCompute.SetBuffer(kernelMax, "_GroupResult", groupBufferMax);
        _artifactMat.SetBuffer("_PleuralLine", groupBufferMax);

        kernelShadow = shadowCompute.FindKernel("Shadows");
        renderTextureShadow = new RenderTexture(128, 128, GraphicsFormat.R32_SFloat, 0);
        renderTextureShadow.enableRandomWrite = true;
        shadowCompute.SetTexture(kernelShadow, "_Input", planeRenderTexture);
        shadowCompute.SetTexture(kernelShadow, "_GroupResult", renderTextureShadow);
        
        kernelNoise = noiseCompute.FindKernel("Noise");
        renderTextureNoise = new RenderTexture(256, 256, GraphicsFormat.R32_SFloat, 0);
        renderTextureNoise.enableRandomWrite = true;
        noiseCompute.SetTexture(kernelNoise, "_GroupResult", renderTextureNoise);
    }
    
    private void Start() {
        
    }

    void Update()
    {
        SimulateUltrasound();
    }

    private void SimulateUltrasound()
    {
        if (_deactivateUltrasound)
        {
            Graphics.Blit(_sliceMaterial.GetTexture("_MainTex"), planeRenderTexture, _sliceMaterial);
            return;
        }
            
        Graphics.Blit(_sliceMaterial.GetTexture("_MainTex"), _firstTex, _sliceMaterial);
        
        // First step: generate reflections + intensities and combine them
        Graphics.Blit(_firstTex, _combineTex, _differenceMat); 
        
        // Finding the pleural line Y coordinate
        // I set the input texture and the compute buffer
        // And I tell the Compute Shader to run 64 times to cover the entire 128x128 texture.
        // The shader runs to cover 8 tiles of size 16x16.
        // For each tile, the maximum of that tile is stored in the compute buffer.
        // Returns in a compute buffer 64 possible values for the pleural line height.
        pleuralCompute.Dispatch(kernel,8, 8, 1);
        
        // This already sends data to the Artifact shader.
        // Computes in the compute shader the max out of 64 possible values for the pleural line height.
        maxCompute.Dispatch(kernelMax, 1, 1, 1);
        
        _artifactMat.SetFloat("_IntensityFactor", Random.Range(0.5f, 1.0f));
        Graphics.Blit(_combineTex, planeRenderTexture, _artifactMat);
        
        // Computes shadow locations, then the result is passed over to the screen renderer.
        shadowCompute.Dispatch(kernelShadow, 1, 1, 1);
        bigScreenRenderer.sharedMaterial.SetTexture("_ShadowMask", renderTextureShadow);
        
        // Creates a 256x256 perlin noise texture, which is then added each frame to the main texture.
        noiseCompute.SetFloat("_Input", Random.Range(0.1f, 0.2f));
        noiseCompute.Dispatch(kernelNoise, 16, 16, 1);
        _artifactMat.SetTexture("_NoiseTex", renderTextureNoise);
        
        switch (transform.parent.gameObject.name)
        {
            case "Convex Probe(Clone)" or "Convex Probe":
                bigScreenRenderer.sharedMaterial.SetFloat("_IsLinearProbe", 0);
                bigScreenRenderer.sharedMaterial.SetFloat("_SectorAngle", 60);
                break;
            case "Linear Probe(Clone)" or "Linear Probe":
                bigScreenRenderer.sharedMaterial.SetFloat("_IsLinearProbe", 1);
                break;
            case "Phased Probe(Clone)" or "Phased Probe":
                bigScreenRenderer.sharedMaterial.SetFloat("_IsLinearProbe", 0);
                bigScreenRenderer.sharedMaterial.SetFloat("_SectorAngle", 120);
                break;
        }
    }
    
    private bool IsPlaneScaled()
    {
        float displacement = transform.localScale.x - planeLastScaleX;
        if(displacement != 0){
            planeLastScaleX = transform.localScale.x;
            return true;
        }

        planeLastScaleX = transform.localScale.x;
        return false;
    }

    private bool IsPlaneMoved()
    {   
        float displacement = Vector3.Magnitude(transform.position - planeLastPosition);
        if(displacement > 0.0001f){
            planeLastPosition = transform.position;
            return true;
        }

        planeLastPosition = transform.position;
        return false;
    }

    public void SetArtifactCode(int artifactCode)
    {
        _artifactCode = artifactCode;
        if (_artifactMat)
        {
            _artifactMat.SetFloat("_ArtifactCode", _artifactCode);
        }
    }

    public int GetArtifactCode()
    {
        return _artifactCode;
    }

    public void DeactivateUltrasound(bool choice)
    {
        _deactivateUltrasound = choice;
        bigScreenRenderer.sharedMaterial.SetFloat("_ToggleSimulateUltrasound", choice ? 0f : 1f);
    }
}
