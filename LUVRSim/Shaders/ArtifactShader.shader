Shader "Custom/ArtifactShader"
{
    Properties
    {
        _MainTex ("Slice Texture", 2D) = "white" {}
        _PleuralY ("Pleural Y", Float) = -1.0
        _ArtifactCode ("Artifact Code", Float) = 2
        _IntensityFactor ("Intensity Factor", Float) = 0.5
        _NoiseTex ("Texture", 2D) = "white" {}
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
                UNITY_VERTEX_OUTPUT_STEREO
                float4 vertex : SV_POSITION;
                float2 uvThisRow : TEXCOORD0;
                float2 uvAbove : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _PleuralY;
            float _ArtifactCode;
            float _IntensityFactor;
            sampler2D _NoiseTex;
            StructuredBuffer<float> _PleuralLine;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvThisRow = v.uv.xy;
                o.uvAbove = float2(v.uv.x, v.uv.y + _MainTex_ST.y);
                return o;
            }

            float hash(float n)
            {
                return frac(sin(n)*43758.5453);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uvThisRow);

                if (_ArtifactCode == 1)
                {
                    if (_PleuralLine[0] != -1.0) // _PleuralY
                    {
                        // draw A-Lines
                        for (int n = 1; n <= 3; n++) {
                            float spacing = 0.1; // spacing between a-lines
                            float aLine = _PleuralLine[0] - n * spacing; // location of a-line y coord
                             
                            if (abs(i.uvThisRow.y - aLine) < 0.005 && col.r < 0.1 && i.uvThisRow.x > 0.2 && i.uvThisRow.x < 0.8) {
                                col.rgb = float3(0.2, 0.2, 0.2);  // grey line
                            }
                        }   
                    }
                } else if (_ArtifactCode == 2)
                {
                    if (_PleuralLine[0] != -1.0)
                    {
                        // draw B-Lines
                        for (int n = 1; n <= 4; n++) {
                            float spacing = 0.1; // spacing between a-lines
                            float bLine = _PleuralLine[0] - n * spacing; // location of b-line x coord
                             
                            if (abs(i.uvThisRow.x - bLine) < 0.005 && col.r < 0.05 && i.uvThisRow.y > 0.2 && i.uvThisRow.y < 0.8) {
                                col.rgb = float3(0.2, 0.2, 0.2)*_IntensityFactor;  // grey line
                            }
                        }
                    }
                }

                float noise = tex2D(_NoiseTex, i.uvThisRow);
                
                return col + noise*0.5;
            }
            ENDCG
        }
    }
}
