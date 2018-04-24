using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using LitJson;
using UnityEditor.Animations;
using System.Reflection;

namespace ASE_to_Unity {
    public class Anim_Import : EditorWindow {


        public AseData animDat;
        [Tooltip("Location of Aseprite on hard disk")]
        /// <summary> location of Aseprite on hard disk </summary>
        public string asepriteLoc = DEFAULT_ASEPITE_INSTALL_PATH;
        [Tooltip("Location of art stuff")]
        public static string artFolder = "";
        [Tooltip("Location of Sprites")]
        public static string spritesLoc = "";

        private static string DEFAULT_ASEPITE_INSTALL_PATH = "C:/Program Files (x86)/Aseprite/";

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
        /// <summary> text dump of imported animation data </summary>
        private string text = "hiya :3 i jus 8 a pair it was gud";
        /// <summary>  </summary>
        private ObjectType objType = ObjectType.Character;
        /// <summary>  </summary>
        private bool OrganizeAssets = true;
        private bool displayDebugData = false;
        /// <summary>  </summary>
        private float scale = 1;

        private int iconSize = 26;

        private Vector2 MasterScrollPosition;

        public enum ImportType {
            DebuggingOutput, ApplyingToExistingObject, CreatingNewObject
        }

        public enum ObjectType {
            Character, Environment, ParticleEffect, Other
        }

        [MenuItem("Tools/Anim Import")]
        private static void AnimImport() {
            EditorWindow.GetWindow(typeof(Anim_Import)).name = "AE to Unity";
        }

        /// <summary>
        /// locates the art folder by assuming it is found in the same directory as the Unity project by the name "Art"
        /// </summary>
        void OnEnable() {
            string s = Application.dataPath;
            string key = s.Contains("/") ? "/" : "\\";
            s = s.Replace(key + "Assets", "");
            artFolder = s.Substring(0, s.LastIndexOf(key)) + key + "Art" + key;

            spritesLoc = Application.dataPath + "/Resources/Sprites/";

            DetectPlatform();
        }

        void OnGUI() {
            MasterScrollPosition = GUILayout.BeginScrollView(MasterScrollPosition, GUI.skin.scrollView);

            HeaderGUI();
            ExtractGUI();
            ImportGUI();

            GUILayout.EndScrollView();
        }

        void HeaderGUI() {

        }

        void ExtractGUI() {
            if (extractFoldout = AseFoldout.BeginFold(extractFoldout, "Ase Extraction Settings")) {
                
            }
        }

        void ImportGUI() {
            if (Directory.Exists(artFolder) && File.Exists(asepriteLoc + "aseprite.exe")) {
                if (importFoldout = AseFoldout.BeginFold(importFoldout, "Import Aseprite Animations")) {
                    #region import
                    importPreference = (ImportType)EditorGUILayout.EnumPopup("Import By:", importPreference);

                    EditorGUILayout.BeginHorizontal();
                    displayDebugData = EditorGUILayout.Toggle("Display Debug Data", displayDebugData);
                    if (displayDebugData)
                        showFrameData = EditorGUILayout.Toggle("Show Frame Data ", showFrameData);
                    EditorGUILayout.EndHorizontal();

                    if (importPreference != ImportType.DebuggingOutput) {
                        update = EditorGUILayout.Toggle("Update Existing Clips", update);
                        if (update) {
                            EditorGUILayout.LabelField("If the names of the clips don't match the loop names", EditorStyles.centeredGreyMiniLabel);
                            EditorGUILayout.LabelField("from the .ase file, a new one will be created instead", EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                    if (importPreference == ImportType.ApplyingToExistingObject) {
                        go = EditorGUILayout.ObjectField("Gameobject", go,
                            typeof(GameObject), true) as GameObject;
                    }
                    if (importPreference == ImportType.CreatingNewObject) {
                        scale = EditorGUILayout.FloatField("Automatic Scaling", scale);
                        referenceObject = EditorGUILayout.ObjectField("Reference Game Object",
                            referenceObject, typeof(GameObject), true) as GameObject;
                    } else referenceObject = null;

                    AseArea.Begin();
                    //EditorGUILayout.BeginHorizontal();
                    OrganizeAssets = EditorGUILayout.BeginToggleGroup("Organize Assets", OrganizeAssets);
                    objType = (ObjectType)EditorGUILayout.EnumPopup("Import Subfolder:", objType);
                    EditorGUILayout.EndToggleGroup();
                    //EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    string btnText = (IsAbleToImportAnims()) ? "Import Animation Data" :
                        "Create Sprite Sheet";

                    if (GUILayout.Button(btnText, GUILayout.Height(35), GUILayout.MaxWidth(200)))
                        ImportAnims();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    #endregion

                    #region dat display
                    if (animDat != null && displayDebugData) {
                        EditorGUILayout.BeginToggleGroup("Anim Data", animDat != null);
                        scroll = EditorGUILayout.BeginScrollView(scroll);
                        text = EditorGUILayout.TextArea(text);
                        EditorGUILayout.EndScrollView();
                    }
                    AseArea.End();
                    #endregion
                }
                AseFoldout.EndFold();
            }
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
        /// recursively search Art Folder for ASE files
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
                (OrganizeAssets ? objType + "/" : "") + objName + "/";
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
            startInfo.FileName = asepriteLoc + "aseprite.exe";
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
        /// determine whether or not spritesheet has yet been created, and if
        /// it is possible to use the sprites
        /// </summary>
        /// <returns></returns>
        bool IsAbleToImportAnims() {
            string aseName = options[index];
            return File.Exists(spritesLoc + aseName + ".png");
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
            startInfo.FileName = asepriteLoc + "aseprite.exe";
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