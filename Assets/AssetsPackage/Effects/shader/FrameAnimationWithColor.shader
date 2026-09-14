Shader "Custom/LineFrameAnimation_Final"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
        _RowCount ("Row Count", Float) = 4
        _ColCount ("Column Count", Float) = 4
        _AnimSpeed ("Frame Per Second", Float) = 10
        _Rotation ("Texture Rotation", Range(0,360)) = 0
        [Toggle] _DoubleSided ("Double Sided", Float) = 0
        
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Enum(Normal,0,Multiply,1,ColorBurn,2,Overlay,3)]
        _BaseBlendMode ("Base Blend Mode", Float) = 0

        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        [Toggle] _EnableEmission ("Enable Emission", Float) = 0
        [Enum(Add,0,Additive,1,SoftLight,2,HardLight,3)]
        _EmissionBlendMode ("Emission Blend Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull [_DoubleSided]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _RowCount;
            float _ColCount;
            float _AnimSpeed;
            float _Rotation;
            float _DoubleSided;

            float4 _BaseColor;
            float _BaseBlendMode;

            float4 _EmissionColor;
            float _EnableEmission;
            float _EmissionBlendMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float2 RotateUV(float2 uv, float angle)
            {
                float rad = angle * UNITY_PI / 180.0;
                float s = sin(rad);
                float c = cos(rad);
                uv -= 0.5;
                uv = float2(uv.x*c - uv.y*s, uv.x*s + uv.y*c);
                uv += 0.5;
                return uv;
            }

            uint GetCurrentFrame()
            {
                float totalFrames = _RowCount * _ColCount;
                return (uint)floor(fmod(_Time.y * _AnimSpeed, totalFrames));
            }

            float3 BlendBaseColor(float3 tex, float3 col)
            {
                switch((int)_BaseBlendMode)
                {
                    case 0: return tex * col;
                    case 1: return tex * col;
                    case 2: return 1.0 - (1.0 - tex) / max(col, 0.001);
                    case 3: return tex < 0.5 ? 2.0 * tex * col : 1.0 - 2.0 * (1.0 - tex) * (1.0 - col);
                    default: return tex * col;
                }
            }

            float3 BlendEmission(float3 src, float3 emis)
            {
                switch((int)_EmissionBlendMode)
                {
                    case 0: return src + emis;
                    case 1: return src + emis * 2.0;
                    case 2: return emis < 0.5 ? 2.0 * src * emis : 1.0 - 2.0 * (1.0 - src) * (1.0 - emis);
                    case 3: return src < 0.5 ? 2.0 * src * emis : 1.0 - 2.0 * (1.0 - src) * (1.0 - emis);
                    default: return src + emis;
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = RotateUV(i.uv, _Rotation);

                uint frame = GetCurrentFrame();
                float row = floor(frame / _ColCount);
                float col = fmod(frame, _ColCount);

                uv.x = (uv.x / _ColCount) + (col / _ColCount);
                uv.y = (uv.y / _RowCount) + (row / _RowCount);

                fixed4 tex = tex2D(_MainTex, uv);

                float3 finalRGB = BlendBaseColor(tex.rgb, _BaseColor.rgb * i.color.rgb);
                
                if (_EnableEmission > 0.5)
                    finalRGB = BlendEmission(finalRGB, _EmissionColor.rgb);

                return fixed4(finalRGB, tex.a * _BaseColor.a * i.color.a);
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}