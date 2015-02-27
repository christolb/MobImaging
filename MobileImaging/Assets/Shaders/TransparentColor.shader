Shader "Custom/TransparentColor" {
    Properties {
        _Color ("Main Color, Alpha", Color) = (1,1,1,1)
    }

    SubShader {
    
        ZWrite Off
        Cull off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Color [_Color]
    
        Pass {}
    }
}