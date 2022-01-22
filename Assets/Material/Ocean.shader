Shader "Custom/Ocean"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

        [HideInInspector]_Displacement("Displacement", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma multi_compile _ MID CLOSE
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 4.0

        struct Input
        {
            float2 worldUV;
            float3 viewVector;
            float3 worldNormal;
            float4 screenPos;
            INTERNAL_DATA
        };

        sampler2D _Displacement;
        sampler2D _Normals;

        float _dispStrength0;
        float _dispStrength1;
        float _lengthScale0;
        float _lengthScale1;

        float _roughness;
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex);
            float4 worldUV = float4(worldPos.xz, 0, 0);
            o.worldUV = worldUV.xy;

            o.viewVector = _WorldSpaceCameraPos.xyz - mul(unity_ObjectToWorld, v.vertex).xyz;
            float viewDist = length(o.viewVector);

            float3 displacement = 0;
            displacement += tex2Dlod(_Displacement, worldUV / _lengthScale0) * _dispStrength0 * 0.01;
            displacement += tex2Dlod(_Displacement, worldUV / _lengthScale1) * _dispStrength1 * 0.01;

            v.vertex.xyz += mul(unity_WorldToObject, displacement);
        }


        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float3 WorldToTangentNormalVector(Input IN, float3 normal) {
            float3 t2w0 = WorldNormalVector(IN, float3(1, 0, 0));
            float3 t2w1 = WorldNormalVector(IN, float3(0, 1, 0));
            float3 t2w2 = WorldNormalVector(IN, float3(0, 0, 1));
            float3x3 t2w = float3x3(t2w0, t2w1, t2w2);
            return normalize(mul(t2w, normal));
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 normals = tex2D(_Normals, IN.worldUV / _lengthScale0).rgb;
            normals += tex2D(_Normals, IN.worldUV / _lengthScale1).rgb;
            
            o.Normal = normalize(normals);

            o.Albedo = _Color.rgb;

            o.Metallic = 0.0;
            o.Smoothness = 1.0 - _roughness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
