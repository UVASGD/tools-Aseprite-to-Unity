using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ASE_to_Unity {

    /// <summary>
    /// class that serializes window data to prevent data loss after compiltion
    /// </summary>
    public class SaveData : MonoBehaviour {

        /// <summary> location of Aseprite on hard disk </summary>
        [Tooltip("location of Aseprite on hard disk")]
        public string asepriteExeLoc;

        /// <summary> location of source ASE and JSON files </summary>
        [Tooltip("location of source ASE and JSON files")]
        public static string artFolder = "";

        /// <summary> location in project to output </summary>
        [Tooltip("location to read/write Spritesheet for selected .ase file. MUST be part of a Resources folder tree")]
        public static string spritesLoc = "";

        /// <summary> location in project to find all Sprite data </summary>
        public static string rootSpritesLoc = "";

        /// <summary> whether or not to directly attach animation clips to object </summary>
        [Tooltip("whether or not to directly attach animation clips to object")]
        private Anim_Import.ImportType importPreference = Anim_Import.ImportType.CreatingNewObject;

        /// <summary> whether or not to also include frame information in text dump </summary>
        [Tooltip("whether or not to also include frame information in text dump")]
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
        [Tooltip("the category of the current sprite that is being updated")]
        private Anim_Import.SpriteCategory category = Anim_Import.SpriteCategory.Character;
        /// <summary> whether or not to store Sprites and Animations into
        /// categorized subfolders. Best used with a large amount of sprites </summary>
        [Tooltip("whether or not to store Sprites and Animations into categorized " +
            "subfolders. Best used with a large amount of sprites")]
        private bool OrganizeAssets = true;
        /// <summary> whether or not to output debug information </summary>
        [Tooltip("whether or not to output debug information")]
        private bool displayDebugData = false;
        /// <summary> scale at which to create new gameobject. Putting this at 1 means object 
        /// will be native pixel size </summary>
        [Tooltip("scale at which to create new gameobject. Putting this at 1 means object " +
            "will be native pixel size")]
        private float scale = 1;
    }
}
