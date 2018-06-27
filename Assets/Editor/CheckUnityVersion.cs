// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using UnityEditor;

// PolyToolkitDev namespace is for classes that exist only for developing Poly Toolkit itself,
// and don't ship out to users in the build.
namespace PolyToolkitDev {

[InitializeOnLoad]
public class CheckUnityVersion {
  private const string SUPPORTED_UNITY_VERSION = "5.6.3f1";
  static CheckUnityVersion() {
    if (Application.unityVersion != SUPPORTED_UNITY_VERSION) {
      EditorUtility.DisplayDialog(
        "Unsupported Unity version for the PolyToolkit project.",
        string.Format(
          "You are using Unity version:\n        {0}.\n" +
          "The known-good supported Unity version for this project is:\n        {1}.\n\n" +
          "Please DO NOT COMMIT any Unity asset files generated with an unsupported Unity " +
          "version, as that might break other team members.\n\n" +
          "Please switch to the supported Unity version asap!\n\n" +
          "(If you know this message is out of date, please fix Assets/Editor/CheckUnityVersion.cs)",
          Application.unityVersion, SUPPORTED_UNITY_VERSION), "OK");
    }
  }
}

}