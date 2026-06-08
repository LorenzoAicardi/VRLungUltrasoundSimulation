Shader "Custom/SliceVolume"
{
    Properties
    {
        _MainTex("Texture", 3D) = "" {}
        _BreathFrequency ("Breath Frequency", Float) = 1.0
        _BreathAmplitude ("Breath Amplitude", Float) = 0.025

    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        LOD 100
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_VERTEX_OUTPUT_STEREO
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 relVert : TEXCOORD1;
            };

            sampler3D _MainTex;
            // Parent's inverse transform (used to convert from world space to volume space)
            uniform float4x4 _ParentInverseMat;
            // Plane transform
            uniform float4x4 _PlaneMat;
            float _BreathFrequency;
            float _BreathAmplitude;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Modified UV (because slicing plane has coodinates between 0 and 40, while the volume has 0-1)
                float2 uvMod = float2((0.5f - v.uv.x), (0.5f - v.uv.y)) * 4.f;
                // Calculate plane vertex world position.
                float3 vert = mul(_PlaneMat, float4(uvMod.x, 0.0f, uvMod.y, 1.f));
                // Convert from world space to volume space.
                o.relVert = mul(_ParentInverseMat, float4(vert, 1.0f)); //0.20f controlls w in to the volume; o.relVert = mul(_parentInverseMat, float4(vert * 0.30f, 1.0f * 0.20f))
                o.uv = v.uv;
                return o;
            }

            float3 CoordinateDistorsion(float amplitude){
                return float3(0, sin(_Time.y * _BreathFrequency), 0) * amplitude;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dataCoord = i.relVert + float3(0.5f, 0.5f, 0.5f);
                float4 black = float4(0.0f, 0.0f, 0.0f, 1.0f);

                // If the current fragment is outside the volume, simply colour it black
                if (dataCoord.x > 1.0f || dataCoord.y > 1.0f || dataCoord.z > 1.0f || dataCoord.x < 0.0f || dataCoord.y < 0.0f || dataCoord.z < 0.0f)
                {
                    return black;
                }
                else
                {   
                    float amplitudeAmount = (1.f - dataCoord.z * 1.5f) * _BreathAmplitude;
                    // float3 dataCoordOffset = CoordinateDistorsion(amplitudeAmount);
                    // float3 dataCoordDistorted = dataCoord + dataCoordOffset;

                    // float4 col = tex3D(_MainTex, dataCoordDistorted;
                    float4 col = tex3D(_MainTex, dataCoord);
                    col.a = 1.0f;

                    return col;

                }
            }
            ENDCG
        }
    }
}