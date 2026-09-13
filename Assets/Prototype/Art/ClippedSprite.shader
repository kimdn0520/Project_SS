Shader "ProjectSS/ClippedSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WorldClipRect ("World bounds (left,bottom,right,top)", Vector) = (-100,-100,100,100)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float2 world:TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float4 _WorldClipRect;
            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.world = mul(unity_ObjectToWorld, input.vertex).xy;
                output.uv = input.uv; output.color = input.color * _Color;
                return output;
            }
            fixed4 frag(v2f input):SV_Target
            {
                float2 inside = min(input.world - _WorldClipRect.xy, _WorldClipRect.zw - input.world);
                clip(min(inside.x, inside.y));
                fixed4 color = tex2D(_MainTex, input.uv) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
