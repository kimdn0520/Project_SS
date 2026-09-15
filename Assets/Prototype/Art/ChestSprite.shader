Shader "ProjectSS/Sprites/ChestMatte"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct V {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct F {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   sampler2D _MainTex;
   F vert(V v){F o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
   fixed4 frag(F i):SV_Target
   {
    fixed4 c=tex2D(_MainTex,i.uv);
    // The generated sheet's neutral matte is excluded; the artwork uses chromatic brown outlines.
    float saturation=(max(c.r,max(c.g,c.b))-min(c.r,min(c.g,c.b)))/max(max(c.r,max(c.g,c.b)),.001);
    c.a*=smoothstep(.08,.22,saturation);
    return c*i.color;
   }
   ENDCG
  }
 }
}
