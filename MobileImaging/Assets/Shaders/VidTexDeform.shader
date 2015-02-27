
Shader "Custom/VidTexDeform" {
	Properties {
		_MainTex("Texture", 2D) = "white" { }	
		
		//Note that these variables will show up in the Unity
		//editor, but but do not change these values manually.
		//The values will be set at runtime. 
		_ScaleFacXa("ScaleFacXa", Float) = 0
		_ScaleFacYa("ScaleFacYa", Float) = 0		
		_ScaleFacXb("ScaleFacXb", Float) = 0
		_ScaleFacYb("ScaleFacYb", Float) = 0	
	}
	
	SubShader{	
	
	Pass{
	
	Cull Back
	
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	float _ScaleFacXa;
	float _ScaleFacYa;
	float _ScaleFacXb;
	float _ScaleFacYb;
	
	struct v2f{
		float4  pos : SV_POSITION;
		float2  uv : TEXCOORD0;
	};
	
	
	v2f vert(appdata_base v){
		
		v2f o;
		float2 screenSpacePos;
		float yOffset;
		float4 clipPos;
		
		//get the y vertex position and store it
		yOffset = v.vertex.y;
		
		//now set the y vertex to 0
		v.vertex.y = 0.0f;
		
		//convert position from world space to clip space
		clipPos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		//Convert position from clip space to screen space.
		//Screen space has range x=-1 to x=1
		screenSpacePos.x = clipPos.x / clipPos.w;
		screenSpacePos.y = clipPos.y / clipPos.w;
		
		//If the video texture would not be flipped,
		//the screeen space range (-1 to 1) has to be converted to
		//the UV range 0 to 1 using this formula:
		//o.uv.x = (0.5f*screenSpacePos.x) + 0.5f;
		//o.uv.y = (0.5f*screenSpacePos.y) + 0.5f;
		//However, due to the fact that the 
		//video texture is flipped, we have to take this into account. 
		//Also, the video texture can be clipped, so we have to take this 
		//into account as well.
		o.uv.x = (_ScaleFacXa * screenSpacePos.x) + _ScaleFacXb;
		o.uv.y = -(_ScaleFacYa * screenSpacePos.y) + _ScaleFacYb;	

		
		//restore the original y offset.
		v.vertex.y = yOffset;
		
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);		
		return o;
	}
	
	half4 frag(v2f i) : COLOR{
		half4 texcol = tex2D(_MainTex, i.uv);
		
		return texcol;
	}
	
	ENDCG
	
	}
	}
} 