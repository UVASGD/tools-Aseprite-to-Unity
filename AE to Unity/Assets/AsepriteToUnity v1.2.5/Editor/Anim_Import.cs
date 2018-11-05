using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using UnityEditor.Animations;
using System.Reflection;

namespace ASE_to_Unity {
    public class Anim_Import : EditorWindow {

        #region ---- FIELDS ----

        /// <summary>  </summary>
        public AseData animDat;
        /// <summary> location to output </summary>
        public static string spritesLoc = "";

        /// <summary> title for the application to be displayed as window header </summary>
        private const string MAIN_TITLE = "Aseprite to Unity";
        /// <summary> default Location of aseprite.exe </summary>
        private const string DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH = "C:/Program Files (x86)/Aseprite/";
        private const string DEFAULT_MAC_ASEPRITE_INSTALL_PATH = "";
        private const string DEFAULT_LINUX_ASEPRITE_INSTALL_PATH = "";

        #region REGISTRY_REFERENCES
        /// in order to prevent these values from being rest upon recompilation of the project, 
        /// their values remain outside of Unity and must be accessed via the EditorPrefs system

        private const string REG_NAME = "ASE_To_Unity.";
        /// <summary> location of source ASE and JSON files </summary>
        public const string artFolder = REG_NAME + "artFolder";
        /// <summary> location of Aseprite on hard disk </summary>
        public const string asepriteExeLoc = REG_NAME + "asepriteExeLoc";
        public const string rootSpritesLoc = REG_NAME + "rootSpritesLoc";
        /// <summary> foldout state for Extract related GUI stuff </summary>
        private const string extractFoldout = REG_NAME + "extractFoldout";
        /// <summary> foldout state for Import related GUI stuff </summary>
        private const string importFoldout = REG_NAME + "importFoldout";
        /// <summary> foldout state for Help related GUI stuff </summary>
        private const string helpFoldout = REG_NAME + "helpFoldout";
        /// <summary> foldout state for Settings related GUI stuff </summary>
        private const string settingsFoldout = REG_NAME + "setingsFoldout";
        /// <summary> foldout state for Animation Controller related GUI stuff </summary>
        private const string experimentalFoldout = REG_NAME + "experimentalFoldout";
        /// <summary> foldout state for Animation Controller related GUI stuff </summary>
        private const string experimentalEnabled = REG_NAME + "experimentalEnabled";
        /// <summary> whether or not to create triggeres based off the name of the
        /// animation clip </summary>
        private const string createTriggers = REG_NAME + "createTriggers";
        /// <summary> try to assume the most logical connections between the most logical of animations </summary>
        private const string connectLogicalClips = REG_NAME + "connectLogicalClips";
        /// <summary>  </summary>
        private const string outputToConsole = REG_NAME + "outputToConsole";
        /// <summary>  </summary>
        private const string displayDebugData = REG_NAME + "displayDebugData";
        private const string autoFindSprites = REG_NAME + "autoFindSprites";
        private const string index = REG_NAME + "fileIndex";
        /// <summary> the category of the current sprite that is being updated </summary>
        private const string category = REG_NAME + "category";
        /// <summary> custom category name set by user </summary>
        private const string customCategory = REG_NAME + "customCategory";
        /// <summary> whether or not to directly attach animation clips to object </summary>
        private const string importPreference = REG_NAME + "importPreference";
        private const string isAnimation = REG_NAME + "isAnimation";

        #endregion

        private string DEFAULT_SPRITES_PATH = "";

        /// <summary> whether or not current system is windows or Unix based </summary>
        private bool isWindows = true;
        /// <summary> location to save extracted json files </summary>
        private string extractLoc = "JSON/";
        /// <summary> aseprite options to display in popup menu </summary>
        private string[] options;
        /// <summary> found aseprite files in art folder </summary>
        private string[] files;
        /// <summary> index of currently selected ase file in options/files </summary>
        // private int index;
        /// <summary> amount of scroll used in data output area </summary>
        private Vector2 scroll;
        /// <summary> Gameobject to attach the animation to </summary>
        private GameObject go;
        private Texture2D spritesheet;
        /// <summary>  </summary>
        private GameObject referenceObject;
        /// <summary> whether or not to update existing ones or to create new animation clips </summary>
        private bool update = true;
        /// <summary> whether or not to also include frame information in text dump </summary>
        private bool showFrameData = true;
        private string text = "hiya :3 i jus 8 a pair it was gud";
        /// <summary> whether or not to store Sprites and Animations into
        /// categorized subfolders. Best used with a large amount of sprites </summary>
        private bool OrganizeAssets = true;
        /// <summary> scale at which to create new gameobject. Putting this at 1 means object 
        /// will be native pixel size </summary>
        private float scale = 1;
        /// <summary> pixel size of helpBox icon </summary>
        private int iconSize = 40;
        /// <summary> Master control for window scrolling </summary>
        private Vector2 MasterScrollPosition;
        private float OptionPopupSize;
        /// <summary>  </summary>

        /// <summary> How the Animations should be imported </summary>
        public enum ImportType {
            DebuggingOutput = 0,
            ApplyingToExistingObject,
            CreatingNewObject
        }


