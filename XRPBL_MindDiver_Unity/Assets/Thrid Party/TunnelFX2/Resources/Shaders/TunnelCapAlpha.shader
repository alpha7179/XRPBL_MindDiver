Shader "TunnelEffect/TunnelCapAlpha" {
	Properties {
		_BackgroundColor ("Transition Color", Color) = (1,1,1)
        _CurveParams("Curve Params", Vector) = (0.02, 15.0, 0.01)
        _Params1 ("Params 1", Vector) = (1.5, 0.5, 0.1, 0.12)
        _Behind("Behind", Int) = 0
        _Aperture("Aperture", Float) = 0
	}
   	SubShader {
       Tags {
	       "Queue"="Transparent-1"
	       "RenderType"="Transparent"
       }
       Stencil {
            Ref 2
            Comp NotEqual
            Pass Replace
       }
       ZWrite Off
       Cull Off
       Blend SrcAlpha OneMinusSrcAlpha

       Pass {
    	CGPROGRAM
		#pragma vertex vert	
		#pragma fragment frag
        #pragma fragmentoption ARB_precision_hint_fastest
        #include "UnityCG.cginc"

		half4  _BackgroundColor;
        float3 _CurveParams;
        float4 _Params1; // x = travel speed, y = rotation speed, z = twist, w = brightness
		int _Behind;
        float _Aperture;

		struct appdata {
			float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f {
			float4 pos : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
		};
		
		v2f vert(appdata v) {
			v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

			v.vertex.xy *= 1.0 + abs(v.vertex.z) * _Aperture;

            float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float distToCam = distance(wpos, _WorldSpaceCameraPos);

            v.vertex.xy += sin(distToCam * _CurveParams.z + _Params1.x) * distToCam * _CurveParams.xy;
			o.pos = UnityObjectToClipPos(v.vertex);

            #if UNITY_REVERSED_Z
                float behind = 0.0001;
            #else
                float behind = o.pos.w * 0.9999;
            #endif
            o.pos.z = lerp(o.pos.z, behind, _Behind);

			return o;
		}
    	
		fixed4 frag(v2f i) : SV_Target {
            UNITY_SETUP_INSTANCE_ID(i);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
			return _BackgroundColor;
		}
			
		ENDCG
    }
  }  
}