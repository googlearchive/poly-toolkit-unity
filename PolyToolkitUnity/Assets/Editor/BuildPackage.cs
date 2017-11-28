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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// PolyToolkitDev namespace is for classes that exist only for developing Poly Toolkit itself,
// and don't ship out to users in the build.
namespace PolyToolkitDev {

static class BuildPackage {
  /// Location of the VERSION.txt file in the git checkout.
  /// It exists in git because we want Unity to give it a .meta file.
  static string kVersionNormalLocation = "Assets/PolyToolkit/Editor/DummyVERSION.txt";

  /// Location of the VERSION.txt file in the output .unitypackage
  static string kVersionBuildLocation = "Assets/PolyToolkit/VERSION.txt";

  [System.Serializable()]
  public class BuildFailedException : System.Exception {
    public BuildFailedException(string fmt, params object[] args)
      : base(string.Format(fmt, args)) { }
  }

  /// Temporarily creates a VERSION.txt build stamp so we can embed it
  /// in the unitypackage. Cleans up afterwards.
  /// Ensures that the VERSION.txt has a consistent GUID.
  class TempBuildStamp : System.IDisposable {
    byte[] m_previousContents;
    public TempBuildStamp(string contents) {
      string err = AssetDatabase.MoveAsset(
          kVersionNormalLocation, kVersionBuildLocation);
      if (err != "") {
        throw new BuildFailedException(
            "Couldn't move {0} to {1}: {2}",
            kVersionNormalLocation, kVersionBuildLocation, err);
      }
      m_previousContents = File.ReadAllBytes(kVersionBuildLocation);
      File.WriteAllText(kVersionBuildLocation, contents);
    }

    public void Dispose() {
      string err = AssetDatabase.MoveAsset(kVersionBuildLocation, kVersionNormalLocation);
      if (err == "" && m_previousContents != null) {
        File.WriteAllBytes(kVersionNormalLocation, m_previousContents);
      }
    }
  }

  /// Uses "git describe" to get a human-readable version number.
  static string GetGitVersion() {
    // Start the child process.
    var p = new System.Diagnostics.Process();
    // Redirect the output stream of the child process.
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.FileName = "git.exe";
    p.StartInfo.Arguments = "describe";
    p.Start();
    // Do not wait for the child process to exit before
    // reading to the end of its redirected stream.
    // p.WaitForExit();
    // Read the output stream first and then wait.
    var version = p.StandardOutput.ReadToEnd().Trim();
    p.WaitForExit();
    if (p.ExitCode != 0) {
      throw new BuildFailedException("git describe exited with code {0}", p.ExitCode);
    }
    return version;
  }

  /// Creates a .unitypackage file named after the current git version.
  /// Writes to the root of the git repo.
  [MenuItem("Poly/Build .unitypackage")]
  static void DoBuild() {
    string version = GetGitVersion();
    string name = string.Format("../poly-toolkit-{0}.unitypackage", version);

    using (var tmp = new TempBuildStamp(version)) {
      AssetDatabase.ExportPackage(
        GetFilesToExport(),
        name,
        ExportPackageOptions.Recurse);
      Debug.LogFormat("Done building {0}", name);
    }
  }

  // Create a list of files to include in the unity package export.
  static string[] GetFilesToExport() {
    // Include all files from the PolyToolkit directory EXCEPT for "upgrade.dat", which is responsible for 
    // informing PolyToolkit whether or not it has been previously installed or upgraded.
    string path = Application.dataPath;
    IEnumerable<string> files = Directory.GetFiles(path + "/PolyToolkit", "*", SearchOption.AllDirectories)
      .Where(s => !s.Contains("upgrade.dat"));
    // Correct the path so it is relative to the package.
    return files.Select(x => x.Replace(Application.dataPath, "Assets")).ToArray();
  }
}

}