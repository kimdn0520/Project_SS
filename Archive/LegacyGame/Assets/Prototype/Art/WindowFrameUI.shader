Shader "ProjectSS/UI/WindowFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HalfExtent ("Source silhouette half extent", Float) = 610
        _CornerRadius ("Source corner radius", Float) = 140
        _UseDarkKey ("Remove dark matte instead of rounded mask", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float2 localUV:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; float2 localUV:TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex; fixed4 _Color; fixed4 _TextureSampleAdd; float4 _ClipRect; float _HalfExtent; float _CornerRadius; float _UseDarkKey;
            v2f vert(appdata v)
            {
                v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.localUV=v.localUV;o.color=v.color*_Color;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=(tex2D(_MainTex,i.uv)+_TextureSampleAdd)*i.color;
                // Clip only the generated sprite's outside matte. UV coordinates retain
                // the authored corner geometry through Unity's nine-slice tessellation.
                float2 q=abs((i.localUV-.5)*1254)-(_HalfExtent-_CornerRadius);
                float d=length(max(q,0))+min(max(q.x,q.y),0)-_CornerRadius;
                float silhouette=saturate(.5-d/max(fwidth(d),.5));
                fixed3 source=tex2D(_MainTex,i.uv).rgb;
                float darkKey=smoothstep(.025,.075,max(source.r,max(source.g,source.b)));
                float chroma=(max(source.r,max(source.g,source.b))-min(source.r,min(source.g,source.b)))/max(max(source.r,max(source.g,source.b)),.001);
                color.a*=_UseDarkKey>1.5?smoothstep(.08,.22,chroma):lerp(silhouette,darkKey,_UseDarkKey);
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
