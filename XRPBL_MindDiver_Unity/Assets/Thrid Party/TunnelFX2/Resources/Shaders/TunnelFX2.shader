Shader "TunnelEffect/TunnelFX2" {
	Properties {
		_TunnelTex1 ("Tunnel Tex 1 (RGB)", 2D) = "black" {}
		_TunnelTex2 ("Tunnel Tex 2 (RGB)", 2D) = "black" {}
		_TunnelTex3 ("Tunnel Tex 3 (RGB)", 2D) = "black" {}
		_TunnelTex4 ("Tunnel Tex 4 (RGB)", 2D) = "black" {}
		_Params1 ("Params 1", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params2 ("Params 2", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params3 ("Params 3", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params4 ("Params 4", Vector) = (1.5, 0.5, 0.1, 0.12)
		_Params5 ("Params 5", Vector) = (0.533328, 0.26664, 0.13332, 0.06666)
		_MixParams ("Mix Params", Vector) = (0,0,4,1)
        _CurveParams("Curve Params", Vector) = (0.02, 15.0, 0.01, 1000)
		_TextureTilings ("Texture Tilings", Vector) = (1,1,1,1)
		_BackgroundColor ("Background Color", Color) = (0,0,0,0)
		_Color ("Tint Color (RGB)", Color) = (1,1,1,1)
		_FallOffStart ("Vignette Start", Float) = 0
        _Aperture("Aperture", Float) = 0
	}
   	SubShader {
       Tags {
	       "Queue"="Geometry+100"
	       "RenderType"="Opaque"
       }
       Cull Off

       Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile __ TUNNEL_BLEND_IN_ORDER
		#pragma multi_compile TUNNEL_LAYER_COUNT_1 TUNNEL_LAYER_COUNT_2 TUNNEL_LAYER_COUNT_3 TUNNEL_LAYER_COUNT_4
		#include "UnityCG.cginc"
		
		float4 _MainTex_TexelSize;
		sampler2D _TunnelTex1, _TunnelTex2, _TunnelTex3, _TunnelTex4;
	
		float4 _Params1; // x = travel speed, y = rotation speed, z = twist, w = brightness
		float4 _Params2; // x = travel speed, y = rotation speed, z = twist, w = brightness
		float4 _Params3; // x = travel speed, y = rotation speed, z = twist, w = brightness
		float4 _Params4; // x = travel speed, y = rotation speed, z = twist, w = brightness
		float4 _Params5; // x = contribution1, y = contribution 2, z = contribution 3, w = contribution 4
		float4 _MixParams; // x = fallOff, y = hyperspace effect, z = texture scale, w = global blend
		float  _FallOffStart;
		float4 _BackgroundColor, _Color;
		float4 _CurveParams;
		float4 _TextureTilings;
        float _Aperture;

		struct appdata {
			float4 vertex : POSITION;
			float2 texcoord: TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f {
			float4 pos : SV_POSITION;
			float3 uv: TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
		};
		
		v2f vert(appdata v) {
			v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			v.vertex.xy *= 1.0 + abs(v.vertex.z) * _Aperture;

			o.uv.xy = v.texcoord * _MixParams.zz;
			o.uv.x *= _MixParams.y;
            o.uv.z = v.vertex.z * _CurveParams.w;

            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float distToCam = distance(wpos, _WorldSpaceCameraPos);
            v.vertex.xy += sin(distToCam * _CurveParams.z + _Params1.x) * distToCam * _CurveParams.xy;
			o.pos = UnityObjectToClipPos(v.vertex);

			return o;
		}
    	
    	float4 getColor(sampler2D tex, float3 uv, float sp, float4 params) {
    		float2 uv2 = uv.xy + params.xy + float2(0, uv.z * _MixParams.w * params.z);
	    	float4 color = tex2D(tex, uv2 * sp);
    		return float4(color.rgb * params.www * color.aaa, color.a);
		}
	
		float4 frag(v2f i) : SV_Target {
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
			float4 f = float4(0,0,0,0);

			#if TUNNEL_BLEND_IN_ORDER
				#if TUNNEL_LAYER_COUNT_4
				f += (1.0-f.a) * _Params5.w * getColor(_TunnelTex4, i.uv * _TextureTilings.w, 8.0, _Params4);
				#endif
				#if TUNNEL_LAYER_COUNT_3 || TUNNEL_LAYER_COUNT_4
	   			f += (1.0-f.a) * _Params5.z * getColor(_TunnelTex3, i.uv * _TextureTilings.z, 4.0, _Params3);
	   			#endif
				#if TUNNEL_LAYER_COUNT_2 || TUNNEL_LAYER_COUNT_3 || TUNNEL_LAYER_COUNT_4
				f += (1.0-f.a) * _Params5.y * getColor(_TunnelTex2, i.uv * _TextureTilings.y, 2.0, _Params2);
				#endif
   				f += (1.0-f.a) * _Params5.x * getColor(_TunnelTex1, i.uv * _TextureTilings.x, 1.0, _Params1); 
			#else
				f = _Params5.x * getColor(_TunnelTex1, i.uv * _TextureTilings.x, 1.0, _Params1); 
				#if TUNNEL_LAYER_COUNT_2 || TUNNEL_LAYER_COUNT_3 || TUNNEL_LAYER_COUNT_4
				f += _Params5.y * getColor(_TunnelTex2, i.uv * _TextureTilings.y, 2.0, _Params2);
				#endif
				#if TUNNEL_LAYER_COUNT_3 || TUNNEL_LAYER_COUNT_4
   				f += _Params5.z * getColor(_TunnelTex3, i.uv * _TextureTilings.z, 4.0, _Params3);
   				#endif
				#if TUNNEL_LAYER_COUNT_4
				f += _Params5.w * getColor(_TunnelTex4, i.uv * _TextureTilings.w, 8.0, _Params4);
				#endif
			#endif
			f.rgb *= _Color.rgb;
			f = lerp(f, _BackgroundColor, smoothstep(_FallOffStart, _MixParams.x, i.uv.z));
			return f;
		}
			
		ENDCG
    }
  }  
}