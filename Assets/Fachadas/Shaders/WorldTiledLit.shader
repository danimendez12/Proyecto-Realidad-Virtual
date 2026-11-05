// World-space tiled lit shader for Revit meshes whose UVs are not in meters.
// Samples albedo and normals on the dominant world axis so siding, rock,
// pavers, and roofs keep a real-world size on every wall and floor.
// Ambient follows the hand-menu day/night factor so facades go dark at night.
Shader "ProyectoVR/WorldTiledLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0, 2)) = 1
        _TileMeters("Meters Per Tile", Float) = 1
        _Smoothness("Smoothness", Range(0,1)) = 0.2
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _TileMeters;
                half _Smoothness;
                half _Cutoff;
            CBUFFER_END

            // Set by DayNightController. 0 = day, 1 = night.
            float _ProyectoVRNight;
            half4 _ProyectoVRNightSky;
            half4 _ProyectoVRNightGround;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 WorldUV(float3 positionWS, float3 normalWS, half tile, out float3 tangentWS, out float3 bitangentWS)
            {
                float3 n = normalize(normalWS);
                float3 an = abs(n);
                float3 axisU;
                float3 axisV;
                float2 uv;
                if (an.y >= an.x && an.y >= an.z)
                {
                    uv = positionWS.xz;
                    axisU = float3(1, 0, 0);
                    axisV = float3(0, 0, 1);
                }
                else if (an.x >= an.z)
                {
                    uv = float2(positionWS.z, positionWS.y);
                    axisU = float3(0, 0, 1);
                    axisV = float3(0, 1, 0);
                }
                else
                {
                    uv = float2(positionWS.x, positionWS.y);
                    axisU = float3(1, 0, 0);
                    axisV = float3(0, 1, 0);
                }

                tangentWS = normalize(axisU - n * dot(axisU, n));
                bitangentWS = normalize(axisV - n * dot(axisV, n) - tangentWS * dot(axisV, tangentWS));
                return uv / max(tile, 0.001h);
            }

            float3 ApplyNormal(float2 uv, float3 normalWS, float3 tangentWS, float3 bitangentWS)
            {
                half3 packed = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv).xyz;
                half3 tangentNormal = UnpackNormalScale(half4(packed, 1), _BumpScale);
                return normalize(tangentWS * tangentNormal.x + bitangentWS * tangentNormal.y + normalWS * tangentNormal.z);
            }

            half3 AmbientTerm(float3 normalWS)
            {
                half up = normalWS.y * 0.5h + 0.5h;
                half3 sh = SampleSH(normalWS);
                half3 hemi = lerp(half3(0.16h, 0.14h, 0.12h), half3(0.46h, 0.52h, 0.58h), up);
                half shAmount = saturate(dot(sh, half3(0.3h, 0.59h, 0.11h)) * 3.0h);
                half3 dayAmbient = lerp(hemi, sh, shAmount);

                half3 nightSky = _ProyectoVRNightSky.rgb;
                half3 nightGround = _ProyectoVRNightGround.rgb;
                half authored = saturate(dot(nightSky + nightGround, half3(1, 1, 1)) * 20.0h);
                half3 nightFallback = lerp(half3(0.008h, 0.008h, 0.012h), half3(0.03h, 0.045h, 0.09h), up);
                half3 nightAmbient = lerp(nightFallback, lerp(nightGround, nightSky, up), authored);
                return lerp(dayAmbient, nightAmbient, saturate(_ProyectoVRNight));
            }

            half3 ShadeLight(half3 albedo, float3 normalWS, Light light, float3 viewWS)
            {
                half ndotl = saturate(dot(normalWS, light.direction));
                half atten = light.shadowAttenuation * light.distanceAttenuation;
                half3 diffuse = albedo * light.color * ndotl * atten;
                half spec = pow(saturate(dot(reflect(-light.direction, normalWS), viewWS)), 24) * _Smoothness * ndotl;
                return diffuse + spec * light.color * atten * 0.25h;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 geometricNormal = normalize(input.normalWS);
                float3 tangentWS;
                float3 bitangentWS;
                float2 uv = WorldUV(input.positionWS, geometricNormal, _TileMeters, tangentWS, bitangentWS);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                clip(albedo.a - _Cutoff);

                float3 normalWS = ApplyNormal(uv, geometricNormal, tangentWS, bitangentWS);
                float3 viewWS = normalize(_WorldSpaceCameraPos - input.positionWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 color = albedo.rgb * AmbientTerm(normalWS);
                color += ShadeLight(albedo.rgb, normalWS, mainLight, viewWS);

                #ifdef _ADDITIONAL_LIGHTS
                uint additionalCount = GetAdditionalLightsCount();
                half4 shadowMask = half4(1, 1, 1, 1);
                for (uint lightIndex = 0u; lightIndex < additionalCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    color += ShadeLight(albedo.rgb, normalWS, light, viewWS);
                }
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 ShadowClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return positionCS;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = ShadowClip(input);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
