Shader "Custom/PSx_Lit"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _SnapStrength ("Vertex Snapping", Float) = 50.0 // Higher = Less Jitter
        _Emission ("Emission", Range(0,1)) = 0.0
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
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _SnapStrength;
            float _Emission;

            v2f vert (appdata v)
            {
                v2f o;
                
                // 1. Convert to Clip Space
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                
                // 2. Vertex Snapping (The "Jitter" Magic)
                // We divide by W to get normalized coordinates, snap them, then multiply back.
                float4 snapPos = clipPos;
                snapPos.xyz = clipPos.xyz / clipPos.w; // Perspective division
                snapPos.x = floor(snapPos.x * _SnapStrength) / _SnapStrength;
                snapPos.y = floor(snapPos.y * _SnapStrength) / _SnapStrength;
                clipPos.xyz = snapPos.xyz * clipPos.w;
                
                o.vertex = clipPos;

                // 3. Affine Texture Mapping emulation
                // By not correcting UVs for perspective, we get that warping effect.
                // (Unity does this automatically, so we just pass standard UVs but the
                // vertex snapping above naturally distorts them).
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                
                // Simple lighting approximation
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(worldNormal, lightDir));
                
                // Bake lighting into vertex color (Gouraud shading feel)
                o.normal = NdotL + _Emission; 

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Apply the simple baked lighting
                col.rgb *= i.normal; 
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}