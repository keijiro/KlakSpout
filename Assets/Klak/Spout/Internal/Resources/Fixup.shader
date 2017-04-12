// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
Shader "Hidden/Spout/Fixup"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define SPOUT_SENDER
            #include "Fixup.cginc"
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            #define SPOUT_RECEIVER
            #include "Fixup.cginc"
            ENDCG
        }
    }
}
