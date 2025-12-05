
Shader "Warp Effect/StarWarpShader" {
	Properties{
		[HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
		[HideInInspector] _MixColor("Mix Color", Color) = (0,0,0,0)
		[HideInInspector] [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0)

		[HideInInspector] _MainTex1("Warp Texture 1", 2D) = "black" {}
		[HideInInspector] _MainTex2("Warp Texture 2", 2D) = "black" {}
		[HideInInspector] _MainTex3("Warp Texture 3", 2D) = "black" {}
		[HideInInspector] _NoiseTex("Noise Texture", 2D) = "black" {}
		[HideInInspector] _EmissionMap_01("Emission Map 1", 2D) = "black" {}
		[HideInInspector] _EmissionMap_02("Emission Map 2", 2D) = "black" {}
		[HideInInspector] [Toggle] _MultiplyEmissionMaps("Multiply Emission Maps", Float) = 0
		[HideInInspector] [Toggle] _EmissionMapDistortion("Emission Map Distortion", Float) = 0

		[HideInInspector] _EmissionMapDistortionSpeedY("Emission Map Distortion Speed", float) = 3.0
		[HideInInspector] _EmissionMapDistortionScale("Emission Map Distortion Scale", Float) = 0.2

		[HideInInspector] _DistortionBumpMap ("Distortion Normalmap", 2D) = "bump" {}
		[HideInInspector] _BackgroundDistortionStrength ("Distortion Strength", Range (0, 100)) = 25
		[HideInInspector] _BackgroundDistortionFrequence ("Background Distortion Frequence", Range (0.01, 0.5)) = 0.05

		[HideInInspector] _SpeedX("SpeedX", float) = 3.0
		[HideInInspector] _SpeedY("SpeedY", float) = 3.0
		[HideInInspector] _Scale("Scale", Range(0.0001, 0.01)) = 0.005
		[HideInInspector] _TileX("TileX", Range(1, 100)) = 50
		[HideInInspector] _TileY("TileY", Range(1, 100)) = 50
		[HideInInspector] _WarpSpeed("Warp Speed", Range(1, 25)) = 8
        
		[HideInInspector] _MainTexRotationSpeed1("Main Texture Rotation Speed 1", Range(-25, 25)) = 0
		[HideInInspector] _MainTexRotationSpeed2("Main Texture Rotation Speed 2", Range(-25, 25)) = 0
		[HideInInspector] _MainTexRotationSpeed3("Main Texture Rotation Speed 3", Range(-25, 25)) = 0

		[HideInInspector] _EmissionMapRotationSpeed_01("Emission Texture Rotation Speed 1", Range(-25, 25)) = 0
		[HideInInspector] _EmissionMapRotationSpeed_02("Emission Texture Rotation Speed 2", Range(-25, 25)) = 0

        [HideInInspector] _StarTexLayer1("Star Texture Layer 1", 2D) = "black" {}
        [HideInInspector] _StarTexLayer2("Star Texture Layer 2", 2D) = "black" {}
        [HideInInspector] _StarTexLayer1Brightness("Brightness Star Layer 1", Range(0, 50)) = 10
        [HideInInspector] _StarTexLayer2Brightness("Brightness Star Layer 2", Range(0, 50)) = 10

        [HideInInspector] _StarsStartPosLayer1("Stars Layer 1 Start Position", Range(0, 0.5)) = 0
        [HideInInspector] _StarsEndPosLayer1("Stars Layer 1 End Position", Range(0, 0.5)) = 1
        [HideInInspector] _StarsStartPosLayer2("Stars Layer 2 Start Position", Range(0, 0.5)) = 0
        [HideInInspector] _StarsEndPosLayer2("Stars Layer 2 End Position", Range(0, 0.5)) = 1

		[HideInInspector] _StarColorLayer1("Star Color Layer 1", Color) = (1,1,1,1)
		[HideInInspector] _StarSpeedFactorLayer1("Stars Speed Factor Layer 1", Range(0.01, 1.0)) = 0.25
		[HideInInspector] _StarLayerRotationSpeed1("Stars Rotation Speed Layer 1", Range(-10.0, 10.0)) = 0.25
		[HideInInspector] [Toggle] _RenderStarLayer1("Render Star Layer 1", Float) = 0

		[HideInInspector] _StarColorLayer2("Star Color Layer 2", Color) = (1,1,1,1)
		[HideInInspector] _StarSpeedFactorLayer2("Stars Speed Factor Layer 2", Range(0.01, 1.0)) = 0.25
		[HideInInspector] _StarLayerRotationSpeed2("Stars Rotation Speed Layer 2", Range(-10.0, 10.0)) = 0.25
		[HideInInspector] [Toggle] _RenderStarLayer2("Render Star Layer 2", Float) = 0
	}
	
	SubShader{
		Tags{
			"Queue" = "Transparent+2"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}
		
		LOD 200
		Cull Front
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off

		Pass {
			ZWrite On
			ColorMask 0
		}

		GrabPass {
			"_WarpGrabTexture"
		}

        // Background distortion pass
		Pass {
			Name "DistortionPass"

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 4.0
				#pragma fragmentoption ARB_precision_hint_fastest

				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float4 uvgrab : TEXCOORD0;
					half2 screenuv : TEXCOORD1;
					float2 uvbump : TEXCOORD2;
					float4 worldPos : TEXCOORD3;
				};

				sampler2D _DistortionBumpMap;
				float4 _DistortionBumpMap_ST;
				half _BackgroundDistortionStrength;
				half _BackgroundDistortionFrequence;

				sampler2D _WarpGrabTexture;
				half4 _WarpGrabTexture_TexelSize;

				half _WarpSpeed;

				v2f vert (appdata v) {
					v2f o = (v2f)0;

					o.worldPos = mul(unity_ObjectToWorld, v.vertex);
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uvgrab = ComputeGrabScreenPos(o.vertex);
					o.uvbump = TRANSFORM_TEX(v.vertex, _DistortionBumpMap);

					o.uvbump.y += _Time.g * (_BackgroundDistortionFrequence) + cross(float3(o.uvbump.xy, 0), float3(o.uvbump.xy, 0));
					o.uvbump.x += _Time.g * (_BackgroundDistortionFrequence) + cross(float3(o.uvbump.xy, 0), float3(o.uvbump.xy, 0));

					return o;
				}

				fixed4 frag (v2f i) : COLOR {
					half xPos = i.uvgrab.x;
					half yPos = i.uvgrab.y;
					half2 uv = i.screenuv;

					half2 bump = UnpackNormal(tex2D(_DistortionBumpMap, i.uvbump)).rg;
					half2 offset = bump * _BackgroundDistortionStrength * _WarpGrabTexture_TexelSize.xy;

					i.uvgrab.xy = offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(i.uvgrab.z) + i.uvgrab.xy;
					half4 col = tex2Dproj(_WarpGrabTexture, UNITY_PROJ_COORD(i.uvgrab));
					col.a = 1;

					return col;
				}

			ENDCG
		}

        // Stars movement pass
        Pass {
            Name "WarpStarPass"
            LOD 200
            Cull Front
            Blend One One
            ZWrite Off
            Lighting Off

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 4.0

                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 uv_1 : TEXCOORD0;
                    float2 uv_2 : TEXCOORD1;
                    float2 screenPos : TEXCOORD3;

                };

                sampler2D _StarTexLayer1;
                float4 _StarTexLayer1_ST;
                sampler2D _StarTexLayer2;
                float4 _StarTexLayer2_ST;

                half _StarsStartPosLayer1;
                half _StarsEndPosLayer1;
                half _StarsStartPosLayer2;
                half _StarsEndPosLayer2;

                fixed4 _StarColorLayer1;
                fixed4 _StarColorLayer2;

                half _WarpSpeed;

                half _StarSpeedFactorLayer1;
                half _StarSpeedFactorLayer2;

                half _StarTexLayer1Brightness;
                half _StarTexLayer2Brightness;

                float4 col_01;
                float4 col_02;

				half _StarLayerRotationSpeed1;
				half _StarLayerRotationSpeed2;

                float _RenderStarLayer1;
                float _RenderStarLayer2;

                v2f vert (appdata v) {
                    v2f o = (v2f)0;
                    o.screenPos = ComputeScreenPos(o.vertex);
                    o.uv_1 = TRANSFORM_TEX(v.uv, _StarTexLayer1);
                    o.uv_2 = TRANSFORM_TEX(v.uv, _StarTexLayer2);
                    o.vertex = UnityObjectToClipPos (v.vertex);
                    o.screenPos = v.uv;

                    return o;
                }

                fixed4 frag (v2f i) : COLOR {
                    float4 col = float4(0, 0, 0, 0);

                    // Render stars layer 1
                    if (_RenderStarLayer1 > 0) {
                        // Move star textures
                        i.uv_1.x += _Time.x * _WarpSpeed * _StarLayerRotationSpeed1;
                        i.uv_1.y += _Time.x * _WarpSpeed * _StarSpeedFactorLayer1;

                        _StarsStartPosLayer1 += 0.5;
                        if (i.screenPos.y > _StarsEndPosLayer1 && i.screenPos.y < 0.5) {
                            col_01 = tex2D(_StarTexLayer1, i.uv_1) * _StarColorLayer1 * _StarTexLayer1Brightness;

                        } else if (i.screenPos.y < _StarsStartPosLayer1 && i.screenPos.y >= 0.5) {
                            col_01 = tex2D(_StarTexLayer1, i.uv_1) * _StarColorLayer1 * _StarTexLayer1Brightness;
                        }
                    }

                    // Render stars layer 2
                    if (_RenderStarLayer2 > 0) {
                        // Move star textures
                        i.uv_2.x -= _Time.x * _WarpSpeed * _StarLayerRotationSpeed2;
                        i.uv_2.y += _Time.x * _WarpSpeed * _StarSpeedFactorLayer2;
                        
                        _StarsStartPosLayer2 += 0.5;
                        if (i.screenPos.y > _StarsEndPosLayer2 && i.screenPos.y < 0.5) {
                            col_02 = tex2D(_StarTexLayer2, i.uv_2) * _StarColorLayer2 * _StarTexLayer2Brightness;

                        } else if (i.screenPos.y < _StarsStartPosLayer2 && i.screenPos.y >= 0.5) {
                            col_02 = tex2D(_StarTexLayer2, i.uv_2) * _StarColorLayer2 * _StarTexLayer2Brightness;
                        }
                    }

                    col = col_01 + col_02;
					col.a = 1.0;

                    return col;
                }

            ENDCG
        }


		CGPROGRAM
			#pragma surface surf NoLighting alpha
			#pragma target 4.0
			#pragma multi_compile _ UNITY_COLORSPACE_GAMMA

			sampler2D _MainTex1;
			sampler2D _MainTex2;
			sampler2D _MainTex3;
			sampler2D _NoiseTex;
			sampler2D _EmissionMap_01;
			sampler2D _EmissionMap_02;

			float4 uv_MainTex1_ST;
			float4 uv_MainTex2_ST;
			float4 uv_MainTex3_ST;
			float4 uv_NoiseTex_ST;
			float4 uv_EmissionMap_01_ST;
			float4 uv_EmissionMap_02_ST;

			struct Input {
				float2 uv_MainTex1;
				float2 uv_MainTex2;
				float2 uv_MainTex3;
				float2 uv_NoiseTex;
				float2 uv_EmissionMap_01;
				float2 uv_EmissionMap_02;
				float3 worldPos;
			};

			half4 _Color;
			half4 _MixColor;
			half4 _EmissionColor;

			half _MainTexRotationSpeed1;
			half _MainTexRotationSpeed2;
			half _MainTexRotationSpeed3;
			half _EmissionMapRotationSpeed_01;
			half _EmissionMapRotationSpeed_02;
			half _MultiplyEmissionMaps;
			half _EmissionMapDistortion;
			half _EmissionMapDistortionSpeedY;

			half _SpeedX;
			half _SpeedY;
			half _Scale;
			half _TileX;
			half _TileY;
			half _WarpSpeed;
			half _StarSpeedFactorLayer1;
			half _StarSpeedFactorLayer2;
			half4 _StarColorLayer1;
			half4 _StarColorLayer2;
			half _EmissionMapDistortionScale;
			half _EmissionMapDistortionTexStrength;
			//float dist;

			// #pragma instancing_options assumeuniformscaling
			UNITY_INSTANCING_BUFFER_START(Props)
			// put more per-instance properties here
			UNITY_INSTANCING_BUFFER_END(Props)
			
			// Ignore lighting from rxternal light sources and return albedo color
			fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) {
				fixed4 c;
				c.rgb = s.Albedo;
				c.a = s.Alpha;

				return c;
			}

			void surf(Input IN, inout SurfaceOutput o) {
				const float PI = 3.14159265;

                // Grab texture uvs
				float2 uv1 = IN.uv_MainTex1;
				float2 uv2 = IN.uv_MainTex2;
				float2 uv3 = IN.uv_MainTex3;
				float2 noise_uv = IN.uv_NoiseTex;
				float2 emission_uv_01 = IN.uv_EmissionMap_01;
				float2 emission_uv_02 = IN.uv_EmissionMap_02;

                // Grab texture colors
				half4 t1 = tex2D(_MainTex1, uv1);
				half4 t2 = tex2D(_MainTex2, uv2);
				half4 t3 = tex2D(_MainTex3, uv3);
				half4 noise_tex = tex2D(_NoiseTex, noise_uv);

				half dist = distance(IN.worldPos, _WorldSpaceCameraPos);

				// Texture 1
				uv1.x += sin((uv1.x + uv1.y) * _TileX + _Time.g * _SpeedX) * _Scale;
				uv1.x += cos(uv1.y*_TileY + _Time.g * _SpeedY) * _Scale;

				uv1.y -= sin((uv1.x + uv1.y) * _TileX + _Time.g * _SpeedX) * _Scale;
				uv1.y -= cos(uv1.y * _TileY + _Time.g * _SpeedY) * lerp(t2.r, noise_tex.r, 0.025) * _Scale / _Time;
				uv1.y += _WarpSpeed * _Time.x * 1.25f;

				// Texture 1 Rotation
				uv1.x += _MainTexRotationSpeed1 * _Time.x;

				t1 = tex2D(_MainTex1, uv1);

				// Texture 2
				uv2.x += sin((uv2.x + uv2.y) * _TileX + _Time.g * _SpeedX) * _Scale;
				uv2.x += cos(uv2.y * _TileY + _Time.g * _SpeedY) * _Scale;

				uv2.y += sin((uv2.x + uv2.y) * _TileX + _Time.g * _SpeedX) * _Scale;
				uv2.y += cos(uv2.y * _TileY + _Time.g * _SpeedY) * cos(uv2.y * _TileY + _Time.g * _SpeedY) * _Scale;
				uv2.y += _WarpSpeed * _Time.x * 1.05f;

				// Texture 2 Rotation
				uv2.x += _MainTexRotationSpeed2 * _Time.x;

				t2 = tex2D(_MainTex2, uv2);

				// Texture 3
				uv3.x += sin((uv3.x + uv3.y) * _TileX + _Time.g *_SpeedX) * _Scale;
				uv3.x += cos(uv3.y * _TileY + _Time.g * _SpeedY) * _Scale;

				uv3.y += sin((uv3.x + uv3.y) * _TileX + _Time.g * _SpeedX) * _Scale;
				uv3.y += cos(uv3.y * _TileY + _Time.g * _SpeedY) * cos(uv3.y * _TileY + _Time.g * _SpeedY) * _Scale;

				uv3.x = lerp(uv3.x, t2.r, t1.g/5) + _Time.x;
				uv3.y += _WarpSpeed * _Time.x * 0.75f;

				// Texture 3 Rotation
				uv3.x += _MainTexRotationSpeed3 * _Time.x;

				t3 = tex2D(_MainTex3, uv3);

				// Emission map distortion
				emission_uv_01.x += _Time.x * _WarpSpeed / 10.0;
				emission_uv_01.x += (_EmissionMapDistortion == 1) ? (sin(IN.worldPos.x + _Time * _EmissionMapDistortionSpeedY * _WarpSpeed) * cos(IN.worldPos.x + _Time * _EmissionMapDistortionSpeedY * _WarpSpeed)) * _EmissionMapDistortionScale : 0.0;
				emission_uv_01.y += _Time.x * _WarpSpeed;

				// Emission Map 1 Rotation
				emission_uv_01.x += _EmissionMapRotationSpeed_01 * _Time.x;

				emission_uv_02.x -= _Time.x * _WarpSpeed / 5.0;
				emission_uv_02.x += (_EmissionMapDistortion == 1) ? (sin(IN.worldPos.x + _Time * _EmissionMapDistortionSpeedY * _WarpSpeed) * cos(IN.worldPos.x + _Time * _EmissionMapDistortionSpeedY * _WarpSpeed)) * _EmissionMapDistortionScale : 0.0;
				emission_uv_02.y += _Time.x * _WarpSpeed;

				// Emission Map 2 Rotation
				emission_uv_02.x += _EmissionMapRotationSpeed_02 * _Time.x;

				half4 emission_tex_01 = tex2D(_EmissionMap_01, emission_uv_01);
				half4 emission_tex_02 = tex2D(_EmissionMap_02, emission_uv_02);
				half4 emission = (_MultiplyEmissionMaps == 0) ? (emission_tex_01 + emission_tex_02) / 2.0 : (emission_tex_01 * emission_tex_02) / 2.0;
				emission *= _EmissionColor;

                // Combine all textures and colors
				half3 col = (((t1.rgb + t2.rgb + t3.rgb) / 3.0) * _Color + _MixColor.rgb) * dist;

				// Linear color space correction
				#ifndef UNITY_COLORSPACE_GAMMA
					col = pow(col, 2.0);
					emission = pow(emission, 2.0);
				#endif

				o.Albedo = col * emission;

				o.Alpha = _Color.a;
			}
		ENDCG
	}

	FallBack "Diffuse"
}
