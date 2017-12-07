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
using System.IO;
using System.Text;
using PolyToolkit;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

public static class PtUtils {
  /// <summary>
  /// Normalizes a Unity local asset path: trims, converts back slashes into forward slashes,
  /// removes trailing slash.
  /// </summary>
  /// <param name="path">The path to normalize (e.g., " Assets\Foo\Bar\Qux  ").</param>
  /// <returns>The normalized path (e.g., "Assets/Foo/Bar/Qux")</returns>
  public static string NormalizeLocalPath(string path) {
    path = path.Trim().Replace('\\', '/');
    // Strip the trailing slash, if there is one.
    if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
    return path;
  }

  /// <summary>
  /// Converts a local path (like "Assets/Foo/Bar") into an absolute system-dependent path
  /// (like "C:\Users\foo\MyUnityProject\Assets\Foo\Bar").
  /// </summary>
  /// <param name="localPath">The local path to convert.</param>
  /// <returns>The absolute path.</returns>
  public static string ToAbsolutePath(string localPath) {
    return Path.Combine(
      Path.GetDirectoryName(Application.dataPath).Replace('/', Path.DirectorySeparatorChar),
      localPath.Replace('/', Path.DirectorySeparatorChar));
  }

  /// <summary>
  /// Sanitizes the given string to use as a file name.
  /// </summary>
  /// <param name="str">The string to sanitize</param>
  /// <returns>The sanitized version, with invalid characters converted to _.</returns>
  public static string SanitizeToUseAsFileName(string str) {
    if (str == null) {
      throw new System.Exception("Can't sanitize a null string");
    }
    StringBuilder sb = new StringBuilder();
    bool lastCharElided = false;
    for (int i = 0; i < Mathf.Min(str.Length, 40); i++) {
      char c = str[i];
      if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) {
        lastCharElided = false;
      } else {
        if (lastCharElided) continue;
        c = '_';
        lastCharElided = true;
      }
      sb.Append(c);
    }
    return sb.ToString();
  }

  /// <summary>
  /// Returns the default asset path for the given asset.
  /// </summary>
  public static string GetDefaultPtAssetPath(PolyAsset asset) {
    return string.Format("{0}/{1}.asset",
      NormalizeLocalPath(PtSettings.Instance.assetObjectsPath),
      GetPtAssetBaseName(asset));
  }

  public static string GetPtAssetBaseName(PolyAsset asset) {
    return string.Format("{0}_{1}_{2}",
      SanitizeToUseAsFileName(asset.displayName),
      SanitizeToUseAsFileName(asset.authorName),
      SanitizeToUseAsFileName(asset.name).Replace("assets_", ""));
  }
}

}