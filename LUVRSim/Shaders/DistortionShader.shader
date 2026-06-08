Shader "Custom/DistortionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SectorAngle ("Sector Angle (Degrees)", Float) = 90
        _ProbeRadius ("Probe Radius", Float) = 0.05
        _IsLinearProbe ("Linear Probe Check", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _SectorAngle;
            float _ProbeRadius;
            float _IsLinearProbe;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                // UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
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
                float2 fanUV = Fan(i.uv);
                
                if (fanUV.x < 0.0 || fanUV.x > 1.0 || fanUV.y < 0.0 || fanUV.y > 1.0)
                    return fixed4(0,0,0,1); 

                if (_IsLinearProbe == 0)
                    return tex2D(_MainTex, fanUV);

                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
