Shader "Custom/JustColor" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1)
    }

    SubShader {
    
        ZWrite On
        Cull off
        Lighting Off
        Color [_Color]
    
        Pass {}
    }
}