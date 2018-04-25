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
        /// <summary> location of Aseprite on hard disk </summary>
        public string asepriteExeLoc;
        /// <summary> location of source ASE and JSON files </summary>
        public static string artFolder = "";
        /// <summary> location to output </summary>
        public static string spritesLoc = "";
        /// <summary>  </summary>
        public static string rootSpritesLoc="";

        /// <summary> title for the application to be displayed as window header </summary>
        private static string MAIN_TITLE = "Aseprite to Unity";
        /// <summary> default Location of aseprite.exe </summary>
        private static string DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH = "C:/Program Files (x86)/Aseprite/";
        /// <summary>  </summary>
        private static string DEFAULT_MAC_ASEPRITE_INSTALL_PATH = "";
        /// <summary>  </summary>
        private static string DEFAULT_LINUX_ASEPRITE_INSTALL_PATH = "";

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
        private int index;
        /// <summary> amount of scroll used in data output area </summary>
        private Vector2 scroll;
        /// <summary> Gameobject to attach the animation to </summary>
        private GameObject go;
        /// <summary>  </summary>
        private GameObject referenceObject;
        /// <summary> whether or not to update existing ones or to create new animation clips </summary>
        private bool update = true;
        /// <summary> whether or not to directly attach animation clips to object </summary>
        private ImportType importPreference = ImportType.CreatingNewObject;
        /// <summary> whether or not to also include frame information in text dump </summary>
        private bool showFrameData = true;
        /// <summary> foldout state for Extract related GUI stuff </summary>
        private bool extractFoldout = true;
        /// <summary> foldout state for Import related GUI stuff </summary>
        private bool importFoldout = true;
        /// <summary> foldout state for Help related GUI stuff </summary>
        private bool helpFoldout = false;
        /// <summary> text dump of imported animation data </summary>
        private string text = "hiya :3 i jus 8 a pair it was gud";
        /// <summary> the category of the current sprite that is being updated </summary>
        private SpriteCategory category = SpriteCategory.Character;
        /// <summary> whether or not to store Sprites and Animations into
        /// categorized subfolders. Best used with a large amount of sprites </summary>
        private bool OrganizeAssets = true;
        /// <summary> whether or not to output debug information </summary>
        private bool displayDebugData = false;
        /// <summary> scale at which to create new gameobject. Putting this at 1 means object 
        /// will be native pixel size </summary>
        private float scale = 1;
        /// <summary> pixel size of helpBox icon </summary>
        private int iconSize = 40;
        /// <summary> Master control for window scrolling </summary>
        private Vector2 MasterScrollPosition;
        /// <summary>  </summary>

        #endregion

        /// <summary> How the Animations should be imported </summary>
        public enum ImportType {
            DebuggingOutput, ApplyingToExistingObject, CreatingNewObject
        }


        /// <summary> Different Categories sprites can be imported as;
        /// Used for file organization</summary>
        public enum SpriteCategory {
            Character, Environment, ParticleEffect, Other
        }

        [MenuItem("Tools/Anim Import")]
        private static void AnimImport() {
            EditorWindow.GetWindow(typeof(Anim_Import)).name = "AE to Unity";
        }

        #region ---- INIT ----

        /// <summary>
        /// locates the art folder by assuming it is found in the same directory as the Unity project by the name "Art"
        /// </summary>
        void OnEnable() {
            string s = Application.dataPath;
            string key = s.Contains("/") ? "/" : "\\";
            s = s.Replace(key + "Assets", "");
            artFolder = s.Substring(0, s.LastIndexOf(key)) + key + "Art" + key;
            FindAseFiles();

            DEFAULT_SPRITES_PATH = Application.dataPath + "/Resources/Sprites/";
            rootSpritesLoc = DEFAULT_SPRITES_PATH + "";
            spritesLoc = DEFAULT_SPRITES_PATH + "";

            DetectPlatform();
            asepriteExeLoc = isWindows ? DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH :
                DEFAULT_MAC_ASEPRITE_INSTALL_PATH;
        }

        /// <summary>
        /// update list of ase file locations
        /// </summary>
        private void FindAseFiles() {
            files = FindAseFiles(artFolder).ToArray();

            options = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                options[i] = files[i].Replace(artFolder, "").Replace(".ase", "");
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
            res.AddRange(Directory.GetFiles(artFolder, "*.ase"));
            return res;
        }

        #endregion


        #region ---- GUI ----

        void OnGUI() {
            MasterScrollPosition = GUILayout.BeginScrollView(MasterScrollPosition, GUI.skin.scrollView);

            //UnityEngine.Debug.Log(spritesLoc);

            HeaderGUI();
            HelpGUI();
            ExtractGUI();
            ImportGUI();

            GUILayout.EndScrollView();
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

            GUILayout.Space(20);
        }

        /// <summary>
        /// GUI for telling the user How To use ASE to Unity
        /// </summary>
        void HelpGUI() {
            if (helpFoldout = AseGUILayout.BeginFold(helpFoldout, "How To Use")) {
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
            if (extractFoldout = AseGUILayout.BeginFold(extractFoldout, "Ase Extraction Settings")) {
                GUILayout.BeginHorizontal();
                asepriteExeLoc = EditorGUILayout.TextField("Aseprite.exe Location", asepriteExeLoc,
                    GUILayout.Width(-50), GUILayout.ExpandWidth(true));
                if (GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                    string temp = EditorUtility.OpenFolderPanel("Aseprite Install Location", asepriteExeLoc,
                        //(isWindows ? DEFAULT_WINDOWS_ASEPRITE_INSTALL_PATH : DEFAULT_MAC_ASEPRITE_INSTALL_PATH));
                        "");
                    if (!String.IsNullOrEmpty(temp))
                        asepriteExeLoc = temp;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (File.Exists(asepriteExeLoc + "aseprite.exe")) {
                    GUILayout.BeginHorizontal();
                    string newArtFolder = EditorGUILayout.TextField("Source Folder", artFolder,
                        GUILayout.Width(-50), GUILayout.ExpandWidth(true));
                    if(GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                        string temp = EditorUtility.OpenFolderPanel("Source Folder Location", newArtFolder, "");
                        if (!String.IsNullOrEmpty(temp))
                            newArtFolder = temp;
                        if (!newArtFolder.EndsWith("/")) newArtFolder += "/";
                    }
                    if (!newArtFolder.Equals(artFolder)) {
                        artFolder = newArtFolder;
                        FindAseFiles();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);

                    if (Directory.Exists(artFolder)) {
                        GUILayout.BeginHorizontal();
                        // In textbox, the path to the sprites folder 
                        // should be represented as a local path
                        string shortSprite = rootSpritesLoc.Replace(Application.dataPath + "/", "");
                        shortSprite = EditorGUILayout.TextField("Root Sprites Folder", shortSprite,
                            GUILayout.Width(-50), GUILayout.ExpandWidth(true));
                        rootSpritesLoc = Application.dataPath + "/" + shortSprite;
                        if (GUI.Button(AseGUILayout.GUIRect(30, 18), "...", EditorStyles.miniButtonMid)) {
                            string temp = EditorUtility.OpenFolderPanel("Root Sprites Folder", 
                                rootSpritesLoc, "");
                            if (!String.IsNullOrEmpty(temp))
                                rootSpritesLoc = temp;
                            if (!rootSpritesLoc.EndsWith("/"))
                                rootSpritesLoc += "/";
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);

                        // add subfolder organization
                        spritesLoc = rootSpritesLoc + (OrganizeAssets ? 
                            category.ToString() + "/" : "");
                        // remove subfolder organization if it is unwanted
                        if (!OrganizeAssets && spritesLoc.Equals(rootSpritesLoc + category.ToString() + "/"))
                            spritesLoc = spritesLoc.Substring(0, spritesLoc.LastIndexOf(category.ToString()));

                        // validate root sprite location
                        if (!Directory.Exists(rootSpritesLoc))
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "Root Sprite Folder does not exist.",
                             MessageType.Error);
                        if(!rootSpritesLoc.Contains("Assets/"))
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "Sprites Folder must be located within project Assets!", 
                             MessageType.Error);
                        else if (!rootSpritesLoc.Contains("Resources/"))
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
                            GUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("Aseprite File");
                            index = EditorGUILayout.Popup(index, options);
                            GUILayout.EndHorizontal();

                            AseGUILayout.BeginArea();
                            GUILayout.Space(2);
                            EditorGUILayout.LabelField(files[index], EditorStyles.centeredGreyMiniLabel);
                            GUILayout.Space(2);
                            AseGUILayout.EndArea();

                            // extract data from ase file
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.FlexibleSpace();
                            string btnText = HasExtractedJSON() ? "Update .ase Interpretation" :
                                "Extract .ase Interpretation";
                            if (GUILayout.Button(btnText, 
                                GUILayout.Height(35), GUILayout.MaxWidth(200)))
                                ExtractAse(options[index]);
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                        }
                    } else {
                        //GUI.DrawTexture(AseGUILayout.GUIRect(iconSize, iconSize), DirFileIcon);
                        EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                            "Cannot find specified art folder.", MessageType.Error);
                    }
                } else {
                    EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                        "Could not find aseprite.exe at \"" + asepriteExeLoc + "\".", 
                        MessageType.Error);
                }
            } AseGUILayout.EndFold();
        }

        void ImportGUI() {
            if (Directory.Exists(artFolder) && File.Exists(asepriteExeLoc + "aseprite.exe")) {
                if (importFoldout = AseGUILayout.BeginFold(importFoldout, "Import Aseprite Animations")
                    && files.Count() > 0) {
                    importPreference = (ImportType)EditorGUILayout.EnumPopup("Import By:", importPreference);

                    EditorGUILayout.BeginHorizontal();
                    displayDebugData = EditorGUILayout.Toggle("Display Debug Data", displayDebugData);
                    if (displayDebugData)
                        showFrameData = EditorGUILayout.Toggle("Show Frame Data ", showFrameData);
                    EditorGUILayout.EndHorizontal();

                    if (importPreference != ImportType.DebuggingOutput) {
                        update = EditorGUILayout.Toggle("Update Existing Clips", update);
                        if (update) {
                            EditorGUI.HelpBox(AseGUILayout.GUIRect(0, iconSize),
                             "If the names of the clips don't match the loop names " +
                                "from the .ase file, new clips will be created for mismatches" +
                                "instead.",
                             MessageType.Warning);
                        }
                    }
                    if (importPreference == ImportType.ApplyingToExistingObject) {
                        go = EditorGUILayout.ObjectField("Target Gameobject", go,
                            typeof(GameObject), true) as GameObject;
                    }
                    if (importPreference == ImportType.CreatingNewObject) {
                        scale = EditorGUILayout.FloatField("Automatic Scaling", scale);
                        referenceObject = EditorGUILayout.ObjectField("Reference Game Object",
                            referenceObject, typeof(GameObject), true) as GameObject;
                    } else referenceObject = null;

                    AseGUILayout.BeginArea();
                    OrganizeAssets = EditorGUILayout.BeginToggleGroup("Organize Assets", OrganizeAssets);
                    category = (SpriteCategory)EditorGUILayout.EnumPopup("Import Subfolder:", category);
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
                    if (animDat != null && displayDebugData) {
                        EditorGUILayout.BeginToggleGroup("Anim Data", animDat != null);
                        scroll = EditorGUILayout.BeginScrollView(scroll);
                        text = EditorGUILayout.TextArea(text);
                        EditorGUILayout.EndScrollView();
                    }
                    AseGUILayout.EndArea();
                    #endregion
                }
                AseGUILayout.EndFold();
            }
        }
        #endregion


        #region ---- IMPORT ----
        /// <summary>
        /// imports the sprites of a given animation into each AnimationClip with appropiate frame rates
        /// </summary>
        /// <param name="anim"></param>
        public void ImportAnims() {
            string objName = options[index];

            // if JSON file hasn't been created for ASE file, extract it
            string filename = artFolder + "/" + extractLoc + options[index] + ".json";
            if (!File.Exists(filename)) {
                ExtractAse(files[index]);
            }

            // load file containing values for frame durations
            animDat = AseData.ReadFromJSON(filename);
            if (animDat == null) {
                UnityEngine.Debug.LogError("Error parsing \"" + filename + "\".");
                return;
            } else {
                animDat.name = objName;

                // create sprites if not available
                if (!SpritesExist(objName))
                    ExtractSpriteSheet(files[index]);

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
                                    text += "\n[" + j + "]; " + clip[j] + "\t" + options[index] + "_" + (clip.start + j);
                            } else {
                                text += "\n[" + j + "]; " + clip[j] + "\t" + options[index] + "_" +
                                    (clip.start + j - ((j == clip.Count) ? 1 : 0));
                            }
                    text += "\n================================\n";
                }
            }

            // directly update animation data
            if (importPreference != ImportType.DebuggingOutput) {
                if (importPreference == ImportType.ApplyingToExistingObject && go == null) {
                    UnityEngine.Debug.LogError("GameObject cannot be null!");
                    return;
                }

                string path = spritesLoc.Substring(spritesLoc.IndexOf("Assets/"))
                    .Replace("Assets/", "").Replace("Resources/", "") + objName;
                Sprite[] sprites = Resources.LoadAll<Sprite>(path);
                if (sprites.Length <= 0) {
                    if (!IsAbleToImportAnims())
                        UnityEngine.Debug.LogError("Sprites for \"" + objName + "\" were not found.\n" + path);
                    else
                        UnityEngine.Debug.Log("Created spritesheet for " + objName + " in " + path.Replace(objName, ""));
                } else {
                    if (importPreference == ImportType.CreatingNewObject) {
                        go = new GameObject(objName, typeof(SpriteRenderer), typeof(Animator));
                        go.transform.localScale = scale * Vector3.one;

                        // copy components frrom a reference object
                        // good for adding things like physic objects and AI scripts from a generic model
                        if (referenceObject != null)
                            foreach (Component c in referenceObject.GetComponents<Component>()) {
                                if (!(c is SpriteRenderer) && !(c is Animator)) {
                                    CopyComponent(c, go);
                                }
                            }
                    }

                    Animator anim = go.GetComponent<Animator>();

                    // if animator has no controller, we must set it to something
                    if (anim.runtimeAnimatorController == null) {
                        // if anim controller was created, attach it to the new GameObject
                        string destination = "Assets/Resources/Animations/" + objName + "/";
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
                    if (importPreference == ImportType.CreatingNewObject)
                        go.GetComponent<SpriteRenderer>().sprite = sprites[0];

                    if (update) UpdateClips(objName, sprites);
                    else CreateClips(objName, sprites);

                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>
        /// Updates AnimationClips attached to the GameObject to reflect Sprite Animations from
        /// the respective ase file. Note that if a loop with the name of the AnimationClip does not exist
        /// in the ase file (case insensitive), the animation will not be updated. 
        /// </summary>
        /// <param name="objName"> name of the sprites to add references to </param>
        /// <param name="sprites"> list of all sprites available in the project</param>
        public void UpdateClips(string objName, Sprite[] sprites) {
            // for each clip, adjust frames
            foreach (AnimationClip aC in AnimationUtility.GetAnimationClips(go)) {
                AseData.Clip clip = animDat[aC.name];
                if (clip == null) {
                    CreateClip(objName, clip, sprites);
                    continue;
                }

                aC.frameRate = clip.sampleRate;
                aC.wrapMode = clip.looping ? WrapMode.Loop : WrapMode.Once;
                ObjectReferenceKeyframe[] k = new ObjectReferenceKeyframe[clip.Count + 1];
                Sprite sprite = null;
                for (int j = 0; j <= clip.Count; j++) {
                    if (!clip.dynamicRate) {
                        if (j < clip.Count)
                            sprite = sprites[clip.start + j];
                    } else {
                        sprite = sprites[clip.start + j - ((j == clip.Count) ? 1 : 0)];
                    }

                    k[j] = new ObjectReferenceKeyframe();
                    k[j].time = clip[j] * (clip.l0 / 1000f); //time is in secs? WTF!!!
                    k[j].value = sprite;
                }
                AnimationUtility.SetObjectReferenceCurve(aC, 
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), k);
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
            Sprite sprite = null;
            aC.frameRate = clip.sampleRate;
            ObjectReferenceKeyframe[] k = new ObjectReferenceKeyframe[clip.Count + 1];

            for (int j = 0; j <= clip.Count; j++) {
                if (!clip.dynamicRate) {
                    if (j < clip.Count)
                        sprite = sprites[clip.start + j];
                } else {
                    sprite = sprites[clip.start + j - ((j == clip.Count) ? 1 : 0)];
                }

                k[j] = new ObjectReferenceKeyframe();
                k[j].time = clip[j] * (clip.l0 / 1000f); //time is in secs? WTF!!!
                k[j].value = sprite;
            }

            aC.SetCurve("", typeof(SpriteRenderer), "Sprite", null);
            aC.wrapMode = clip.looping ? WrapMode.Loop : WrapMode.Once;
            AnimationUtility.SetObjectReferenceCurve(aC, EditorCurveBinding.
                PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), k);
            string destination = "Assets/Resources/Animations/" +
                (OrganizeAssets ? category + "/" : "") + objName + "/";
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            AssetDatabase.CreateAsset(aC, destination + clip.name + ".anim");
            
            Animator anim = go.GetComponent<Animator>();
            AnimatorController controller = (AnimatorController)anim.runtimeAnimatorController;
            controller.AddMotion(aC);
            
        }

        /// <summary>
        /// Copies the .ase file into a readable .json at a temp folder in the Art directory 
        /// </summary>
        /// <param name="aseName"></param>
        void ExtractAse(string asePath) {
            if (!isWindows) {
                ExtractAseMac(asePath);
                return;
            }

            string aseJSONLoc = artFolder + extractLoc;
            string aseName = asePath.Substring(asePath.Contains("/") ? asePath.LastIndexOf("/") + 1 : 0)
                .Replace(".ase", "");
            if (!Directory.Exists(aseJSONLoc))
                Directory.CreateDirectory(aseJSONLoc);

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = asepriteExeLoc + "aseprite.exe";
            startInfo.Arguments = "-b --list-tags --ignore-empty --data \"" +
               aseJSONLoc + aseName + ".json\" \"" + asePath;
            process.StartInfo = startInfo;
            process.Start();
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
            if (!isWindows) {
                ExtractSpriteSheetMac(asePath);
                return;
            }

            string aseName = asePath.Substring(asePath.Contains("/") ? asePath.LastIndexOf("/") + 1 : 0)
                .Replace(".ase", "");

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = asepriteExeLoc + "aseprite.exe";
            startInfo.Arguments = "-b \"" + asePath + "\" --sheet \"" + spritesLoc + aseName + ".png\" --sheet-pack";
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
        #endregion

        /// <summary>
        /// determine whether or not spritesheet has yet been created, and if
        /// it is possible to use the sprites
        /// </summary>
        /// <returns></returns>
        bool IsAbleToImportAnims() {
            if (options.Count() == 0) return false;
            string aseName = options[index];
            return File.Exists(spritesLoc + aseName + ".png");
        }

        bool HasExtractedJSON() {
            if (options.Count() == 0) return false;
            string aseName = options[index];
            return File.Exists(artFolder + extractLoc + aseName + ".json");
        }

        static T CopyComponent<T>(T original, GameObject destination) where T : Component {
            Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields) {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
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

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetLocalPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

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
        private static string helpText = "Aseprite to Unity allows you to save time importing those awesome pixel art animations your artists have spent hours creating!\n\n" +
            "It's actually pretty simple to use. First, make sure your artists have followed standard practices for creating their animations. This includes:\n" +
                "\t- saving all animations loops for the same object in .ase file\n" +
                "\t- animation loops have names that accurately reflect their purpose in-game\n" +
                "\t- all.ase files can be found under a single folder\n" +
                "\t- hiding layers that should not be visible in the final sprite\n" +
                "\t- (optional) setting loop types in Aseprite for animations that do not loop\n" +
            "\nOnce that has been verified, now you must set the extraction settings in Unity.\n\n" +
                "\t1) Aseprite.exe - In order to run this tool, you must have Aseprite installed on the local machine. This field is set to the default installation location acording to your machine, but if you've installed it in a custom folder you must browse to it.\n" +
                "\t2) Art Source Folder - This is where all the.ase files are stored. ASE to Unity will also create.JSON representations that will be used to fully read all animation data, as it is unable to directly parse the.ase file itself. \n" +
                "\t3) Sprites Folder - This is where you want store all your exported sprites. Because the tool also uses Unity's Resource manager, this folder must be located within a folder called \"Resources\". This folder can be anywhere in your projcet, and the Sprites folder need not be a direct child of it.\n" +
                "\t4) Aseprite file - This is the file you want to import! If you have set the Art Source Folder to one that contains.ase files, this will list ALL of the.ase files found, INCLUDING FILES THAT MIGHT SHARE THE SAME NAME. Please make sure you have selected the correct file!\n" +
            
            "\nNext, it is time to import the animations.This can only be done if the proper extraction settings have been set. Now, you might notice that you have 3 options for how to import your file.\n" +
                "\tDebugging Output - This doesn't import any animation data, but it will output information it was able to read from the JSON\n" +
                "\tApplying Directly to Object - This allows you to update an object that currently exists within the scene with the newly created animation data\n" +
                "\tCreating New GameObject - This will create a new GameObject with the necessary components into the scene.It will have the same name as the selected .ase file. You can also copy components from a reference GameObject into the newly created object.\n" +
            "\nImporting animations for the first time will create animation clips AND animation controllers specific to the.ase file. All animations will be saved under \"Resources/Animations/[ase file name]/\", for improved organization and Resource Management.If a .controller file already exists for the.ase you want to import but is not in the previously mentioned folder, a new one will be created and attached to the GameObject.\n" +
            "\nOrganization";
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