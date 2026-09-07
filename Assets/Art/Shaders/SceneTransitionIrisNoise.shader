Shader "Wildlife/UI/SceneTransitionIrisNoise"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _GridSize ("Grid Size", Range(4, 32)) = 14
        _TileDuration ("Tile Duration", Range(0.05, 0.9)) = 0.34
        _WaveEndPadding ("Wave End Padding", Range(0, 0.4)) = 0.12
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.2)) = 0.025
        _FlipXStrength ("Flip X Strength", Range(0, 1)) = 0.55
        _FlipYStrength ("Flip Y Strength", Range(0, 1)) = 0.7
        _Perspective ("Perspective", Range(0, 1.5)) = 0.45
        _TileScaleBoost ("Tile Scale Boost", Range(0, 0.5)) = 0.16
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _Progress;
            float _GridSize;
            float _TileDuration;
            float _WaveEndPadding;
            float _EdgeSoftness;
            float _FlipXStrength;
            float _FlipYStrength;
            float _Perspective;
            float _TileScaleBoost;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 gridCount = float2(max(1.0, _GridSize * aspect), max(1.0, _GridSize));
                float2 gridUv = input.uv * gridCount;
                float2 cell = floor(gridUv);
                float2 local = frac(gridUv) * 2.0 - 1.0;

                float maxOrder = max(gridCount.x + gridCount.y - 2.0, 1.0);
                float diagonalOrder = ((gridCount.x - 1.0 - cell.x) + (gridCount.y - 1.0 - cell.y)) / maxOrder;
                float duration = max(_TileDuration, 0.001);
                float waveTravel = max(1.0 - duration - _WaveEndPadding, 0.001);
                float tileStart = diagonalOrder * waveTravel;
                float tileProgress = saturate((_Progress - tileStart) / duration);
                float easedProgress = tileProgress * tileProgress * (3.0 - 2.0 * tileProgress);

                float flipAmount = 1.0 - easedProgress;
                float perspective = 1.0 + ((local.x * _FlipYStrength) - (local.y * _FlipXStrength)) * flipAmount * _Perspective;
                float2 cardLocal = local / max(perspective, 0.25);
                cardLocal.x /= max(lerp(0.22, 1.0, easedProgress), 0.05);
                cardLocal.y /= max(lerp(0.28, 1.0, easedProgress), 0.05);

                float scaleBoost = sin(easedProgress * 3.14159265) * _TileScaleBoost;
                float tileScale = lerp(0.02, 1.0, easedProgress) + scaleBoost;
                float boxDistance = max(abs(cardLocal.x), abs(cardLocal.y));
                float alpha = 1.0 - smoothstep(tileScale - _EdgeSoftness, tileScale + _EdgeSoftness, boxDistance);

                fixed4 spriteColor = tex2D(_MainTex, input.uv);
                fixed4 color = _Color * input.color * spriteColor;
                color.a *= saturate(alpha);
                return color;
            }
            ENDCG
        }
    }
}
