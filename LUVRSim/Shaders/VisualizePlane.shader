Shader "Custom/VisualizePlane"
{
    Properties
    {
        _MainTex("Plane Texture", 2D) = "" {}
        _BackgroundTex("Background", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _ShadowMask("Shadow Mask", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1
        _LowBlurAmount ("Blur Amount", Range(0.1, 10)) = 0.1
        _BlurSizeShadow("Blur Size Shadow", Range(0, 10)) = 0.3
        _Brightness ("Brightness", Float) = 0.24
        _BrightnessBackGround ("Brightness Background", Float) = 1.14
        _LowNoiseIntensity("Low Noise Intensity", Range(1.1, 3.0)) = 1.1
        _SectorAngle ("Sector Angle (Degrees)", Float) = 60
        _ProbeRadius ("Probe Radius", Float) = 0.05
        _IsLinearProbe ("Linear Probe Check", Float) = 0
        _ToggleSimulateUltrasound ("Toggle Simulate Ultrasound", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _BackgroundTex;
            sampler2D _MaskTex;
            Texture2D<float> _ShadowMask;
            SamplerState sampler_ShadowMask;
            float _NoiseStrength;
            float _Brightness;
            float _BrightnessBackGround;
            float _LowNoiseIntensity;
            float _LowBlurAmount;
            float _BlurSizeShadow;
            float _SectorAngle;
            float _ProbeRadius;
            float _IsLinearProbe;
            float _ToggleSimulateUltrasound;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float PseudoRandomCoord(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float4 AddBlur(float2 uv, float blurSize, sampler2D tex)
            {
                float4 color = 0;
                blurSize = blurSize * 0.05; // Adjust blur intensity
            
                // Sample pixels in the blur kernel
                [unroll]
                for (float x = -2; x <= 2; x++)
                {
                    [unroll]
                    for (float y = -2; y <= 2; y++)
                    {
                            float4 textureColor = tex2D(tex, uv + float2(x, y) * blurSize) * _BrightnessBackGround;
                            color += textureColor;
                    }
                }

                return color / 25.0; // Divide by number of samples
            }

            float2 Fan(float2 uv)
            {
                // Convert to probe-relative coordinates
                float2 centered = float2(uv.x - 0.5, uv.y);
                
                // Calculate angle and radius
                float angle = centered.x * (_SectorAngle * UNITY_PI/180.0);
                float radius = _ProbeRadius + uv.y * (1.0 - _ProbeRadius);
                
                // Outward fan transformation
                float2 fanUV;
                fanUV.x = radius * sin(angle) + 0.5;
                fanUV.y = radius * cos(angle);
                
                return fanUV;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                
                if (_ToggleSimulateUltrasound == 1)
                {
                    float2 fanUV = _IsLinearProbe == 0 ? Fan(i.uv) : i.uv;
                    float4 black = float4(0.0, 0.0, 0.0, 1.0);

                    // Add blur
                    float blurAmount = ((1.f - fanUV.y) * 1.5f) * _LowBlurAmount; // 2.f attenuation factor
                    float4 blurColor = AddBlur(fanUV.xy, blurAmount, _MainTex);   // the planerendertexture

                    // Add shadows
                    float shadow = _ShadowMask.Sample(sampler_ShadowMask, fanUV);
                    float4 shadowColor = lerp(black, blurColor, shadow);

                    // Add decorations
                    float4 background = tex2D(_BackgroundTex, i.uv.xy); // decorations on border
                    float4 mask = tex2D(_MaskTex, i.uv.xy);             // the cone
                    
                    // Mix the foreground and background textures based on blend mask
                    float4 finalColor = lerp(background, shadowColor, mask); // *_Brightness // Black first texture, white second texture
                    
                    return finalColor;
                }

                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
