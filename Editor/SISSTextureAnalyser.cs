using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SISSTextureAnalyser
{
	public static TextureType AnalyseTextureType(Texture2D texture)
	{
		TextureType result = TextureType.Other;

		int stepSize = 8;

		Color[] pixels = texture.GetPixels(0);
		Color avgColor = pixels[0];

		int counter100 = 0;
		int blackCount = 0;

		int analysedPixelCount = (texture.height / stepSize) + (texture.width / stepSize);

		for (int y = 0; y < texture.height; y += stepSize)
		{
			for (int x = 0; x < texture.width; x += stepSize)
			{
				Color c = pixels[y * texture.height + x];

				avgColor = (avgColor + c) * 0.5f;

				Vector3 v = Vector3.zero;
				v.x = c.r;
				v.y = c.g;
				v.z = c.b;

				if (c == Color.black)
					blackCount++;

				if (v == Vector3.right)
					counter100++;
			}
		}

		// the base requirement to be any kind of model texture
		if (texture.width == texture.height && IsPowerOfTwo(texture.height))
			result = TextureType.Diffuse;

		// more than 10% of pixels had the default normal color
		if (counter100 > analysedPixelCount * 0.1f)
			result = TextureType.NormalMap;

		if (avgColor.r == avgColor.g && avgColor.g == avgColor.b)
		{
			result = TextureType.Metallic;

			// more than 40% of pixels were black
			if (blackCount > analysedPixelCount * 0.4f)
				result = TextureType.EmissionMap;
		}

		return result;
	}

	// From: https://stackoverflow.com/questions/600293/how-to-check-if-a-number-is-a-power-of-2
	private static bool IsPowerOfTwo(int x)
	{
		return (x & (x - 1)) == 0;
	}
}
