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

Shader "Brush/StandardDoubleSided" {
Properties {
  _Color ("Main Color", Color) = (1,1,1,1)
  _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
  _MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
  _BumpMap ("Normalmap", 2D) = "bump" {}
  _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
}
    SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 400
    Cull Off

    CGPROGRAM
    #pragma target 3.0
    #pragma surface surf StandardSpecular vertex:vert alphatest:_Cutoff addshadow
    #pragma multi_compile __ AUDIO_REACTIVE
    #pragma multi_compile __ TBT_LINEAR_TARGET

    #include "../../../Shaders/Include/Brush.cginc"

    struct Input {
      float2 uv_MainTex;
      float2 uv_BumpMap;
      float4 color : Color;
      fixed vface : VFACE;
    };

    sampler2D _MainTex;
    sampler2D _BumpMap;
    fixed4 _Color;
    half _Shininess;

    void vert (inout appdata_full i /*, out Input o*/) {
      // UNITY_INITIALIZE_OUTPUT(Input, o);
      // o.tangent = v.tangent;
      i.color = TbVertToNative(i.color);
    }

    void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
      fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
      o.Albedo = tex.rgb * _Color.rgb * IN.color.rgb;
      o.Smoothness = _Shininess;
      o.Specular = _SpecColor;
      o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
      o.Normal.z *= IN.vface;

      o.Alpha = tex.a * IN.color.a;
    }
      ENDCG
    }

  // MOBILE VERSION
  SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
    LOD 100
    Cull Off

  CGPROGRAM
  #pragma surface surf Lambert vertex:vert alphatest:_Cutoff
  #pragma target 3.0

  sampler2D _MainTex;
  fixed4 _Color; 

  struct Input {
    float2 uv_MainTex;
    float4 color : COLOR;
    fixed vface : VFACE;
  };

  void vert (inout appdata_full v) {
    }

  void surf (Input IN, inout SurfaceOutput o) {
    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
    o.Albedo = c.rgb * IN.color.rgb;
    o.Alpha = c.a * IN.color.a;
    o.Normal = float3(0,0,IN.vface);
  }
  ENDCG
  }

  FallBack "Transparent/Cutout/VertexLit"
}
