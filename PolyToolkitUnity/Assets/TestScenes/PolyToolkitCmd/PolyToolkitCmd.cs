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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PolyToolkit;
using PolyToolkitInternal;  // NO_POLY_TOOLKIT_INTERNAL_CHECK
using System.Reflection;
using System.Text;

namespace PolyToolkitDev {

/// <summary>
/// Poly Toolkit "command line"-style test app.
/// Useful for testing API calls at runtime, etc.
/// </summary>
public class PolyToolkitCmd : MonoBehaviour {
  // These credentials are from a test project. Not the Poly Toolkit official project.
  private string API_KEY = "AIzaSyDfWJ9E2Vgee4hHnyNB6zV3WRf5L5unfZs";
  private string CLIENT_ID = "110645446910-8duhhimg4ups5k45vsri2de8ndlnni86.apps.googleusercontent.com";
  private string CLIENT_SECRET = "n0ctCDTVn4xxkws9_BKgnle7";

  private const float ROTATION_SENSIVITY = 180.0f;

  // NOTE: indentation of the help texts is a bit awkward here because of the raw @" syntax.
  // This is a debug file, so practicity is more important than aesthetics here.

  private string HELP_TEXT=@"
  COMMANDS (type 'help <cmd>' for command specific help):
    auth - Manage auth state.
    help - Print help text.
    list - Sends a 'List assets' request.
    listmy - Sends a 'List my assets' request.
    show - Shows the assets we got on the last request.
    thumb - Load thumbnail for a given asset.
    imp - Import an asset into scene.
    clear - Clear scene (remove imported object)";
  private Dictionary<string, string> COMMAND_HELP = new Dictionary<string, string> {
    { "auth", @"
    auth signin
      Signs in.
    auth signin -n
      Signs in, non-interactive mode.
    auth signin --at <access_token> --rt <refresh_token>
      Signs in using the given tokens instead of the normal auth flow.
    auth cancel
      Cancels authentication (while signing in).
    auth signout
      Signs out.
    auth status
      Prints auth status." },

    { "list", @"
    Lists assets.

    list featured [<options>]
      Lists featured assets.
    list latest [<options>]
      Lists latest assets.
    list [<options>]
      List assets (custom request). Use options below.

    Options:
      -c category
        Filter by category, (example: animals).
      -k
        Return only curated assets.
      -s
        Specify keyword to search for (example: tree).
      -o
        Specifies order (example: BEST).
      -p
        Page size (example: 50).
      -f
        Format filter (example: BLOCKS, GLTF, GLTF_2, TILT).
      --pt
        Page token.
      --maxc
        Specifies maximum complexity (example: MEDIUM).
      --dry
        Only show the request that would be sent, don't send it." },

    { "listmy", @"
    Lists my (the signed-in user's) assets.
    Requires authentication.

    listmy newest [<options>]
       Lists my newest assets.
    listmy [<options>]
       Lists my assets (custom request). Use options below.
    
    Options:
      -o
        Specifies order (example: BEST).
      -p
        Page size (example: 50).
      -f
        Format filter (example: BLOCKS, GLTF, GLTF_2, TILT).
      -v <visibility>
        Visibility (PRIVATE, UNLISTED, PUBLISHED).
      --pt
        Page token.
      --dry
        Only show the request that would be sent, don't send it." },

    { "imp", @"
    imp <index> [<options>]
      Imports an asset. <index> is the index of the asset in
      the list results, as shown by the 'show' command.

      Options:
      -m: rescaling mode
        (CONVERT_UNITS, SPECIFIC_SIZE, SPECIFIC_FACTOR).
      --ds <size>: the desired size,
        (only relevant if -m SPECIFIC_SIZE is used).
      --sf <factor>: the scale factor
        (only relevant if if -m SPECIFIC_FACTOR is used).
      --no-center: do not recenter import" },

    { "thumb", @"
    thumb <index>
      Loads and displays the thumbnail for the given
      asset (<index> is the index of the asset in the list
      results, as shown by the 'show' command." },

    { "show", @"
    show [<index>]
      Shows the assets we got in the last list request.
      If an index is given, shows details on the particular
      asset index." },

  };

  private InputField inputField;
  private Text outputText;
  private bool isTransparent = true;

  private Queue<string> messagesPendingDisplay = new Queue<string>();
  private object messagesPendingDisplayLock = new byte[1];

  private List<PolyAsset> currentResults = new List<PolyAsset>();

  private bool dragging;
  private Vector3 dragAnchor;
  private Quaternion rotationOnDragStart;

  // The asset we're currently displaying.
  private GameObject currentAsset = null;
  
  private Image userProfileImage = null;
  private Image imageDisplay = null;

  private bool hasRunListCommand = false;

  private void Start() {
    inputField = ComponentById<InputField>("ID_Input");
    outputText = ComponentById<Text>("ID_Output");
    outputText.text = "Poly Toolkit v" + PtSettings.Version.ToString() + "\n" +
      "Type 'help' for a list of commands.\n\n";

    ComponentById<Button>("ID_ClearButton").onClick.AddListener(() => { outputText.text = ""; });
    ComponentById<Button>("ID_RunButton").onClick.AddListener(SubmitCurrentCommand);
    ComponentById<Button>("ID_CopyButton").onClick.AddListener(() => {
      GUIUtility.systemCopyBuffer = outputText.text;
    });
    ComponentById<Button>("ID_ToggleTransparencyButton").onClick.AddListener(ToggleTransparency);
    userProfileImage = ComponentById<Image>("ID_UserProfileImage");
    userProfileImage.gameObject.SetActive(false);
    imageDisplay = ComponentById<Image>("ID_ImageDisplay");
    imageDisplay.gameObject.SetActive(false);

    // Subscribe to Unity log messages so we can show them.
    Application.logMessageReceivedThreaded += HandleLogThreaded;

    // Initialize Poly. We do this explicit initialization rather than have a PolyToolkitManager on the
    // scene because we want to use a specific API key that's not the one that's configured in PtSettings
    // (since we want to keep PtSettings uninitialized for the user to fill in).
    PolyApi.Init(new PolyAuthConfig(API_KEY, CLIENT_ID, CLIENT_SECRET));
  }

  private void OnDisable() {
    Application.logMessageReceivedThreaded -= HandleLogThreaded;
  }
  
  private static T ComponentById<T>(string id) where T : Component {
    GameObject obj = GameObject.Find(id);
    if (obj == null) throw new Exception("Can't find object with ID " + id);
    T comp = obj.GetComponent<T>();
    if (comp == null) throw new Exception(string.Format("Object {0} has no component {1}", id, typeof(T).Name));
    return comp;
  }

  private void Update() {
    if (Input.GetKeyDown(KeyCode.Return)) {
      SubmitCurrentCommand();
    }
    // Print any pending messages.
    lock (messagesPendingDisplayLock) {
      while (messagesPendingDisplay.Count > 0) {
        PrintLn(messagesPendingDisplay.Dequeue());
      }
    }

    if (dragging) {
      if (!Input.GetMouseButton(0)) {
        dragging = false;
      } else {
        UpdateDragging();
      }
    } else if (Input.GetMouseButton(0)) {
      StartDragging();
    }
  }

  private void StartDragging() {
    if (null == currentAsset) return;
    dragging = true;
    rotationOnDragStart = currentAsset.transform.rotation;
    dragAnchor = Input.mousePosition;
  }

  private void UpdateDragging() {
    Vector3 mouseDelta = Input.mousePosition - dragAnchor;
    float normDeltaX = -mouseDelta.x / Screen.width;
    float normDeltaY = mouseDelta.y / Screen.height;
    
    currentAsset.transform.rotation = rotationOnDragStart;
    currentAsset.transform.Rotate(normDeltaY * ROTATION_SENSIVITY,
        normDeltaX * ROTATION_SENSIVITY, 0, Space.World);
  }

  private void SubmitCurrentCommand() {
    outputText.text = "";
    imageDisplay.sprite = null;
    imageDisplay.gameObject.SetActive(false);
    RunCommand(inputField.text);
    inputField.text = "";
    inputField.Select();
    inputField.ActivateInputField();
  }

  private void ToggleTransparency() {
    isTransparent = !isTransparent;
    ComponentById<Image>("ID_ScrollView").color = new Color(0.0f, 0.0f, 0.0f,
        isTransparent ? 0.0f : 1.0f);
  }

  private void PrintLn(string text = "", params object[] args) {
    if (args.Length > 0) text = string.Format(text, args);
    outputText.text += text + "\n";
    Debug.Log("[Output] " + text);
  }

  private void RunCommand(string command) {
    command = command.Trim();
    if (command.Length < 1) return;
    
    // Find the verb (first word).
    string verb;
    string[] args;
    int firstSpace = command.IndexOf(' ');
    if (firstSpace > 0) {
      verb = command.Substring(0, firstSpace);
      args = command.Substring(firstSpace + 1).Split(' ');
    } else {
      verb = command;
      args = new string[0];
    }

    // "foo --help" is a synonym for "help foo"
    if (args.Length == 1 && args[0] == "--help") {
      CmdHelp(new string[] { verb });
      return;
    }

    // Look for a method called Cmd<verb> in this class.
    MethodInfo methodInfo = GetType().GetMethod("Cmd" + verb,
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
    if (methodInfo == null) {
      PrintLn("Unrecognized command. Type 'help' for a list of commands.");
      return;
    }
    try {
      methodInfo.Invoke(this, new object[] { args });
    } catch (Exception ex) {
      Debug.LogError("Error executing command '" + command + "':\n" + ex);
    }
  }

  /// <summary>
  /// Called by Unity when a log message prints.
  /// Can be called on any thread!
  /// </summary>
  private void HandleLogThreaded(string logString, string stackTrace, LogType logType) {
    if (logType == LogType.Log) return;  // Don't log regular messages (they are spammy).
    lock (messagesPendingDisplayLock) {
      bool isWarn = logType == LogType.Warning;
      messagesPendingDisplay.Enqueue(string.Format("{0}: {1}{2}",
        isWarn ? "!!!" : "***", logString,
        isWarn && !string.IsNullOrEmpty(stackTrace) ? "" : ("\n" + stackTrace)));
    }
  }

  private void CmdHelp(string[] args) {
    if (args.Length >= 1) {
      PrintCommandHelp(args[0]);
    } else {
      PrintLn(HELP_TEXT);
    }
  }

  private void PrintCommandHelp(string command) {
    // Command-specific help.
    command = command.ToLowerInvariant();
    string commandHelp;
    if (COMMAND_HELP.TryGetValue(command, out commandHelp)) {
      PrintLn(commandHelp);
    } else {
      PrintLn("No further documentation for command: " + command);
    }
  }

  private static bool HasOpt(string[] args, string option) {
    foreach (string arg in args) {
      if (arg == option) return true;
    }
    return false;
  }

  private static string GetOpt(string[] args, string option, string defaultValue = null) {
    for (int i = 0; i < args.Length - 1; i++) {
      if (args[i] == option) return args[i + 1];
    }
    return defaultValue;
  }

  private static T GetEnumOpt<T>(string[] args, string option, T defaultValue) {
    string stringVal = GetOpt(args, option, null);
    return stringVal != null ? ParseEnum<T>(stringVal) : defaultValue;
  }

  private static int GetIntOpt(string[] args, string option, int defaultValue) {
    string stringVal = GetOpt(args, option, null);
    if (stringVal == null) return defaultValue;
    int result;
    if (!int.TryParse(stringVal, out result)) {
      throw new Exception(string.Format("Value of option '{0}' must be an integer, was '{1}'", option, stringVal));
    }
    return result;
  }

  private static float GetFloatOpt(string[] args, string option, float defaultValue) {
    string stringVal = GetOpt(args, option, null);
    if (stringVal == null) return defaultValue;
    float result;
    if (!float.TryParse(stringVal, out result)) {
      throw new Exception(string.Format("Value of option '{0}' must be a float, was '{1}'", option, stringVal));
    }
    return result;
  }

  private void AuthCallback(PolyStatus status) {
    if (status.ok) {
      PrintLn("Signed in successfully as {0}.", PolyApi.UserName);
      userProfileImage.sprite = PolyApi.UserIcon;
      userProfileImage.gameObject.SetActive(true);
    } else {
      PrintLn("Sign in FAILED: {0}", status);
    }
  }

  private void CmdAuth(string[] args) {
    if (args.Length < 1) {
      PrintCommandHelp("auth");
      return;
    }
    switch (args[0]) {
      case "signin":
        bool interactive = !HasOpt(args, "-n");
        string manualAccessToken = GetOpt(args, "--at", null);
        string manualRefreshToken = GetOpt(args, "--rt", null);

        // Both --at and --rt should be present, or neither.
        if ((manualAccessToken == null) ^ (manualRefreshToken == null)) {
          // One is present, the other isn't: error.
          PrintLn("*** The --at and --rt options must be both present, or both absent.");
          return;
        }

        if (manualAccessToken != null) {
          PrintLn("Attempting auth with given tokens...");
          PolyApi.Authenticate(manualAccessToken, manualRefreshToken, AuthCallback);
        } else {
          PrintLn("Attempting {0} auth...", interactive ? "interactive" : "non-interactive");
          PolyApi.Authenticate(interactive, AuthCallback);
        }
        break;
      case "signout":
        PolyApi.SignOut();
        userProfileImage.sprite = null;
        userProfileImage.gameObject.SetActive(false);
        PrintLn("Requested sign out.");
        break;
      case "cancel":
        PrintLn("Requesting to cancel auth.");  
        PolyApi.CancelAuthentication();
        break;
      case "status":
        PrintLn("authenticated: {0}\nauthenticating: {1}\naccess token: {2}\nrefresh token: {3}",
            PolyApi.IsAuthenticated, PolyApi.IsAuthenticating, PolyApi.AccessToken, PolyApi.RefreshToken);
        break;
      default:
        PrintCommandHelp("auth");
        break;
    }
  }

  private void CmdList(string[] args) {
    PolyListAssetsRequest req;
    
    if (args.Length > 0 && args[0] == "featured") {
      // Default list request (featured).
      req = PolyListAssetsRequest.Featured();
    } else if (args.Length > 0 && args[0] == "latest") {
      // Default list request (latest).
      req = PolyListAssetsRequest.Latest();
    } else {
      // Custom list request.
      req = new PolyListAssetsRequest();
    }
    // Mutate the request according to parameters.
    req.category = GetEnumOpt(args, "-c", req.category);
    req.curated = HasOpt(args, "-k") ? true : req.curated;
    req.keywords = GetOpt(args, "-s", req.keywords);
    req.maxComplexity = GetEnumOpt(args, "--maxc", req.maxComplexity);
    req.orderBy = GetEnumOpt(args, "-o", req.orderBy);
    req.pageSize = GetIntOpt(args, "-p", req.pageSize);
    req.pageToken = GetOpt(args, "--pt", req.pageToken);
    // FormatFilter is weird because it's nullable, so we have to test before trying to parse:
    if (HasOpt(args, "-f")) {
      req.formatFilter = GetEnumOpt(args, "-f", PolyFormatFilter.BLOCKS /* not used */);
    }

    if (HasOpt(args, "--dry")) {
      // Dry run mode.
      PrintLn(req.ToString());
      return;
    }

    // Send the request.
    hasRunListCommand = true;
    PrintLn("Sending list request... Please wait.");
    PolyApi.ListAssets(req, (PolyStatusOr<PolyListAssetsResult> res) => {
      if (!res.Ok) {
        PrintLn("Request ERROR: " + res.Status);
        return;
      }
      currentResults = res.Value.assets;
      CmdShow(new string[] {});
    });
  }

  private void CmdListMy(string[] args) {
    PolyListUserAssetsRequest req;
    
    if (args.Length > 0 && args[0] == "newest") {
      // Default list request (newest).
      req = PolyListUserAssetsRequest.MyNewest();
    } else {
      // Custom list request.
      req = new PolyListUserAssetsRequest();
    }
    // Mutate the request according to parameters.
    req.visibility = GetEnumOpt(args, "-v", req.visibility);
    req.orderBy = GetEnumOpt(args, "-o", req.orderBy);
    req.pageSize = GetIntOpt(args, "-p", req.pageSize);
    req.pageToken = GetOpt(args, "--pt", req.pageToken);
    // FormatFilter is weird because it's nullable, so we have to test before trying to parse:
    if (HasOpt(args, "-f")) {
      req.formatFilter = GetEnumOpt(args, "-f", PolyFormatFilter.BLOCKS /* not used */);
    }

    if (HasOpt(args, "--dry")) {
      // Dry run mode.
      PrintLn(req.ToString());
      return;
    }

    // Send the request.
    hasRunListCommand = true;
    PrintLn("Sending listmy request... Please wait.");
    PolyApi.ListUserAssets(req, (PolyStatusOr<PolyListAssetsResult> res) => {
      if (!res.Ok) {
        PrintLn("Request ERROR: " + res.Status);
        return;
      }
      PrintLn("Request SUCCESS.");
      currentResults = res.Value.assets;
      CmdShow(new string[] {});
    });
  }

  private void CmdShow(string[] args) {
    int index = -1;
    if (args.Length > 0 && int.TryParse(args[0], out index)) {
      ShowAssetDetails(index);
      return;
    }

    PrintLn("Results: {0} assets.", currentResults.Count);

    // If the user hasn't run a list command, there's a good reason why there are no
    // results to show, so give them a friendly hint.
    if (currentResults.Count == 0) {
      if (!hasRunListCommand) {
        PrintLn();
        PrintLn("This list is sad and empty because you haven't");
        PrintLn("run a list command yet. For a happier listing, use the");
        PrintLn("'list' or 'listmy' command to make a list request.");
      } else {
        PrintLn("(Try a different list command).");
      }
      return;
    }

    PrintLn("To import a result, use the 'imp' command.");
    PrintLn("To show this list again, use the 'show' command.");
    PrintLn();
    for (int i = 0; i < currentResults.Count; i++) {
      PolyAsset asset = currentResults[i];
      PrintLn("[{0}]: {1}\n  ID: {2}\n  Author: {3}\n  Formats: {4}\n",
        i, asset.displayName, asset.name, asset.authorName,
        FormatListToString(asset.formats));
    }
  }

  private void ShowAssetDetails(int index) {
    if (index < 0 || index >= currentResults.Count) {
      PrintLn("*** Invalid asset index.");
      return;
    }
    PolyAsset asset = currentResults[index];
    PrintLn(asset.ToString());
  }

  private void CmdImp(string[] args) {
    int index;
    if (args.Length == 0 || !int.TryParse(args[0], out index)) {
      PrintLn("*** Specify the index of the asset to import (as given by the 'show' command).");
      return;
    }
    if (index < 0 || index >= currentResults.Count) {
      PrintLn("*** Invalid index. Use the 'show' command to see the valid assets.");
      return;
    }
    PolyAsset assetToImport = currentResults[index];
    PolyImportOptions options = new PolyImportOptions();
    options.rescalingMode = GetEnumOpt(args, "-m", PolyImportOptions.RescalingMode.FIT);
    options.recenter = !HasOpt(args, "--no-center");
    options.desiredSize = GetFloatOpt(args, "--ds", 5.0f);
    options.scaleFactor = GetFloatOpt(args, "--sf", 1.0f);
    PrintLn("Importing asset... May take a while. Please wait!");
    PolyApi.Import(assetToImport, options, (PolyAsset asset, PolyStatusOr<PolyImportResult> result) => {
      if (!result.Ok) {
        PrintLn("ERROR: failed to import {0}: {1}", asset.name, result.Status);
        return;
      }
      PrintLn("Successfully imported asset '{0}' ({1})", asset.name, asset.displayName);

      if (currentAsset != null) {
        Destroy(currentAsset);
        currentAsset = null;
      }

      currentAsset = result.Value.gameObject;
    });
  }

  private void CmdClear(string[] args) {
    if (currentAsset != null) {
      Destroy(currentAsset);
      currentAsset = null;
    }
    outputText.text = "";
    imageDisplay.sprite = null;
    imageDisplay.gameObject.SetActive(false);
  }

  private void CmdThumb(string[] args) {
    int index;
    if (args.Length == 0 || !int.TryParse(args[0], out index) || index < 0 || index >= currentResults.Count) {
      PrintLn("*** Invalid index. Please specify the index of the asset whose thumbnail " + 
        "you wish to load (use the 'show' command to show the results).");
      return;
    }
    PolyAsset assetToUse = currentResults[index];
    PrintLn("Fetching thumbnail... Please wait.");
    PolyApi.FetchThumbnail(assetToUse, (PolyAsset asset, PolyStatus status) => {
      if (status.ok) {
        PrintLn("Successfully fetched thumbnail for asset '{0}'", asset.name);
        imageDisplay.sprite = Sprite.Create(asset.thumbnailTexture,
          new Rect(0, 0, asset.thumbnailTexture.width, asset.thumbnailTexture.height),
          Vector2.zero);
        imageDisplay.gameObject.SetActive(true);
      } else {
        PrintLn("*** Error loading thumbnail for asset '{0}': {1}", asset.name, status);
      }
    });
  }

  private static string FormatListToString(List<PolyFormat> list) {
    if (list == null) return "(null)";
    if (list.Count == 0) return "(empty)";
    StringBuilder sb = new StringBuilder(list[0].formatType.ToString());
    for (int i = 1; i < list.Count; i++) {
      sb.Append(", ").Append(list[i].formatType.ToString());
    }
    return sb.ToString();
  }

  private static T ParseEnumOpt<T>(string[] args, string opt) {
    return ParseEnum<T>(GetOpt(args, opt));
  }

  private static T ParseEnum<T>(string input) {
    try {
      return (T) Enum.Parse(typeof(T), input, ignoreCase: true);
    } catch (ArgumentException) {
      throw new Exception(string.Format("'{0}' is not a valid value for enum {1}", input, typeof(T).Name));
    }
  }
}

}