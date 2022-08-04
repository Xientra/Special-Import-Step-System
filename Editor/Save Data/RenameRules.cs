using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

[Serializable]
public class RenameRules
{
	// -----========== Static ==========----- //
	private static readonly SuffixTypeVariations[] DEFAULT_SUFFIX_VARIATIONS = new SuffixTypeVariations[] {
		new SuffixTypeVariations("Diffuse", new string[] { "D", "Diffuse", "Surface", "Skin", "Albedo", "AlbedoMap", "Color" }, TextureType.Diffuse),
		new SuffixTypeVariations("Specular", new string[] { "S", "Specular","Roughness", "RoughnessMap" }, TextureType.Specular),
		new SuffixTypeVariations("Metallic", new string[] { "M", "Metallic", "Glossiness" }, TextureType.Metallic),
		new SuffixTypeVariations("Normal", new string[] { "N", "Normal", "NormalMap", "Bump", "BumpMap" }, TextureType.NormalMap),
		new SuffixTypeVariations("Height", new string[] { "H", "Height", "HeightMap", "ParallaxMap", "Displacement" }, TextureType.HeightMap),
		new SuffixTypeVariations("Occlusion", new string[] { "AO", "Occlusion", "O", "Ambient Occlusion" }, TextureType.OcclusionMap),
		new SuffixTypeVariations("Emission", new string[] { "E", "Glow", "GlowMap", "Emission", "EmissionMap" }, TextureType.EmissionMap),

		new SuffixTypeVariations("Mat", new string[] { "Mat", "Material" }, TextureType.Other),
		new SuffixTypeVariations("Anim", new string[] { "Anim", "Animation" }, TextureType.Other),
		new SuffixTypeVariations("AnimCtrl", new string[] { "AnimCtrl", "Animation Controller", "AnimationController" }, TextureType.Other),
	};

	private static readonly string[] DEFAULT_SUFFIX_SEPARATOR_VARIATIONS = new string[] { "_", ".", " ", "-" };

	// -----========== Rename Keywords ==========----- //
	[Header("Replace Keywords")]

	public string objectNameKeyword = "[[OBJECTNAME]]";
	public string fileNameKeyword = "[[FILENAME]]";
	public string extensionKeyword = "[[EXTENSION]]";
	public string pathKeyword = "[[PATH]]";
	public string typeKeyword = "[[TYPE]]";
	//public string counterKeyword = "[[COUNTER]]";

	// -----========== Suffixes and Prefixes ==========----- //
	[Header("Suffix Definitions:")]

	[Tooltip("The symbol(s) used to seperate name from suffix.\nFor example '_' would lead to something like \"ModelName_normal\" for the normal map.")]
	public string desieredSuffixSeparator = "_";

	public string[] suffixSeparatorVariations = DEFAULT_SUFFIX_SEPARATOR_VARIATIONS;
	public string GetSuffixSeparatorVariationsRegex()
	{
		string r = "[";
		for (int i = 0; i < suffixSeparatorVariations.Length; i++)
			if (string.IsNullOrEmpty(suffixSeparatorVariations[i]) == false)
				r += Regex.Escape(suffixSeparatorVariations[i]);

		return r + "]";
	}

	public static string GetSuffixVariationRegex(string[] varitations)
	{
		string r = "(";
		for (int i = 0; i < varitations.Length; i++)
			r += Regex.Escape(varitations[i]) + "|";

		// remove | at the end
		if (r.EndsWith("|"))
			r = r.Substring(0, r.Length - 1);

		r += ")";
		return r;
	}

	[Space(5)]

	[SerializeField]
	private SuffixTypeVariations[] suffixes = DEFAULT_SUFFIX_VARIATIONS;
	public SuffixTypeVariations[] Suffixes { get => suffixes; }

	public void LoadDefaultSuffixVarations()
	{
		suffixes = DEFAULT_SUFFIX_VARIATIONS;
	}

	public string GetSuffix(TextureType textureType)
	{
		for (int i = 0; i < suffixes.Length; i++)
			if (suffixes[i].type == textureType)
				return suffixes[i].desieredSuffixName;
		return "";
	}

	[Serializable]
	public class SuffixTypeVariations
	{
		[SerializeField]
		public TextureType type;

		[SerializeField]
		public string desieredSuffixName;
		[SerializeField]
		public string[] suffixVariations;

		public SuffixTypeVariations(string wantedString, string[] variations, TextureType type = TextureType.Other)
		{
			this.desieredSuffixName = wantedString;
			this.suffixVariations = variations;
		}
	}

	public static string PathToRegex(string path)
	{
		path = path.Replace("/", "\\/");
		path = path.Replace(SISSData.Instance.multipleFolderWildcard, SISSData.MULTIPLE_FOLDER_REGEX);
		path = path.Replace(SISSData.Instance.fileOrFolderWildcard, SISSData.FILE_OR_FOLDER_REGEX);
		return path;
	}

	public string GetKeywordTooltip()
	{
		return objectNameKeyword + " is replaced with the name of the sub asset like a mesh name or similar.\n" +
		fileNameKeyword + " is replaced with the name of the asset in the file explorer.\n" +
		extensionKeyword + " is replaced with the extension of the asset, including the dot.\n" +
		pathKeyword + " is replaced with the path to the asset, not including the asset name.\n" +
		typeKeyword + " is replaced with a string for the type of asset. (Mesh, Texture, ...).\n";
	}
}

public enum TextureType { Other, Diffuse, Specular, Metallic, NormalMap, HeightMap, OcclusionMap, EmissionMap }
