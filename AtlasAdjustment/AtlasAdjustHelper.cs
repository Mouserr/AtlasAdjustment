using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AtlasAdjustHelper
{
	public static int CreateUIAtlas(List<Texture2D> images, string atlasName, int? size, out int minSize, UITexturePacker.FreeRectChoiceHeuristic heuristic)
	{
        string selectionFolder = NGUIEditorTools.GetSelectionFolder();
        if (string.IsNullOrEmpty(atlasName))
	    {
	        var lastIndexOf = selectionFolder.Substring(0, selectionFolder.Length - 1).LastIndexOf("/");
	        atlasName = selectionFolder.Substring(lastIndexOf + 1, selectionFolder.Length - lastIndexOf - 2);
	    }

	    UIAtlas atlas;
		string prefabPath = AssetDatabase.GetAllAssetPaths().FirstOrDefault(x => x.EndsWith("/" + atlasName + ".prefab"));
		if (prefabPath == null)
		{
			prefabPath = selectionFolder + atlasName + ".prefab";
			atlas = CreateAtlas(atlasName);
		}
		else
		{
			Debug.Log("Update existing");
			atlas = AssetDatabase.LoadAssetAtPath(prefabPath, typeof (UIAtlas)) as UIAtlas;
		}

        Texture2D newTexture = UpdateUIAtlas(atlas, images, ref size, out minSize, heuristic);
		var texture = SaveTexture(prefabPath, newTexture);
		atlas.spriteMaterial.mainTexture = texture;
		
		EditorUtility.SetDirty(atlas);

		// Update the prefab
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("***************** All done - " + DateTime.Now);
		return size.Value;
	}

    public static int GetMinSize(Texture2D[] textures, UITexturePacker.FreeRectChoiceHeuristic heuristic)
    {
        int padding = 1;
        return GetMinAtlasSize(textures, 4, ref padding, 4, heuristic);
    }

	private static UIAtlas CreateAtlas(string atlasName)
	{
// Create a new prefab for the atlas
		var selectionFolder = NGUIEditorTools.GetSelectionFolder();
		
		string prefabPath = selectionFolder + atlasName + ".prefab";
		string matPath = NGUIEditorTools.GetSelectionFolder() + atlasName + ".mat";
		Object prefab = PrefabUtility.CreateEmptyPrefab(prefabPath);
		Shader shader = Shader.Find(NGUISettings.atlasPMA ? "Unlit/Premultiplied Colored" : "Unlit/Transparent Colored");
		var mat = new Material(shader);

		// Save the material
		AssetDatabase.CreateAsset(mat, matPath);
		AssetDatabase.Refresh();

		// Create a new game object for the atlas
		UIAtlas atlas = new GameObject(atlasName).AddComponent<UIAtlas>();
		atlas.spriteMaterial = mat;

		// Update the prefab
		PrefabUtility.ReplacePrefab(atlas.gameObject, prefab);
		Object.DestroyImmediate(atlas.gameObject);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		// Select the atlas
		return  AssetDatabase.LoadAssetAtPath(prefabPath, typeof(UIAtlas)) as UIAtlas;
	}


    private static Texture2D UpdateUIAtlas(UIAtlas atlas, List<Texture2D> textures, ref int? size, out int minSize, UITexturePacker.FreeRectChoiceHeuristic heuristic)
	{
		Debug.Log("***************** Updating atlas " + atlas.name + " - " + DateTime.Now);
		Rect[] texRects;
        Texture2D newTexture = CreateTexture(textures.ToArray(), ref size, 1, out texRects, out minSize, heuristic);
		List<UIAtlasMaker.SpriteEntry> sprites = UIAtlasMaker.CreateSprites(textures.Cast<Texture>().ToList());
		for (int i = 0; i < sprites.Count; i++)
		{
			Rect rect = NGUIMath.ConvertToPixels(texRects[i], newTexture.width, newTexture.height, true);

			UIAtlasMaker.SpriteEntry se = sprites[i];
			se.x = Mathf.RoundToInt(rect.x);
			se.y = Mathf.RoundToInt(rect.y);
			se.width = Mathf.RoundToInt(rect.width);
			se.height = Mathf.RoundToInt(rect.height);
		}

		// Replace the sprites within the atlas
		UIAtlasMaker.ReplaceSprites(atlas, sprites);
		
		// Release the temporary textures
		UIAtlasMaker.ReleaseSprites(sprites);

		return newTexture;
	}

	private static Texture2D SaveTexture(string atlasPath, Texture2D newTexture)
	{
		Debug.Log("***************** Saving texture - " + DateTime.Now);
		byte[] bytes = newTexture.EncodeToPNG();
		var dotIndex = atlasPath.LastIndexOf('.');
		string texturePath = atlasPath.Replace(atlasPath.Substring(dotIndex, atlasPath.Length - dotIndex), ".png");
		using (FileStream file = File.Open(texturePath, FileMode.Create))
		{
			var binary = new BinaryWriter(file);
			binary.Write(bytes);
			file.Close();
		}
		AssetDatabase.Refresh();
		Debug.Log("***************** texture path - " + texturePath);
		newTexture = NGUIEditorTools.ImportTexture(texturePath, false, true, true);
		return newTexture;
	}

    private static Texture2D CreateTexture(Texture2D[] textures, ref int? size, int padding, out Rect[] texRects, out int minAtlasSize, UITexturePacker.FreeRectChoiceHeuristic heuristic)
	{
		var result = new Texture2D(1, 1);

		minAtlasSize = GetMinAtlasSize(textures, size ?? 128, ref padding, size ?? 128, heuristic);
		Debug.Log("*************************** Min size = " + minAtlasSize + "  - " + DateTime.Now) ;
		NGUISettings.forceSquareAtlas = true;
        texRects = UITexturePackerSpec.PackTexturesSpec(result, textures, minAtlasSize, minAtlasSize, padding, minAtlasSize, heuristic);
		Debug.Log("*************************** texture packed - " + DateTime.Now);
		if (size == null)
		{
			int power = (int) Math.Round(Math.Log(minAtlasSize, 2));
			
			size = (int) Math.Pow(2, power);
		}
		if (size <= minAtlasSize)
		{
			TextureScale.Resize(result, size.Value, size.Value, InterpolationMode.HighQualityBicubic);
		}
		else
		{
			texRects = UITexturePackerSpec.PackTextures(result, textures, size.Value, size.Value, padding, size.Value);
		}

		return result;
	}

	public static ulong GetSumArea(Texture2D[] textures)
	{
		ulong area = 0;
		foreach (var texture in textures)
		{
			area += ((ulong) texture.width) * ((ulong) texture.height);
		}

		return area;
	}

    private static int GetMinAtlasSize(Texture2D[] textures, int startSize, ref int padding, int nessSize, UITexturePacker.FreeRectChoiceHeuristic heuristic)
	{
		int startDiff = startSize /2;
		int minDiff = 0;

		int size = startSize;
		int diff = startDiff;
		bool? prevResult = null;
		while (true)
		{
            bool curResult = CanPackTexturesInSize(textures, size, (int)Math.Ceiling(padding * ((double)size / nessSize)), heuristic);

			if (prevResult.HasValue && curResult != prevResult)
			{
				if (diff/2 <= minDiff)
				{

					if (!curResult)
					{
						size += diff;
					}

					padding = (int) Math.Ceiling(padding*((double) size/nessSize));

					return size;
				}

				diff /= 2;
			}

			if (curResult)
			{
				size -= diff;
			}
			else
			{
				size += diff;
			}

			prevResult = curResult;
		}	
	}

	private static bool CanPackTexturesInSize(Texture2D[] textures, int size, int padding, UITexturePacker.FreeRectChoiceHeuristic heuristic)
	{
		using (var packer = new UITexturePackerSpec(size, size, false))
		{
			for (int i = 0; i < textures.Length; i++)
			{
				Texture2D tex = textures[i];
				if (!tex) continue;

				Rect rect = new Rect();

				int xPadding;
				int yPadding;

				for (xPadding = 1; xPadding >= 0; --xPadding)
				{
					for (yPadding = 1; yPadding >= 0; --yPadding)
					{
						rect = packer.Insert(tex.width + (xPadding*padding), tex.height + (yPadding*padding), heuristic);
						if (rect.width > 0 && rect.height > 0) break;

							// After having no padding if it still doesn't fit -- we can't pack all textures in this size.
						else if (xPadding == 0 && yPadding == 0)
						{
							return false;
						}
					}
					if (rect.width > 0 && rect.height > 0) break;
				}
			}
		}

		return true;
	}
}