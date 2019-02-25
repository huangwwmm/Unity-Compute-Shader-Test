Shader "Custom/waterscreen1" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Water ("WaterBlur (BW)", 2D) = "white" {}
		_Slide ("SliderTime", Float) = 0
		
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		
		CGPROGRAM
		#pragma surface surf Lambert

		struct Input {
			float2 uv_MainTex;
			float2 uv_Water;
		};
		
		sampler2D _MainTex;
		sampler2D _Water;
		uniform float _Slide;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			float fader = smoothstep( 1, 0.0, _Slide*7);
			half4 waterflow = tex2D(_Water, float2(IN.uv_Water.x,IN.uv_Water.y+(5)))*fader;
			half4 col1 = tex2D(_MainTex, float2(IN.uv_MainTex.x+waterflow.r,IN.uv_MainTex.y+waterflow.r))*1;
			o.Albedo = col1.rgb;
			o.Alpha = 1;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
