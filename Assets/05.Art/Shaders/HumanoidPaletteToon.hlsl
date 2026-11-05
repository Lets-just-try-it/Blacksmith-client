#ifndef HUMANOID_PALETTE_TOON_INCLUDED
#define HUMANOID_PALETTE_TOON_INCLUDED

// 인간형 모델의 팔레트 램프 툰 셰이딩. 사양은 Blender 익스포트 문서(Hero_v2 README 3절)를 따른다.
// - 메쉬 UV의 v가 팔레트 행(색 램프)을 가리키고, u는 해 방향에 따른 명암 단계(그림자·기본·하이라이트 열)로 바꾼다.
// - 렌더러마다 행 교체(Swap) 두 쌍으로 피부색·머리색 같은 변형을 보여 준다. From이 -1이면 쓰지 않는다.
// - 사양과 달리 낮·밤 조명에 따라 어두워지도록 해 색·세기와 환경광을 곱한다.

#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

float HumanoidPaletteRow(float2 uv, float rows, float4 swaps)
{
    float row = floor((1.0 - uv.y) * rows);
    if (abs(row - swaps.x) < 0.5)
    {
        row = swaps.y;
    }
    else if (abs(row - swaps.z) < 0.5)
    {
        row = swaps.w;
    }

    return row;
}

void HumanoidPaletteToon_float(UnityTexture2D Palette, float2 UV, float3 NormalWS, float3 PositionWS,
    float Rows, float T1, float T2, float Swap0From, float Swap0To, float Swap1From, float Swap1To, float SunWeight, float AmbientWeight, out float3 Color)
{
    float row = HumanoidPaletteRow(UV, Rows, float4(Swap0From, Swap0To, Swap1From, Swap1To));
#ifdef SHADERGRAPH_PREVIEW
    float band = 1.0;
    float3 lighting = 1.0;
#else
    Light sun = GetMainLight(TransformWorldToShadowCoord(PositionWS));
    float ndotl = saturate(dot(normalize(NormalWS), sun.direction)) * sun.shadowAttenuation;
    float band = ndotl < T1 ? 0.0 : (ndotl < T2 ? 1.0 : 2.0);
    float3 lighting = sun.color * SunWeight + SampleSH(float3(0.0, 1.0, 0.0)) * AmbientWeight;
#endif
    float2 paletteUV = float2((band + 0.5) / 4.0, 1.0 - (row + 0.5) / Rows);
    Color = SAMPLE_TEXTURE2D_LOD(Palette.tex, Palette.samplerstate, paletteUV, 0).rgb * lighting;
}

void HumanoidPaletteToon_half(UnityTexture2D Palette, float2 UV, float3 NormalWS, float3 PositionWS,
    float Rows, float T1, float T2, float Swap0From, float Swap0To, float Swap1From, float Swap1To, float SunWeight, float AmbientWeight, out float3 Color)
{
    HumanoidPaletteToon_float(Palette, UV, NormalWS, PositionWS, Rows, T1, T2, Swap0From, Swap0To, Swap1From, Swap1To, SunWeight, AmbientWeight, Color);
}

#endif
