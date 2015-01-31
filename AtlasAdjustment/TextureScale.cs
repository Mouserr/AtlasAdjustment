// Only works on ARGB32, RGB24 and Alpha8 textures that are marked readable

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public static class TextureScale
{
	public static void Resize(Texture2D texture, int width, int height, InterpolationMode mode)
	{
		Bitmap image;
		using (var stream = new MemoryStream(texture.EncodeToPNG()))
		{
			using (var bitmap = new Bitmap(stream))
			{
				image = bitmap.Resize(new Size(width, height), mode);
			}
			stream.Close();
		}

		using (var stream = new MemoryStream())
		{
			image.Save(stream, image.RawFormat);
			stream.Seek(0, SeekOrigin.Begin);
			texture.LoadImage(stream.ToArray());
			stream.Close();
		}
		
		image.Dispose();
	}

	public static Bitmap Resize(this Image image, Size size, InterpolationMode mode)
	{
	  if (image == null || size.IsEmpty)
		return null;

	  var resizedImage = new Bitmap(size.Width, size.Height, image.PixelFormat);
	  resizedImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

	  using (var g = System.Drawing.Graphics.FromImage(resizedImage))
	  {
		var location = new Point(0, 0);
		g.InterpolationMode = mode;
		g.DrawImage(image, new Rectangle(location, size), new Rectangle(location, image.Size), GraphicsUnit.Pixel);
	  }
		
	  return resizedImage;
	}

	public static byte[] BitmapToByteArray(Bitmap bitmap)
	{
		BitmapData bmpdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
		int numbytes = bmpdata.Stride * bitmap.Height;
		byte[] bytedata = new byte[numbytes];
		IntPtr ptr = bmpdata.Scan0;

		Marshal.Copy(ptr, bytedata, 0, numbytes);

		bitmap.UnlockBits(bmpdata);

		return bytedata;
	}
}