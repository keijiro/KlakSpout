// KlakSpout - Spout realtime video sharing plugin for Unity
// https://github.com/keijiro/KlakSpout
#include "UnityCG.cginc"

sampler2D _MainTex;
fixed _ClearAlpha;

v2f_img vert(appdata_img v)
{
    v2f_img o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv = float2(v.texcoord.x, 1 - v.texcoord.y);
    return o;
}

fixed4 frag(v2f_img i) : SV_Target
{
    fixed4 col = tex2D(_MainTex, i.uv);
    #if defined(SPOUT_RECEIVER) && !defined(UNITY_COLORSPACE_GAMMA)
        col.rgb = GammaToLinearSpace(col.rgb);
    #endif
    col.a = saturate(col.a + _ClearAlpha);
    return col;
}