        /// <summary> Different Categories sprites can be imported as;
        /// Used for file organization</summary>
        public enum SpriteCategory {
            Character,
            Environment,
            ParticleEffect,
            Other
        }
        #endregion

        #region ---- INIT ----

        [MenuItem("Tools/Ase to Unity")]
        private static void OpenFromMenu() {
            EditorWindow window = EditorWindow.GetWindow(typeof(Anim_Import));
            window.name = "AE to Unity";
            window.minSize = new Vector2(400, 100);
            window.titleContent = new GUIContent(window.name);
        }

        /// <summary>
        /// initialize variables of first time activation
        /// </summary>
        void OnAwake() {
            if (String.IsNullOrEmpty(EditorPrefs.GetString(extractFoldout))) EditorPrefs.SetBool(extractFoldout, true);
            if (String.IsNullOrEmpty(EditorPrefs.GetString(importFoldout))) EditorPrefs.SetBool(importFoldout, true);
            if (String.IsNullOrEmpty(EditorPrefs.GetString(experimentalFoldout))) EditorPrefs.SetBool(experimentalFoldout, true);
            if (String.IsNullOrEmpty(EditorPrefs.GetString(autoFindSprites))) EditorPrefs.SetBool(autoFindSprites, true);
            if (String.IsNullOrEmpty(EditorPrefs.GetString(isAnimation))) EditorPrefs.SetBool(isAnimation, true);
        }

        /// <summary>
        /// Initializes EditorPrefs Data to ensure maximum stability
        /// </summary>
        void OnEnable() {   
            // locates the art folder by assuming it is found in the same directory as the Unity project by the name "Art"
            if (String.IsNullOrEmpty(EditorPrefs.GetString(artFolder)) || files == null) {
                string s = Application.dataPath;
                string key = s.Contains("/") ? "/" : "\\";
                s = s.Replace(key + "Assets", "");
                EditorPrefs.SetString(artFolder, s.Substring(0, s.LastIndexOf(key)) + key + "Art" + key);
                FindAseFiles();
            }

            DEFAULT_SPRITES_PATH = Application.dataPath + "/Resources/Sprites/";
            if (String.IsNullOrEmpty(EditorPrefs.GetString(rootSpritesLoc))){
                EditorPrefs.SetString(rootSpritesLoc, DEFAULT_SPRITES_PATH + "");
                spritesLoc = EditorPrefs.GetString(rootSpritesLoc);
            }

            if (String.IsNullOrEmpty(EditorPrefs.GetString(asepriteExeLoc))) {
                DetectPlatform(); 
                EditorPrefs.SetString(asepriteExeLoc, isWindows ? DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH :
                    DEFAULT_MAC_ASEPRITE_INSTALL_PATH);
            }

            if (OptionPopupSize == 0)
                FindMaxOptionPopupSize();

            if (String.IsNullOrEmpty(EditorPrefs.GetString(category))) {
                EditorPrefs.SetString(category, SpriteCategory.Character.ToString());
                EditorPrefs.SetString(importPreference, ImportType.CreatingNewObject.ToString());
            }
        }

        /// <summary>
        /// update list of ase file locations
        /// </summary>
        private void FindAseFiles() {
            files = FindAseFiles(EditorPrefs.GetString(artFolder)).ToArray();

            options = new string[files.Length];
            for (int i = 0; i < files.Length; i++) {
                options[i] = files[i].Replace(EditorPrefs.GetString(artFolder), "").Replace(".ase", "");
            }
            FindMaxOptionPopupSize();
        }

        private void FindMaxOptionPopupSize() {
            if (options.Count() == 0) {
                OptionPopupSize = 50;
                return;
            }

            string max = "";
            foreach (string s in options) 
                max = s.Length > max.Length ? s : max;
            try {
                OptionPopupSize = EditorStyles.label.CalcSize(new GUIContent(max)).x / 4f;
            } catch {
                OptionPopupSize = 50;
            }
        }

        /// <summary>
        /// recursively search Source Folder for ASE files
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private List<String> FindAseFiles(string dir) {
            List<String> res = new List<String>();
            foreach (string d in Directory.GetDirectories(dir)) {
                res.AddRange(FindAseFiles(d));
            }
            
            res.AddRange(Directory.GetFiles(dir, "*.ase"));
            return res;
        }

        private ImportType ImportPref {
            get {
                return (ImportType)Enum.Parse(typeof(ImportType),
                    EditorPrefs.GetString(importPreference));
            }
        }

        private string Category {
            get {
                string cat = EditorPrefs.GetString(category);
                if (cat.Equals(SpriteCategory.Other.ToString()))
                    return EditorPrefs.GetString(customCategory);
                return cat;
            }
        }

        #endregion


        #region ---- GUI ----

        void OnGUI() {
            MasterScrollPosition = GUILayout.BeginScrollView(MasterScrollPosition, GUI.skin.scrollView);

            HeaderGUI();
            HelpGUI();
            ExtractGUI();
            if (EditorPrefs.GetBool(experimentalEnabled))
                AnimControlFormatGUI();
            ImportGUI();
            SettingsGUI();

            GUILayout.EndScrollView();
            //GUIUtility.ExitGUI();
        }

