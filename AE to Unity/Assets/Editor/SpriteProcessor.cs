using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ASE_to_Unity {

    /// <summary>
    /// Ensures that the newly created spritesheet is imported into the asset database with the proper settings
    /// Because 90% of sprites from aseprite will be in pixel art format, the interpolation and compressions are
    /// applied to best suit pixel art.
    /// </summary>
    public class SpriteProcessor /*: AssetPostprocessor */{

        //private AseData ase;
        //public AseData AnimData { set { this.ase = value; } }

        //void OnPreprocessTexture() {
        //    TextureImporter textureImporter = (TextureImporter)assetImporter;
        //    textureImporter.textureType = TextureImporterType.Sprite;
        //    textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        //    textureImporter.filterMode = FilterMode.Point;
        //    textureImporter.compressionQuality = 100;
        //    textureImporter.mipmapEnabled = false;
        //}

        //public void OnPostprocessTexture(Texture2D texture) {
        //    Debug.Log("ASE: (" + ase + ")");

        //    Vector2 dim = (ase == null) ? new Vector2(texture.width, texture.height) : ase.dim;

        //    int colCount = texture.width / (int)dim.x;
        //    int rowCount = texture.height / (int)dim.y;

        //    List<SpriteMetaData> metas = new List<SpriteMetaData>();

        //    for (int r = 0; r < rowCount; ++r) {
        //        for (int c = 0; c < colCount; ++c) {
        //            SpriteMetaData meta = new SpriteMetaData();
        //            meta.rect = new Rect(c * dim.x, r * dim.y, dim.x, dim.y);
        //            meta.name = c + "-" + r;
        //            metas.Add(meta);
        //        }
        //    }

        //    TextureImporter textureImporter = (TextureImporter)assetImporter;
        //    textureImporter.spritesheet = metas.ToArray();
        //}

        //public void OnPostprocessSprites(Texture2D texture, Sprite[] sprites) {
        //    Debug.Log("Sprites: " + sprites.Length);
        //}
    }
}
