Shader "Unlit/SpriteCustomShader"
{
    Properties 
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader 
    {
        Tags { "Queue" = "Geometry+1" "RenderType" = "Transparent" }
        Pass {
            Cull Off
            ZWrite Off
            ColorMask 0 // Invisible
            Blend One One
            Stencil {
                Ref 1
                Comp always
                Pass replace
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
            };
            v2f vert(appdata_t v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.texcoord) * i.color;
                clip(col.a - 0.1); // Only write stencil where sprite is visible
                return col; // Must return a color, even if ColorMask is 0
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