        /// <summary>
        /// GUI for our beautiful header :)
        /// </summary>
        void HeaderGUI() {
            GUILayout.Space(16);

            GUIStyle style = new GUIStyle() {
                alignment = TextAnchor.LowerCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            style.richText = true;
            Rect rect = AseGUILayout.GUIRect(0, 18);

            GUIStyle shadowStyle = new GUIStyle(style) {
                richText = false
            };

            EditorGUI.DropShadowLabel(rect, MAIN_TITLE, shadowStyle);
            GUI.Label(rect, MAIN_TITLE, style);

            GUILayout.Space(15);
        }

        void SettingsGUI() {
            EditorPrefs.SetBool(settingsFoldout, AseGUILayout.BeginFold(EditorPrefs.GetBool(settingsFoldout), "Program Settings"));
            if (EditorPrefs.GetBool(settingsFoldout)) {
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Attempt to find sprites automatically", GUILayout.ExpandWidth(true));
                EditorPrefs.SetBool(autoFindSprites, EditorGUILayout.Toggle("",
                    EditorPrefs.GetBool(autoFindSprites)));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorPrefs.SetBool(displayDebugData, EditorGUILayout.Toggle("Display Debug Data",
                    EditorPrefs.GetBool(displayDebugData)));
                if (EditorPrefs.GetBool(displayDebugData))
                    showFrameData = EditorGUILayout.Toggle("Show Frame Data", showFrameData);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Enable Experimental Functions", GUILayout.ExpandWidth(true));
                EditorPrefs.SetBool(experimentalEnabled, EditorGUILayout.Toggle("",
                    EditorPrefs.GetBool(experimentalEnabled)));
                EditorGUILayout.EndHorizontal();
                if (EditorPrefs.GetBool(experimentalEnabled))
                    EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                     "These functions are experimental for a reason. Use them at your own risk.",
                     MessageType.Info);

            } AseGUILayout.EndFold();
        }

        /// <summary>
        /// GUI for telling the user How To use ASE to Unity
        /// </summary>
        void HelpGUI() {
            EditorPrefs.SetBool(helpFoldout, AseGUILayout.BeginFold(EditorPrefs.GetBool(helpFoldout), "How To Use"));
            if (EditorPrefs.GetBool(helpFoldout)) {
                GUIStyle style = new GUIStyle (GUI.skin.textArea);
                style.normal.background = null;
                style.active.background = null;
                style.onHover.background = null;
                style.hover.background = null;
                style.onFocused.background = null;
                style.focused.background = null;

                EditorGUILayout.TextArea(helpText, style);
            }
            AseGUILayout.EndFold();
        }

