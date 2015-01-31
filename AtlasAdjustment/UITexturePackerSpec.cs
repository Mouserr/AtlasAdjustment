/*
	Override for NGui UITexturePacker
*/

using System;
using UnityEngine;

public class UITexturePackerSpec : UITexturePacker, IDisposable
{
	private struct Storage
	{
		public Rect rect;
		public bool paddingX;
		public bool paddingY;
	}

	public UITexturePackerSpec(int width, int height, bool rotations) : base(width, height, rotations)
	{
	}
	
	public void Dispose()
	{
		freeRectangles.Clear();
		freeRectangles = null;
		usedRectangles.Clear();
		usedRectangles = null;
	}

	public static Rect[] PackTexturesSpec(Texture2D texture, Texture2D[] textures, int width, int height, int padding, int maxSize, FreeRectChoiceHeuristic heuristic)
	{
		if (width > maxSize && height > maxSize) return null;
		if (width > maxSize || height > maxSize) { int temp = width; width = height; height = temp; }

		// Force square by sizing up
		if (NGUISettings.forceSquareAtlas)
		{
			if (width > height)
				height = width;
			else if (height > width)
				width = height;
		}
		Storage[] storage;
		using (var bp = new UITexturePackerSpec(width, height, false))
		{
			storage = new Storage[textures.Length];

			for (int i = 0; i < textures.Length; i++)
			{
				Texture2D tex = textures[i];
				if (!tex) continue;

				Rect rect = new Rect();

				int xPadding = 1;
				int yPadding = 1;

				for (xPadding = 1; xPadding >= 0; --xPadding)
				{
					for (yPadding = 1; yPadding >= 0; --yPadding)
					{
						rect = bp.Insert(tex.width + (xPadding * padding), tex.height + (yPadding * padding), heuristic);
						if (rect.width > 0 && rect.height > 0) break;

							// After having no padding if it still doesn't fit -- increase texture size.
						else if (xPadding == 0 && yPadding == 0)
						{
							return PackTexturesSpec(texture, textures, width * (width <= height ? 2 : 1),
                                height * (height < width ? 2 : 1), padding, maxSize, heuristic);
						}
					}
					if (rect.width > 0 && rect.height > 0) break;
				}

				storage[i] = new Storage();
				storage[i].rect = rect;
				storage[i].paddingX = (xPadding != 0);
				storage[i].paddingY = (yPadding != 0);
			}
		}

		texture.Resize(width, height);
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				texture.SetPixel(i, j, new Color());
			}
		}


		// The returned rects
		Rect[] rects = new Rect[textures.Length];

		for (int i = 0; i < textures.Length; i++)
		{
			Texture2D tex = textures[i];
			if (!tex) continue;

			Rect rect = storage[i].rect;
			int xPadding = (storage[i].paddingX ? padding : 0);
			int yPadding = (storage[i].paddingY ? padding : 0);
			Color[] colors = tex.GetPixels();

			// Would be used to rotate the texture if need be.
			if (rect.width != tex.width + xPadding)
			{
				Color[] newColors = tex.GetPixels();

				for (int x = 0; x < rect.width; x++)
				{
					for (int y = 0; y < rect.height; y++)
					{
						int prevIndex = ((int)rect.height - (y + 1)) + x * (int)tex.width;
						newColors[x + y * (int)rect.width] = colors[prevIndex];
					}
				}

				colors = newColors;
			}

			texture.SetPixels((int)rect.x, (int)rect.y, (int)rect.width - xPadding, (int)rect.height - yPadding, colors);
			rect.x /= width;
			rect.y /= height;
			rect.width = (rect.width - xPadding) / width;
			rect.height = (rect.height - yPadding) / height;
			rects[i] = rect;
		}
		texture.Apply();
		return rects;
	}
}
