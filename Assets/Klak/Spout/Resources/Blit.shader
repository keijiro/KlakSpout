// KlakSpout - Spout video frame sharing plugin for Unity
// https://github.com/keijiro/KlakSpout

Shader "Hidden/Spout/Blit"
{
    Properties
    {
        _MainTex("", 2D) = "white" {}
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    fixed _ClearAlpha;

    v2f_img vert_yflip(appdata_img v)
    {
        v2f_img o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = float2(v.texcoord.x, 1 - v.texcoord.y);
        return o;
    }

    fixed4 frag_sender(v2f_img i) : SV_Target
    {
        fixed4 col = tex2D(_MainTex, i.uv);
        col.a = saturate(col.a + _ClearAlpha);
        return col;
    }

    fixed4 frag_receiver(v2f_img i) : SV_Target
    {
        fixed4 col = tex2D(_MainTex, i.uv);
        #if !defined(UNITY_COLORSPACE_GAMMA)
        col.rgb = GammaToLinearSpace(col.rgb);
        #endif
        return col;
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_yflip
            #pragma fragment frag_sender
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_yflip
            #pragma fragment frag_receiver
            #pragma multi_compile _ UNITY_COLORSPACE_GAMMA
            ENDCG
        }
    }
}
