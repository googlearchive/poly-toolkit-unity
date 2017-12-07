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

using System.IO;
using UnityEditor;
using UnityEngine;

// PolyToolkitDev namespace is for classes that exist only for developing Poly Toolkit itself,
// and don't ship out to users in the build.
namespace PolyToolkitDev {

static class PrepForUASExport {
  [MenuItem("Poly/Dev/Prep for UAS Export")]
  public static void DoPrepForUASExport() {
    // The exported package should have the placeholder credentials, not our credentials.
    BuildPackage.ResetToPlaceholderCredentials();

    // We used to create upgrade.dat in the editor, so there might be left over copies of it in people's
    // working copies. To ensure that it's not exported, let's delete it.
    File.Delete(Application.dataPath + "/Editor/upgrade.dat");
    File.Delete(Application.dataPath + "/Editor/upgrade.dat.meta");

    EditorUtility.DisplayDialog("Ready", "Ready for Unity Asset Store export.", "OK");
  }
}

}