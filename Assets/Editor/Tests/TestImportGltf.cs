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

//NO_POLY_TOOLKIT_INTERNAL_CHECK
using NUnit.Framework;
using PolyToolkit;
using PolyToolkitInternal;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;


// PolyToolkitDev namespace is for classes that exist only for developing Poly Toolkit itself,
// and don't ship out to users in the build.
namespace PolyToolkitDev {

internal class TestImportGltf {
  internal const string kMoto = "TestData/gltf1/Motorcycle_bBbozwADWnS.gltf";
  internal const string kAllBrush10 = "TestData/gltf1/All_Brushes_TB10.gltf";
  internal const string kAllBrush14 = "TestData/gltf1/All_Brushes_TB14.gltf";
  internal const string kComputer = "TestData/gltf2/computer.gltf";
  internal const string kGoblets = "TestData/gltf2/goblets.gltf";

  // The root of this git repository
  static string RepoRoot { get {
      return Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
    } }

  static void AddResultsToAsset(ImportGltf.GltfImportResult result, UnityEngine.Object asset) {
    foreach (var texture in result.textures) {
      AssetDatabase.AddObjectToAsset(texture, asset);
    }
    foreach (var mesh in result.meshes) {
      AssetDatabase.AddObjectToAsset(mesh, asset);
    }
    foreach (var material in result.materials) {
      AssetDatabase.AddObjectToAsset(material, asset);
    }
  }

  // Save like so:
  // PtAsset (references GameObject)
  //   Meshes, Textures, Materials
  // GameObject
  //   References sub-asset meshes in PtAsset
  //
  // Does not currently handle replacement
  static void SaveAsSeparateWithMeshesInAsset(
      ImportGltf.GltfImportResult result, string assetPath, string prefabPath) {
    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
    Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));

    GameObject prefabToReplace = null; {
      PtAsset assetToReplace = AssetDatabase.LoadAssetAtPath<PtAsset>(assetPath);
      if (assetToReplace != null) {
        if (assetToReplace.assetPrefab == null) {
          Debug.LogErrorFormat("Couldn't find prefab for asset {0}.", assetToReplace);
          return;
        }
        prefabToReplace = assetToReplace.assetPrefab;
      }
    }

    PtAsset ptAsset = ScriptableObject.CreateInstance<PtAsset>();
    ptAsset.title = "bogus title";
    // Must make ptAsset a real asset before adding objects to it
    AssetDatabase.CreateAsset(ptAsset, assetPath);
    // Should make meshes real assets before saving them into a prefab
    AddResultsToAsset(result, ptAsset);
    AssetDatabase.ImportAsset(assetPath);

    // Create prefab after meshes are safely ensconced as assets
    result.root.AddComponent<PtAssetObject>().asset = ptAsset;

    GameObject newPrefab = null;
    if (prefabToReplace != null) {
      // Replace the existing prefab with our new object, without breaking prefab connections.
      // There's nothing but prefab in the asset so we don't need to worry about clearing
      // anything else out.
      newPrefab = PrefabUtility.ReplacePrefab(
          result.root, prefabToReplace, ReplacePrefabOptions.ReplaceNameBased);
    } else {
      if (File.Exists(prefabPath)) {
        // Could probably handle this like the case above; replace it.
        Debug.LogErrorFormat("Unexpected: overwriting a prefab {0}", prefabPath);
      } else {
        newPrefab = PrefabUtility.CreatePrefab(prefabPath, result.root);
      }
    }

    ptAsset.assetPrefab = newPrefab;
    EditorUtility.SetDirty(ptAsset);

