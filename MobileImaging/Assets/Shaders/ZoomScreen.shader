

Shader "Custom/ZoomScreen" {
	Properties {
	
		_MainTex("Texture", 2D) = "white" { }	
		_CookieTex("Cookie", 2D) = "white" { }	
		
		//Note that these variables will show up in the Unity
		//editor, but but do not change these values manually.
		//The values will be set at runtime. 
		_ScaleFacXa("ScaleFacXa", Float) = 0
		_ScaleFacYa("ScaleFacYa", Float) = 0		
		_ScaleFacXb("ScaleFacXb", Float) = 0
		_ScaleFacYb("ScaleFacYb", Float) = 0	
		
		//TODO: figure out if I can transfer these 4 variables via 
		//a color vector instead.
		_DisplaceVectorX("DisplaceVectorX",  float) = 0
		_DisplaceVectorY("DisplaceVectorY",  float) = 0
		_ObjectPositionScreenSpaceX("ObjectPositionScreenSpaceX", Float) = 0
		_ObjectPositionScreenSpaceY("ObjectPositionScreenSpaceY", Float) = 0
		
		//this assumes the zoom screen mesh is a square, not a rectangle.
		_ZoomFactor("ZoomFactor", Float) = 0	
	}
	
	SubShader{	
	
	Pass{
	
	Cull Back
	
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	sampler2D _CookieTex;
	float _ScaleFacXa;
	float _ScaleFacYa;
	float _ScaleFacXb;
	float _ScaleFacYb;
	float _DisplaceVectorX;
	float _DisplaceVectorY;
	float _ZoomFactor;	
	float _ObjectPositionScreenSpaceX;
	float _ObjectPositionScreenSpaceY;

	
	struct v2f{
		float4  pos : SV_POSITION;
		float2  uv0 : TEXCOORD0;
		float2  uv1 : TEXCOORD1;
	};
	
	
	v2f vert(appdata_base v){
		
		v2f o;
		float2 displaceVector;
		float2 vertexScreenSpacePos;
		float2 modScreenSpacePos;
		float4 clipPos;
		float2 objectPositionScreenSpace;
		float2 zoomVector;
	
		//Stick the single variables in a vector for easier handling.
		//This is to be replaced by sending the variable from the script
		//to the shader as a vector instead of single variables.
		displaceVector.x = _DisplaceVectorX;
		displaceVector.y = _DisplaceVectorY;		
		objectPositionScreenSpace.x = _ObjectPositionScreenSpaceX;
		objectPositionScreenSpace.y = _ObjectPositionScreenSpaceY;
		
		//convert position from world space to clip space
		clipPos = mul(UNITY_MATRIX_MVP, v.vertex);
		
		//Convert position from clip space to screen space.
		//Screen space has range x=-1 to x=1
		vertexScreenSpacePos.x = clipPos.x / clipPos.w;
		vertexScreenSpacePos.y = clipPos.y / clipPos.w;
			
		//Get a vector from the current vertex position to the center of the object.
		zoomVector = objectPositionScreenSpace - vertexScreenSpacePos;
		
		//Set the length of the zoom vector
		zoomVector *= _ZoomFactor; 

		//Modify the screen space position so the zoom screen seems to
		//be positioned somewhere else and has a different size.
		modScreenSpacePos = vertexScreenSpacePos + displaceVector;
		
		//add the zoom factor
		modScreenSpacePos += zoomVector;
		
		//If the video texture would not be flipped,
		//the screeen space range (-1 to 1) has to be converted to
		//the UV range 0 to 1 using this formula:
		//o.uv.x = (0.5f*vertexScreenSpacePos.x) + 0.5f;
		//o.uv.y = (0.5f*vertexScreenSpacePos.y) + 0.5f;
		//However, due to the fact that both the Vuforia and String
		//video texture is flipped, we have to take this into account. 
		//Also, the video texture can be clipped, so we have to take this 
		//into account as well.
		o.uv0.x = (_ScaleFacXa * modScreenSpacePos.x) + _ScaleFacXb;
		o.uv0.y = -(_ScaleFacYa * modScreenSpacePos.y) + _ScaleFacYb;	
		
	    //this is for the cookie texture. Do not change the coordinates.
	    o.uv1 = v.texcoord.xy;
	 
	    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);

	    return o;
	}
	
	half4 frag(v2f i) : COLOR{
	
		//Get the video texture
		half4 Sampled2D1 = tex2D(_MainTex, i.uv0);
		
		//Get the cookie texture
		half4 Sampled2D0 = tex2D(_CookieTex, i.uv1);
		
		//Overlay the cookie texture over the video texture. The cookie texture
		//must be set to "Alpha from Grayscale". 
		half4 Lerp0 = lerp(Sampled2D1, Sampled2D0, Sampled2D0.a);

		return Lerp0;
	}
	
	ENDCG
	
	}
	}
} 