void raymarch_float(float3 rayOrigin,
    float3 rayDirection, UnityTexture3D volumeTex,
    UnitySamplerState volumeSampler,
    float alpha,
    float stepsize,
    out float4 result)
{
    const int MAX_STEP_COUNT = 128;
    const float EPSILON = 0.00001f;

    float4 color = float4(0, 0, 0, 0);
    float3 samplePosition = rayOrigin;

    for (int i = 0; i < MAX_STEP_COUNT; i++)
    {
        if (max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 0.5f + EPSILON)
        {
            float4 sampledColor = SAMPLE_TEXTURE3D(volumeTex, volumeSampler, samplePosition + float3(0.5f, 0.5f, 0.5f));
            sampledColor.a *= alpha;
            color.rgb += sampledColor.a * sampledColor.rgb;
            color.a += (1.0 - color.a) * sampledColor.a;

            samplePosition += rayDirection * stepsize;
        }
    }

    result = color;
}