    // Maybe not needed?
    AssetDatabase.Refresh();
  }

  // Save like so:
  // GameObject
  //   Meshes, Textures, Materials
  //   Sub-objects reference sibling meshes
  //   PtAsset references the top-level prefab
  //
  // When replacing an existing prefab:
  // - scene objects that reference the prefab get properly updated
  // - Unless explicitly destroyed, old meshes stick around in the prefab and are
  //   not replaced (so the prefab has many meshes with duplicate names)
  // - Scene objects that reference mesh sub-assets keep referencing the same mesh
  //   (if they are left around) or get dangling mesh references (if they are destroyed)
  //   There is no known way to replace the existing meshes by name or otherwise,
  //   unless we implement it manually (eg by mutating the mesh sub-assets)
  //
  static void SaveAsSinglePrefab(
      ImportGltf.GltfImportResult result, string prefabPath) {
    Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));

    PtAsset ptAsset = ScriptableObject.CreateInstance<PtAsset>();
    ptAsset.name = Path.GetFileName(prefabPath).Replace(".prefab", "");

    GameObject oldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    GameObject prefab = null;
    if (oldPrefab == null) {
      // Chicken and egg problem: the Meshes aren't assets yet, so refs to them will dangle
      prefab = PrefabUtility.CreatePrefab(prefabPath, result.root);
      AddResultsToAsset(result, prefab);
      // This fixes up the dangling refs
      prefab = PrefabUtility.ReplacePrefab(result.root, prefab);
    } else {
      // ReplacePrefab only removes the GameObjects from the asset.
      // Clear out all non-prefab junk (ie, meshes), because otherwise it piles up.
      // The main difference between LoadAllAssetRepresentations and LoadAllAssets
      // is that the former returns MonoBehaviours and the latter does not.
      foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(prefabPath)) {
        if (! (obj is GameObject)) {
          Object.DestroyImmediate(obj, allowDestroyingAssets: true);
        }
      }
      AddResultsToAsset(result, oldPrefab);
      prefab = PrefabUtility.ReplacePrefab(
          result.root, oldPrefab, ReplacePrefabOptions.ReplaceNameBased);
    }
    AssetDatabase.AddObjectToAsset(ptAsset, prefab);
    ptAsset.assetPrefab = prefab;
    EditorUtility.SetDirty(ptAsset);
    AssetDatabase.ImportAsset(prefabPath);
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Save as Separate")]
  public static void TestSaveAsSeparateWithMeshesInAsset() {
    IUriLoader binLoader = new BufferedStreamLoader(Path.GetDirectoryName(Path.Combine(RepoRoot, kAllBrush10)));
    ImportGltf.GltfImportResult result = null;
    using (TextReader reader = new StreamReader(Path.Combine(RepoRoot, kAllBrush10))) {
      result = ImportGltf.Import(GltfSchemaVersion.GLTF1, reader, binLoader, PolyImportOptions.Default());
    }
    string assetPath = "Assets/Poly/TestData/separate_a.asset";
    string prefabPath = "Assets/Poly/TestData/separate_p.prefab";
    SaveAsSeparateWithMeshesInAsset(result, assetPath, prefabPath);
    GameObject.DestroyImmediate(result.root);
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Save as Single")]
  public static void TestSaveAsSinglePrefab() {
    IUriLoader binLoader = new BufferedStreamLoader(Path.GetDirectoryName(Path.Combine(RepoRoot, kMoto)));
    ImportGltf.GltfImportResult result = null;
    using (TextReader reader = new StreamReader(Path.Combine(RepoRoot, kMoto))) {
      result = ImportGltf.Import(GltfSchemaVersion.GLTF1, reader, binLoader, PolyImportOptions.Default());
    }
    string prefabPath = "Assets/Poly/TestData/single_p.prefab";
    SaveAsSinglePrefab(result, prefabPath);
    GameObject.DestroyImmediate(result.root);
  }

  private static GameObject DoImport(string gltfPath, PolyImportOptions options, bool savePrefab=false) {
    var text = File.ReadAllText(gltfPath);

    GltfSchemaVersion version;
    if (text.Contains("\"version\": \"1")) {
      version = GltfSchemaVersion.GLTF1;
    } else if (text.Contains("\"version\": \"2")) {
      version = GltfSchemaVersion.GLTF2;
    } else {
      // well, just guess I suppose... it's just test code
      version = GltfSchemaVersion.GLTF1;
    }

    Debug.LogFormat("Import {0} as {1}", gltfPath, version);
    IUriLoader binLoader = new BufferedStreamLoader(Path.GetDirectoryName(gltfPath));
    using (TextReader reader = new StreamReader(gltfPath)) {
      var result = ImportGltf.Import(version, reader, binLoader, options);
      if (savePrefab) {
        SaveAsSinglePrefab(result, Path.ChangeExtension(gltfPath, ".prefab"));
      }
      return result.root;
    }
  }

  [MenuItem("Poly/Dev/Test/Import+save selected .gltf assets")]
  public static void TestImportSelection() {
    var gltfAssets = Selection.objects
        .Select(o => AssetDatabase.GetAssetPath(o))
        .Where(p => !string.IsNullOrEmpty(p))
        .Where(p => p.Contains(".gltf"))
        .ToArray();
    if (gltfAssets.Length == 0) {
      Debug.LogFormat("No selected .gltf assets");
      return;
    }
    foreach (var asset in gltfAssets) {
      DoImport(asset, PolyImportOptions.Default(), true);
    }
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only/glTF1")]
  public static void TestImportGltf1() {
    DoImport(Path.Combine(RepoRoot, kAllBrush14), PolyImportOptions.Default());
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only/glTF2, defaults")]
  public static void TestImportGltf2() {
    DoImport(Path.Combine(RepoRoot, kComputer), PolyImportOptions.Default());
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only/glTF2, scale x2")]
  public static void TestImportGltf2Scale() {
    PolyImportOptions options = new PolyImportOptions();
    options.rescalingMode = PolyImportOptions.RescalingMode.CONVERT;
    options.scaleFactor = 2.0f;
    DoImport(Path.Combine(RepoRoot, kComputer), options);
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only/glTF2, target size=50")]
  public static void TestImportGltf2TargetSize() {
    PolyImportOptions options = new PolyImportOptions();
    options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
    options.desiredSize = 50;
    DoImport(Path.Combine(RepoRoot, kComputer), options);
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only/glTF2, target size=50, recenter")]
  public static void TestImportGltf2TargetSizeRecenter() {
    PolyImportOptions options = new PolyImportOptions();
    options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
    options.recenter = true;
    options.desiredSize = 50;
    DoImport(Path.Combine(RepoRoot, kComputer), options);
  }

  [Test]
  [MenuItem("Poly/Dev/Test/Import only (GLTF2, transparent)")]
  public static void TestImportGltf2Transparent() {
    DoImport(Path.Combine(RepoRoot, kGoblets), PolyImportOptions.Default());
  }

  static void AssertNear(float f1, float f2, float eps=1e-4f) {
    if (Mathf.Abs(f2-f1) >= eps) {
      Assert.Fail("{0} not near {1}", f1, f2);
    }
  }
  static void AssertOrthogonal(Vector3 v1, Vector3 v2, float eps=1e-4f) {
    float dot = Vector3.Dot(v1.normalized, v2.normalized);
    if (Mathf.Abs(dot) > eps) {
      Assert.Fail("{0} not orthogonal to {1}: {2}", v1, v2, dot);
    }
  }
  static Vector3 CalculateAndVerifyNormal(Vector3 v0, Vector3 v1, Vector3 v2) {
    Vector3 vn = PolyToolkitInternal.MathUtils.CalculateNormal(v0, v1, v2);
    AssertNear(vn.magnitude, 1);
    AssertOrthogonal(vn, (v1-v0));
    AssertOrthogonal(vn, (v2-v1));
    AssertOrthogonal(vn, (v0-v2));
    return vn;
  }

  [Test]
  public static void TestMathUtilsCalculateNormal() {
    // Test vs the standard meaning of normal: leg-a cross leg-b
    {
      Vector3 v0 = Vector3.zero;
      Vector3 v1 = new Vector3(1, 0, 0);
      Vector3 v2 = new Vector3(0, 1, 0);
      Vector3 vn = CalculateAndVerifyNormal(v0, v1, v2);
      Assert.IsTrue(vn == new Vector3(0, 0, 1));
    }

    // Check degenerate triangle: two sides parallel.
    {
      Vector3 v0 = Vector3.zero;
      Vector3 v1 = new Vector3(1, 0, 0);
      CalculateAndVerifyNormal(v0, v1, v1 + (v1-v0) * 2);
    }

    // Check degenerate triangle: all vertices coincident
    {
      Vector3 v0 = new Vector3(1, 2, 3);
      CalculateAndVerifyNormal(v0, v0, v0);
    }
  }

  [Test]
  [Ignore("CalculateNormal not robust enough")]
  public static void TestMathUtilsCalculateNormalDifficult() {
    Vector3 v0 = Vector3.zero;
    Vector3 v1 = new Vector3(10000, 100, 200);
    Vector3 v2 = new Vector3(10000, 100.1f, 200);
    CalculateAndVerifyNormal(v0, v1, v2);
  }


}

}
