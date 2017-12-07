// Copyright 2017 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

//
// Tilt Brush variant of the Blocks gem shader
//
Shader  "Blocks/BlocksGem"  {
  Properties {
    _Color ("Color", Color) = (1,1,1,1)
    _BumpMap ("Normal Map", 2D) = "bump" {}
    _Shininess ("Shininess", Range(0,1)) = 0.8
    _RimIntensity ("Rim Intensity", Range(0,1)) = .2
    _RimPower ("Rim Power", Range(0,16)) = 5
    _Frequency ("Frequency", Float) = 1
    _Jitter ("Jitter", Float) = 1 
  }

  SubShader {

  Tags { "RenderType"="Transparent" "Queue"="Transparent"}
  LOD 200

  Blend One SrcAlpha
  Zwrite Off
  Cull Back

  CGPROGRAM
  #pragma surface surf StandardSpecular vertex:vert fullforwardshadows nofog
  #pragma target 3.0
  #include "../../../Shaders/Include/Brush.cginc"
  // Our adaptation of the GPU Voronoi noise routines:
  #include "Assets/PolyToolkit/ThirdParty/GPU-Voronoi-Noise/Assets/GPUVoronoiNoise/Shader/BlocksGemGPUVoronoiNoise.cginc"

  struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
    float3 localPos;
    float3 worldRefl;
    float3 viewDir;

    INTERNAL_DATA
  };

  half _Shininess; 
  half _RimIntensity;
  half _RimPower;
  fixed4 _Color; 
  sampler2D _BumpMap;

  void vert (inout appdata_full v, out Input o) {
   UNITY_INITIALIZE_OUTPUT(Input,o);
   o.localPos = v.vertex.xyz;
 }

  void surf(Input IN, inout SurfaceOutputStandardSpecular o) {
    
    float2 F = fBm_F0(IN.localPos, OCTAVES); 
    float gem = (F.y - F.x);
        
    // Perturb normal with voronoi cells
    // Hack to convert normal to tangent space. _Bump map is actually null.
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
    half perturbIntensity = 10;
    o.Normal.x += ddy(gem) * perturbIntensity;
    o.Normal.y += ddx(gem) * perturbIntensity;
    o.Normal = normalize(o.Normal);

    o.Albedo = 0;

    // Artifical diffraction highlights to simulate what I see in blocks. Tuned to taste.
    half3 refl = clamp(WorldReflectionVector (IN, o.Normal) + gem, -1.0,1.0);
    float3 colorRamp = float3(1,.3,0)*sin(refl.x * 30) + float3(0,1,.5)*cos(refl.y * 37.77) + float3(0,0,1)*sin(refl.z*43.33);

    // Use the voronoi for a specular mask
    half mask = saturate((1 - gem) + .25);
    o.Specular = _Color.rgb + colorRamp*.1; 
    o.Smoothness = _Shininess;

    // Artificial rim lighting
    o.Emission =  (pow(1 - saturate(dot(IN.viewDir, o.Normal)), _RimPower)) * _RimIntensity;
  }
    ENDCG
} // end subshader

  FallBack "Diffuse"
} // end shader
