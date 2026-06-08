Shader "Custom/DifferenceShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                float2 toSource : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uvThisRow = v.uv.xy;
                o.uvAbove = float2(v.uv.x, v.uv.y + _MainTex_ST.y);
                o.toSource = float2(0.5, 1) - v.uv.xy; // Fanning from the top center point of the texture
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                
                half4 thisRow = tex2D(_MainTex, i.uvThisRow);
                half4 above = tex2D(_MainTex, i.uvAbove);

                // I need to take into account bone and air here most likely, using different values thresholded should be enough
                // can try using pow(thisRow-above/thisRow+above, 2);
                // if everything works considering these 2 things then I'm gucci
                half4 acousticImp = pow(thisRow-above/max(thisRow+above, 0.15), 2)*4;
                // (abs(thisRow - above) ); This was an approximation. Can be used to be experimented with

                half4 differenceOutput = saturate(acousticImp);

                half4 intensity = fixed4(0, 0, 0, 1);
                float2 normalizedToSource = i.toSource / length(i.toSource);
                float2 texelToSource = float2(normalizedToSource * _MainTex_ST.y);

                // _TexelSize.w is automatically assigned by Unity to the texture’s height
                for (int step = 0; step < _MainTex_ST.w; step++) {
                    intensity += tex2D(_MainTex, i.uvThisRow + texelToSource * step);
                }

                half4 intensityOutput = 1-intensity;

                half4 baseColor = tex2D(_MainTex, i.uvThisRow);

                // Here I should have:
                return baseColor*intensityOutput + differenceOutput;
            }
            ENDCG
        }
    }
}