        /// <summary>
        /// GUI for Extraction Settings Foldout. Manages control paths
        /// </summary>
        void ExtractGUI() {
            EditorPrefs.SetBool(extractFoldout, AseGUILayout.BeginFold(
                EditorPrefs.GetBool(extractFoldout), "Ase Extraction Settings"));
            if (EditorPrefs.GetBool(extractFoldout)) {
                    GUILayout.BeginHorizontal();
                EditorPrefs.SetString(asepriteExeLoc, EditorGUILayout.TextField("Aseprite.exe Location", 
                    EditorPrefs.GetString(asepriteExeLoc), GUILayout.Width(-50), GUILayout.ExpandWidth(true)));
                if (GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                    string temp = EditorUtility.OpenFolderPanel("Aseprite Install Location", EditorPrefs.GetString(asepriteExeLoc),
                        //(isWindows ? DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH : DEFAULT_MAC_ASEPRITE_INSTALL_PATH));
                        "");
                    if (!String.IsNullOrEmpty(temp)){
						if(!temp.EndsWith("/")) temp += "/";
                        EditorPrefs.SetString(asepriteExeLoc, temp);
					}
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (File.Exists(EditorPrefs.GetString(asepriteExeLoc) + "aseprite.exe")) {
                    GUILayout.BeginHorizontal();
                    string newArtFolder = EditorGUILayout.TextField("Source Folder", EditorPrefs.GetString(artFolder),
                        GUILayout.Width(-50), GUILayout.ExpandWidth(true));
                    if(GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                        string temp = EditorUtility.OpenFolderPanel("Source Folder Location", newArtFolder, "");
                        if (!String.IsNullOrEmpty(temp))
                            newArtFolder = temp;
                        if (!newArtFolder.EndsWith("/")) newArtFolder += "/";
                    }
                    if (!newArtFolder.Equals(EditorPrefs.GetString(artFolder))) {
                        EditorPrefs.SetString(artFolder, newArtFolder);
                        FindAseFiles();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    if (Directory.Exists(EditorPrefs.GetString(artFolder))) {
                        GUILayout.BeginHorizontal();
                        // In textbox, the path to the sprites folder 
                        // should be represented as a local path
                        string shortSprite = EditorPrefs.GetString(rootSpritesLoc)
                            .Replace(Application.dataPath + "/", "");
                        shortSprite = EditorGUILayout.TextField("Root Sprites Folder", shortSprite,
                            GUILayout.Width(-50), GUILayout.ExpandWidth(true));
                        EditorPrefs.SetString(rootSpritesLoc, Application.dataPath + "/" + shortSprite);
                        if (GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                            string temp = EditorUtility.OpenFolderPanel("Root Sprites Folder", 
                                EditorPrefs.GetString(rootSpritesLoc), "");
                            if (!String.IsNullOrEmpty(temp))
                                EditorPrefs.SetString(rootSpritesLoc, temp);
                            if (!EditorPrefs.GetString(rootSpritesLoc).EndsWith("/"))
                                EditorPrefs.SetString(rootSpritesLoc, 
                                    EditorPrefs.GetString(rootSpritesLoc) + "/");
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);

                        // add subfolder organization
                        spritesLoc = EditorPrefs.GetString(rootSpritesLoc) + (OrganizeAssets ? 
                            Category + "/" : "");
                        // remove subfolder organization if it is unwanted
                        if (!OrganizeAssets && spritesLoc.Equals(EditorPrefs.GetString(rootSpritesLoc) +
                            Category + "/"))
                            spritesLoc = spritesLoc.Substring(0, spritesLoc.LastIndexOf(Category));

                        // validate root sprite location
                        if (!Directory.Exists(EditorPrefs.GetString(rootSpritesLoc)))
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "Root Sprite Folder does not exist.",
                             MessageType.Error);
                        if(!EditorPrefs.GetString(rootSpritesLoc).Contains("Assets/"))
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "Sprites Folder must be located within project Assets!", 
                             MessageType.Error);
                        else if (!EditorPrefs.GetString(rootSpritesLoc).Contains("Resources/"))
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "This program uses Unity's Resource Mangaer, so the " +
                             "Sprites Folder must be subfolder in Resources",
                             MessageType.Error);

                        GUILayout.Space(20);

                        if (files.Count() == 0) {
                                EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                                 "No .ase files found in Source Folder.",
                                 MessageType.Info);
                        } else {
                            if (EditorPrefs.GetInt(index) > files.Length)
                                EditorPrefs.SetInt(index, files.Length - 1);

                            EditorPrefs.SetBool(isAnimation, EditorGUILayout.Toggle("Is animated file",
                                EditorPrefs.GetBool(isAnimation)));

                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Aseprite File");
                            EditorPrefs.SetInt(index, EditorGUILayout.Popup(EditorPrefs.GetInt(index),
                                options, GUILayout.MinWidth(OptionPopupSize)));
                            GUILayout.EndHorizontal();

                            AseGUILayout.BeginArea();
                            GUILayout.Space(2);
                            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                            style.wordWrap = true;
                            EditorGUILayout.LabelField(files[EditorPrefs.GetInt(index)]
                                .Replace("\\","/"), style);
                            GUILayout.Space(2);
                            AseGUILayout.EndArea();

                            // extract data from ase file
                            EditorGUI.BeginDisabledGroup(!EditorPrefs.GetBool(isAnimation));
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            string btnText = HasExtractedJSON() ? "Update .ase Interpretation" :
                                "Extract .ase Interpretation";
                            if (GUILayout.Button(btnText, 
                                GUILayout.Height(35), GUILayout.MaxWidth(200)))
                                ExtractAse(files[EditorPrefs.GetInt(index)]);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            EditorGUI.EndDisabledGroup();
                        }
                    } else {
                        //GUI.DrawTexture(AseGUILayout.GUIRect(iconSize, iconSize), DirFileIcon);
                        EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                            "Cannot find specified art folder.", MessageType.Error);
                    }
                } else {
                    EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                        "Could not find aseprite.exe at \"" + EditorPrefs.GetString(asepriteExeLoc) + "\".", 
                        MessageType.Error);
                }
            } AseGUILayout.EndFold();
        }

        void ImportGUI() {
            if (Directory.Exists(EditorPrefs.GetString(artFolder)) && File.Exists(EditorPrefs.GetString(asepriteExeLoc) + "aseprite.exe")) {
                EditorPrefs.SetBool(importFoldout, AseGUILayout.BeginFold(
                    EditorPrefs.GetBool(importFoldout), EditorPrefs.GetBool(isAnimation) ?
                    "Import Aseprite Animations" : "Import Aseprite Spritesheet"));
                if (EditorPrefs.GetBool(importFoldout) && files.Count() > 0) {

                    if (EditorPrefs.GetBool(isAnimation)) {
                        EditorPrefs.SetString(importPreference, EditorGUILayout.EnumPopup("Import By:",
                             (ImportType)Enum.Parse(typeof(ImportType),
                             EditorPrefs.GetString(importPreference))).ToString());

                        if (ImportPref != ImportType.DebuggingOutput) {
                            update = EditorGUILayout.Toggle("Update Existing Clips", update);
                            if (update) {
                                EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                                 "If the names of the clips don't match the loop names " +
                                    "from the .ase file, new clips will be created for mismatches " +
                                    "instead.",
                                 MessageType.Info);
                            }
                        }
                        if (ImportPref == ImportType.ApplyingToExistingObject) {
                            go = EditorGUILayout.ObjectField("Target Gameobject", go,
                                typeof(GameObject), true) as GameObject;
                        }
                        if (ImportPref == ImportType.CreatingNewObject) {
                            scale = EditorGUILayout.FloatField("Automatic Scaling", scale);
                            referenceObject = EditorGUILayout.ObjectField("Reference Game Object",
                                referenceObject, typeof(GameObject), true) as GameObject;
                        } else referenceObject = null;

                        if (!EditorPrefs.GetBool(autoFindSprites)) {
                            spritesheet = (Texture2D)EditorGUILayout.ObjectField("Source Spritesheet", spritesheet, typeof(Texture2D), false);

                            if (spritesheet == null) {
                                EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                                 "Cannot import any animations without a spritesheet!",
                                 MessageType.Warning);
                            }
                        }
                    }

                    AseGUILayout.BeginArea();
                    OrganizeAssets = EditorGUILayout.BeginToggleGroup("Organize Assets", OrganizeAssets);
                    EditorPrefs.SetString(category, EditorGUILayout.EnumPopup("Import Subfolder:", 
                        (SpriteCategory)Enum.Parse(typeof(SpriteCategory), 
                        EditorPrefs.GetString(category))).ToString());
                    if (EditorPrefs.GetString(category).Equals(SpriteCategory.Other.ToString())){
                        EditorPrefs.SetString(customCategory, EditorGUILayout.TextField("Custom Category:",
                            EditorPrefs.GetString(customCategory)));
                    }
                    EditorGUILayout.EndToggleGroup();
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    string btnText = (IsAbleToImportAnims()) ? "Import Animation Data" :
                        "Create Sprite Sheet";

                    if (GUILayout.Button(btnText, GUILayout.Height(35), GUILayout.MaxWidth(200)))
                        ImportAnims();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    #region dat display
                    if (animDat != null && EditorPrefs.GetBool(displayDebugData)) {
                        EditorGUILayout.BeginToggleGroup("Anim Data", animDat != null);
                        scroll = EditorGUILayout.BeginScrollView(scroll);
                        text = EditorGUILayout.TextArea(text);
                        EditorGUILayout.EndScrollView();
                        EditorGUILayout.EndToggleGroup();
                    }
                    AseGUILayout.EndArea();
                    #endregion
                }
                AseGUILayout.EndFold();
            }
        }

        void AnimControlFormatGUI() {
            EditorPrefs.SetBool(experimentalFoldout, AseGUILayout.BeginFold(
                EditorPrefs.GetBool(experimentalFoldout), "~~~~Exprerimental Stuff~~~~"));
            if (EditorPrefs.GetBool(experimentalFoldout)) {
                EditorPrefs.SetBool(createTriggers, EditorGUILayout.Toggle("Automatically Create Animator Parameters",
                    EditorPrefs.GetBool(createTriggers)));
                EditorPrefs.SetBool(connectLogicalClips, EditorGUILayout.Toggle("Logically Connect Animation Clips",
                    EditorPrefs.GetBool(connectLogicalClips)));

                if(EditorPrefs.GetBool(connectLogicalClips) &&
                    !EditorPrefs.GetBool(createTriggers)) {
                    EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                        "Attaching animation clips without known parameters has not yet been implemented.",
                        MessageType.Warning);
                }


                //EditorGUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                //RuntimeAnimatorController rAC = null;
                //bool disabled;
                //if (!(disabled = (go == null))) 
                //    if (!(disabled = (go.GetComponent<Animator>()==null))) 
                //        disabled = ((rAC = go.GetComponent<Animator>().runtimeAnimatorController) == null);
                //EditorGUI.BeginDisabledGroup(disabled);
                //if (GUILayout.Button("Check Wrapmode", GUILayout.Height(35), GUILayout.MaxWidth(200))) {
                //    foreach(AnimationClip aC in rAC.animationClips) {
                //        UnityEngine.Debug.Log(aC.name + ": " + aC.wrapMode.ToString());
                //    }
                //}
                //EditorGUI.EndDisabledGroup();
                //GUILayout.FlexibleSpace();
                //EditorGUILayout.EndHorizontal();
            }  AseGUILayout.EndFold();
        }


        #endregion


        #region ---- IMPORT ----
        /// <summary>
        /// imports the sprites of a given animation into each AnimationClip with appropiate frame rates
        /// </summary>
        /// <param name="anim"></param>
        public void ImportAnims() {
            string objName = AseUtils.StripPath(options[EditorPrefs.GetInt(index)]);

            // if JSON file hasn't been created for ASE file, extract it
            string filename = EditorPrefs.GetString(artFolder) + "/" + extractLoc + objName + ".json";
            if (!File.Exists(filename)) {
                ExtractAse(files[EditorPrefs.GetInt(index)]);
            }

            // load file containing values for frame durations
            animDat = AseData.ReadFromJSON(filename);
            if (animDat == null) {
                UnityEngine.Debug.LogError("Error parsing \"" + filename + "\".");
                return;
            } else {
                animDat.name = objName;

                if (EditorPrefs.GetBool(autoFindSprites)) {
                // create sprites if not available
                    if (!SpritesExist(objName))
                        ExtractSpriteSheet(files[EditorPrefs.GetInt(index)]);
                } else {
                    // cannot operate without a spritesheet
                    if (spritesheet == null) return;
                }

                // update debug text area
                text = "index: sample\tSprite Name\n\n";
                foreach (AseData.Clip clip in animDat.clips) {
                    text += "Clip Name: " + clip.name;
                    //text += "\ndynmaic rate: " + clip.dynamicRate;
                    text += "\nsample rate: " + clip.sampleRate;
                    if (showFrameData)
                        for (int j = 0; j <= clip.Count; j++)
                            if (!clip.dynamicRate) {
                                if (j < clip.Count)
                                    text += "\n[" + j + "]; " + clip[j] + "\t" + objName + "_" + (clip.start + j);
                            } else {
                                text += "\n[" + j + "]; " + clip[j] + "\t" + objName + "_" +
                                    (clip.start + j - ((j == clip.Count) ? 1 : 0));
                            }
                    text += "\n================================\n";
                }
            }

            // directly update animation data
            if (EditorPrefs.GetBool(isAnimation)) {
                if (ImportPref != ImportType.DebuggingOutput) {
                    if (ImportPref == ImportType.ApplyingToExistingObject && go == null) {
                        if (EditorPrefs.GetBool("outputToConsole"))
                            UnityEngine.Debug.LogError("GameObject cannot be null!");
                        return;
                    }

                    string path = EditorPrefs.GetBool(autoFindSprites) ?
                        spritesLoc + objName : AssetDatabase.GetAssetPath(spritesheet)
                        .Substring(0, AssetDatabase.GetAssetPath(spritesheet).IndexOf("."));
                    path = path.Substring(path.IndexOf("Assets/"))
                        .Replace("Assets/", "").Replace("Resources/", "");
                    Sprite[] sprites = Resources.LoadAll<Sprite>(path);
                    if (sprites.Length <= 0) {
                        if (!IsAbleToImportAnims()) {
                            if (EditorPrefs.GetBool("outputToConsole"))
                                UnityEngine.Debug.LogError("Sprites for \"" + objName + "\" were not found.\n" + path);
                        } else
                            if (EditorPrefs.GetBool("outputToConsole"))
                            UnityEngine.Debug.Log("Created spritesheet for " + objName + " in " + path.Replace(objName, ""));
                    } else {
                        if (ImportPref == ImportType.CreatingNewObject) {
                            go = new GameObject(objName, typeof(SpriteRenderer), typeof(Animator));
                            go.transform.localScale = scale * Vector3.one;

                            // copy components frrom a reference object
                            // good for adding things like physic objects and AI scripts from a generic model
                            if (referenceObject != null)
                                foreach (Component c in referenceObject.GetComponents<Component>()) {
                                    if (!(c is SpriteRenderer) && !(c is Animator)) {
                                        AseUtils.CopyComponent(c, go);
                                    }
                                }
                        }

                        Animator anim = go.GetComponent<Animator>();

                        // if animator has no controller, we must set it to something
                        if (anim.runtimeAnimatorController == null) {
                            // if anim controller was created, attach it to the new GameObject
                            string destination = "Assets/Resources/Animations/" +
                                (OrganizeAssets ? Category + "/" : "") + objName + "/";
                            if (Directory.Exists(destination + objName + ".controller")) {
                                anim.runtimeAnimatorController = Resources.Load(destination.Replace("Assets/Resources/", ""))
                                    as RuntimeAnimatorController;
                            } else {
                                if (!Directory.Exists(destination))
                                    Directory.CreateDirectory(destination);
                                anim.runtimeAnimatorController = AnimatorController
                                    .CreateAnimatorControllerAtPath(destination + objName + ".controller");
                            }
                        }

                        // set object's default image
                        if (ImportPref == ImportType.CreatingNewObject)
                            go.GetComponent<SpriteRenderer>().sprite = sprites[0];

                        if (update) UpdateClips(objName, sprites);
                        else CreateClips(objName, sprites);

                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }

        private AnimationClip HasClip(AnimationClip[] clips, String name) {
            foreach(AnimationClip aC in clips) {
                if (aC.name.Equals(name)) return aC;
            }
            return null;
        }

        /// <summary>
        /// Updates AnimationClips attached to the GameObject to reflect Sprite Animations from
        /// the respective ase file. Note that if a loop with the name of the AnimationClip does not exist
        /// in the ase file (case insensitive), the animation will not be updated. 
        /// </summary>
        /// <param name="objName"> name of the sprites to add references to </param>
        /// <param name="sprites"> list of all sprites available in the project</param>
        public void UpdateClips(string objName, Sprite[] sprites) {
            AnimationClip aC;
            AnimationClip[] clips = AnimationUtility.GetAnimationClips(go);

            // for each clip, adjust frames
            // if none exists with same name, create it
            foreach (AseData.Clip clip in animDat.clips) {
                if ((aC = HasClip(clips, clip.name)) != null) {
                    UnityEngine.Debug.Log("Updating");
                    AnimationUtility.SetObjectReferenceCurve(aC,
                        EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), 
                        GetObjectReferences(aC, clip, sprites));
                } else {
                    CreateClip(objName, clip, sprites);
                }
            }
        }

        /// <summary>
        /// create all AnimationClips defined in ASE file 
        /// </summary>
        /// <param name="objName"> name of the sprites to add references to </param>
        /// <param name="sprites"> list of all sprites available in the project</param>
        public void CreateClips(string objName, Sprite[] sprites) {
            foreach (AseData.Clip clip in animDat.clips)
                CreateClip(objName, clip, sprites);
        }

        /// <summary>
        /// Create a single Clip and attach it to GameObject go
        /// </summary>
        /// <param name="objName"></param>
        /// <param name="clip"></param>
        /// <param name="sprites"></param>
        void CreateClip(string objName, AseData.Clip clip, Sprite[] sprites) {
            AnimationClip aC = new AnimationClip();
            aC.frameRate = clip.sampleRate;
            ObjectReferenceKeyframe[] k = GetObjectReferences(aC, clip, sprites);

            aC.SetCurve("", typeof(SpriteRenderer), "Sprite", null);
            aC.wrapMode = clip.looping ? WrapMode.Loop : WrapMode.Once;
            AnimationUtility.SetObjectReferenceCurve(aC, EditorCurveBinding.
                PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), k);
            string destination = "Assets/Resources/Animations/" +
                (OrganizeAssets ? Category + "/" : "") + objName + "/";
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            AssetDatabase.CreateAsset(aC, destination + clip.name + ".anim");
            
            Animator anim = go.GetComponent<Animator>();
            AnimatorController controller = (AnimatorController)anim.runtimeAnimatorController;
            controller.AddMotion(aC);
            
        }

        /// <summary>
        /// get a list of references to the sprites of an animation 
        /// at locations according to its frame speed data
        /// </summary>
        /// <param name="aC"></param>
        /// <param name="clip"></param>
        /// <param name="sprites"></param>
        /// <returns></returns>
        private ObjectReferenceKeyframe[] GetObjectReferences(AnimationClip aC, AseData.Clip clip, Sprite[] sprites) {
            aC.frameRate = clip.sampleRate;
            aC.wrapMode = clip.looping ? WrapMode.Loop : WrapMode.Once;

            SerializedObject sC = new SerializedObject(aC);
            SerializedProperty clipSettings = sC.FindProperty("m_AnimationClipSettings");
            clipSettings.FindPropertyRelative("m_LoopTime").boolValue = clip.looping;
            sC.ApplyModifiedProperties();

            ObjectReferenceKeyframe[] k = new ObjectReferenceKeyframe[clip.Count + (!clip.dynamicRate ? 0 : 1)];
            Sprite sprite = null;
            for (int j = 0; j <= clip.Count; j++) {
                if (!clip.dynamicRate) {
                    if (j < clip.Count) {
                        int i = clip.start + j;
                        sprite = sprites[i];
                    }
                } else {
                    sprite = sprites[clip.start + j - ((j == clip.Count) ? 1 : 0)];
                }

                if (j < k.Length) {
                    k[j] = new ObjectReferenceKeyframe();
                    k[j].time = clip[j] * (clip.l0 / 1000f); //time is in secs? WTF!!!
                    k[j].value = sprite;
                }
            }
            return k;
        }
        
        /// <summary>
        /// Copies the .ase file into a readable .json at a temp folder in the Art directory 
        /// </summary>
        /// <param name="aseName"></param>
        void ExtractAse(string asePath) {
            asePath = asePath.Replace("\\", "/");
            if (!isWindows) {
                ExtractAseMac(asePath);
                return;
            }

            string aseJSONLoc = EditorPrefs.GetString(artFolder) + extractLoc;
            string aseName = asePath.Substring(asePath.Contains("/") ? asePath.LastIndexOf("/") + 1 : 0)
                .Replace(".ase", "");
            if (!Directory.Exists(aseJSONLoc))
                Directory.CreateDirectory(aseJSONLoc);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = EditorPrefs.GetString(asepriteExeLoc) + "aseprite.exe";
            startInfo.Arguments = "-b --list-tags --ignore-empty --data \"" +
               aseJSONLoc + aseName + ".json\" \"" + asePath;
            process.StartInfo = startInfo;
            process.Start();

            while (!process.HasExited) { }
        }

        /// <summary>
        /// Determine if a spritesheet has been extracted for the ase file
        /// </summary>
        /// <param name="aseName"></param>
        /// <returns></returns>
        bool SpritesExist(string aseName) {
            if (!Directory.Exists(spritesLoc))
                Directory.CreateDirectory(spritesLoc);

            // NOT Reliable!!
            int size = Resources.LoadAll<Sprite>(spritesLoc.Substring(spritesLoc.IndexOf("Assets/"))
                .Replace("Assets/", "")
                .Replace("Resources/", "") + aseName).Count();
            return size != 0;
        }

        /// <summary>
        /// Extract an optimed spritesheet from the ASE file
        /// </summary>
        /// <param name="asePath"></param>
        void ExtractSpriteSheet(string asePath) {
            asePath = asePath.Replace("\\", "/");
            if (!isWindows) {
                ExtractSpriteSheetMac(asePath);
                return;
            }

            string aseName = asePath.Substring(asePath.Contains("/") ? asePath.LastIndexOf("/") + 1 : 0)
                .Replace(".ase", "");

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = EditorPrefs.GetString(asepriteExeLoc) + "aseprite.exe";
            startInfo.Arguments = "-b \"" + asePath + "\" --sheet \"" + spritesLoc + aseName + ".png\"" + (
                EditorPrefs.GetBool(isAnimation) ? " --sheet-pack" : "");
            process.StartInfo = startInfo;
            process.Start();

            // wait until spritesheet has been processed by Aseprite
            while (!process.HasExited) { }

            AssetDatabase.Refresh();
            ApplyTextureImportSettings(spritesLoc + aseName + ".png", animDat);
        }

        private void ExtractAseMac(string asePath) {

        }

        private void ExtractSpriteSheetMac(string asePath) {

        }
        
        /// <summary>
        /// Ensures that the newly created spritesheet is imported into the asset database with the proper settings
        /// Because 90% of sprites from aseprite will be in pixel art format, the interpolation and compressions are
        /// applied to best suit pixel art.
        /// </summary>
        /// <param name="spritePath"></param>
        /// <param name="ase"></param>
        private void ApplyTextureImportSettings(string spritePath, AseData ase) {
            string assetLocalPath = spritePath.Substring(spritePath.IndexOf("Assets/"));
            bool isAnim = EditorPrefs.GetBool(isAnimation);

            if (EditorPrefs.GetBool("outputToConsole"))
                UnityEngine.Debug.Log("ALP: " + assetLocalPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetLocalPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = isAnim ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            if (isAnim) {
                Texture2D texture = new Texture2D(1, 1);
                byte[] fileData = File.ReadAllBytes(spritePath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                Vector2 dim = ase.dim;

                int colCount = texture.width / (int)dim.x;
                int rowCount = texture.height / (int)dim.y;

                List<SpriteMetaData> metas = new List<SpriteMetaData>();

                int i = 0;
                for (int r = 0; r < rowCount; ++r) {
                    for (int c = 0; c < colCount; ++c) {
                        if (i >= animDat.FrameCount) break;
                        SpriteMetaData meta = new SpriteMetaData();
                        meta.rect = new Rect(c * dim.x, texture.height - (r + 1) * dim.y, dim.x, dim.y);
                        meta.name = ase.name + "_" + i;
                        metas.Add(meta);
                        i++;
                    }
                }

                importer.spritesheet = metas.ToArray();
                AssetDatabase.Refresh();
            }
        }
        #endregion


        #region ---- UTILS ----


        /// <summary>
        /// determine whether or not spritesheet has yet been created, and if
        /// it is possible to use the sprites
        /// </summary>
        /// <returns></returns>
        bool IsAbleToImportAnims() {
            if(!EditorPrefs.GetBool(isAnimation) || options.Count() == 0) return false;
            string aseName = AseUtils.StripPath(options[EditorPrefs.GetInt(index)]);
            return File.Exists(spritesLoc + aseName + ".png");
        }

        bool HasExtractedJSON() {
            if (options.Count() == 0) return false;
            string aseName = AseUtils.StripPath(options[EditorPrefs.GetInt(index)]);
            return File.Exists(EditorPrefs.GetString(artFolder) + extractLoc + aseName + ".json");
        }
        
        /// <summary>
        /// determine if system is being run on a Mac, Windows, or Linux
        /// </summary>
        private void DetectPlatform() {
            var platform = (int)Environment.OSVersion.Platform;
            isWindows = !((platform == 4) || (platform == 6) || (platform == 128));
        }

        #region math
        /// <summary>
        /// determines the max from a subset of a list of integers
        /// </summary>
        /// <param name="f"></param>
        /// <param name="i0"></param>
        /// <returns></returns>
        public static int sum(int[] f, int i0) {
            int sum = 0;
            for (int i = 0; i < i0; i++) sum += f[i];
            return sum;
        }

        /// <summary>
        /// Determines the Greatest Common Denominator from a list of integers
        /// </summary>
        /// <param name="numbers"></param>
        /// <returns></returns>
        public static int GCD(int[] numbers) { return numbers.Aggregate(GCD); }
        public static int GCD(int a, int b) { return b == 0 ? a : GCD(b, a % b); }
        #endregion

        /// <summary>
        /// the text to be displayed in the help window
        /// </summary>
        private static string helpText = "Visit https://github.com/UVASGD/tools-Aseprite-to-Unity/blob/master/Readme.md " +
            "for information on how to use this! It even includes PICTURES!";

        #endregion
    }

    #region Extra Classes
    public static class LinqHelper {
        /// <summary>
        /// splits an array into a subset
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"> the original array</param>
        /// <param name="index"> the start of the subset </param>
        /// <param name="length"> the length of the subset </param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length) {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }



    #endregion

}
