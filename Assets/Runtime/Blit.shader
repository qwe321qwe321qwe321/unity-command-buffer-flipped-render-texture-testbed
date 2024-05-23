Shader "Custom/MyBlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #pragma multi_compile _ COND_UNITY_UV_STARTS_AT_TOP
            #pragma multi_compile _ COND_PROJECTION_PARAM_X

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            uniform float4 _MainTex_ST;
            uniform float4 _MainTex_TexelSize;
            uniform float4 _Color;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                #ifdef COND_UNITY_UV_STARTS_AT_TOP
                #if UNITY_UV_STARTS_AT_TOP
                if (_MainTex_TexelSize.y < 0)
                    o.texcoord.y = 1.0 - o.texcoord.y;
                #endif
                #endif

                #ifdef COND_PROJECTION_PARAM_X
                if (_ProjectionParams.x < 0)
                    o.texcoord.y = 1.0 - o.texcoord.y;
                #endif
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord);
            }
            ENDCG

        }
    }
    Fallback Off
}
