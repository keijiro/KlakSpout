Shader "Hidden/Klak/Spout/Blit"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;

    // Note: The effect of the v-flip option is reversed here.
    // We have to do v-flip actually on no-v-flip passes.

    void Vertex(float4 position : POSITION,
                float2 texCoord : TEXCOORD0,
                out float4 outPosition : SV_Position,
                out float2 outTexCoord : TEXCOORD0)
    {
        outPosition = UnityObjectToClipPos(position);
        outTexCoord = float2(texCoord.x, 1 - texCoord.y);
    }

    void VertexVFlip(float4 position : POSITION,
                     float2 texCoord : TEXCOORD0,
                     out float4 outPosition : SV_Position,
                     out float2 outTexCoord : TEXCOORD0)
    {
        outPosition = UnityObjectToClipPos(position);
        outTexCoord = texCoord;
    }

    float4 BlitSimple(float4 position : SV_Position,
                      float2 texCoord : TEXCOORD0) : SV_Target
    {
        return tex2D(_MainTex, texCoord);
    }

    float4 BlitClearAlpha(float4 position : SV_Position,
                          float2 texCoord : TEXCOORD0) : SV_Target
    {
        return float4(tex2D(_MainTex, texCoord).rgb, 1);
    }

    float4 BlitFromSrgb(float4 position : SV_Position,
                        float2 texCoord : TEXCOORD0) : SV_Target
    {
        float4 c = tex2D(_MainTex, texCoord);
        #ifndef UNITY_COLORSPACE_GAMMA
        c.rgb = GammaToLinearSpace(c.rgb);
        #endif
        return c;
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment BlitSimple
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment BlitClearAlpha
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexVFlip
            #pragma fragment BlitSimple
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex VertexVFlip
            #pragma fragment BlitClearAlpha
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment BlitFromSrgb
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            ENDCG
        }
    }
}
