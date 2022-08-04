using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImportCreateStandardMaterial : SpecialImportStep
{
	public const string MATERIAL_FILE_EXTENSION = ".mat";

	public static string materialSuffix = "mat";

	public string matchString = "*";
	public bool matchCase = false;

	public SpecialImportCreateStandardMaterial() { }
	public SpecialImportCreateStandardMaterial(SpecialImportTarget target) : base(target) { }

	public override string ToString()
	{
		return "Create Standard Material";
	}

	public override string GetName()
	{
		return "Create Standard Material";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(GameObject) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			matchString = SISSUtils.GUI.SyntaxHighlightTextField("Match String:", matchString,
				"The string based on which the textures are searched. To the end of it the suffix for each texture type will be appended.",
				true, true, SISSData.RenameRules.desieredSuffixSeparator + "[[SUFFIX]]");
		}
		GUIEndDefault();

		SISSUtils.GUI.SettingsButtonLine("Settings are taken from the list of suffix variations that can be found here: ");

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		Material newMat = GenerateMaterialForAsset(context.assetPath);

		// now set mat in unity import settings ...
	}

	public Material GenerateMaterialForAsset(string assetPath)
	{
		string assetName = Path.GetFileNameWithoutExtension(assetPath);
		string folderPath = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/";


		// ----- Automatic Material Creation ----- //
		string[] texturesInTheSameFolder = AssetDatabase.FindAssets("t: texture2D", new[] { folderPath });
		string[] materialsInSameFolder = AssetDatabase.FindAssets("t: material", new[] { folderPath });
		Material assetMat = null;

		// search if material with the name for the asset was imported
		for (int i = 0; i < materialsInSameFolder.Length; i++)
		{
			//Debug.Log("asset in the same folder: " + materialsInSameFolder[i]);
			Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialsInSameFolder[i]));
			if (mat != null)
				if (mat.name.ToLower() == (assetName + SISSData.RenameRules.desieredSuffixSeparator + materialSuffix).ToLower())
					assetMat = mat;
		}

		if (assetMat == null)
		{
			assetMat = new Material(Shader.Find("Standard"));
			AssetDatabase.CreateAsset(assetMat, folderPath + assetName + SISSData.RenameRules.desieredSuffixSeparator + materialSuffix + MATERIAL_FILE_EXTENSION);
			AssetDatabase.SaveAssets();
		}

		for (int i = 0; i < texturesInTheSameFolder.Length; i++)
		{
			//Debug.Log("texture in the same folder: " + texturesInTheSameFolder[i]);
			string textureAssetPath = AssetDatabase.GUIDToAssetPath(texturesInTheSameFolder[i]);
			Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
			if (tex != null)
			{
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.Diffuse), textureAssetPath))
					assetMat.SetTexture("_MainTex", tex);
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.Metallic), textureAssetPath))
				{
					assetMat.SetFloat("_Glossiness", 0.5f);
					assetMat.SetTexture("_MetallicGlossMap", tex);
				}
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.NormalMap), textureAssetPath))
					assetMat.SetTexture("_BumpMap", tex);
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.HeightMap), textureAssetPath))
					assetMat.SetTexture("_ParallaxMap", tex);
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.OcclusionMap), textureAssetPath))
					assetMat.SetTexture("_Occlusion", tex);
				if (CheckSuffix(tex.name, SISSData.RenameRules.GetSuffix(TextureType.EmissionMap), textureAssetPath))
				{
					assetMat.EnableKeyword("_EMISSION");
					assetMat.SetColor("_EmissionColor", Color.white);
					assetMat.SetTexture("_EmissionMap", tex);
				}
			}
		}

		return assetMat;
	}

	public bool CheckSuffix(string name, string suffix, string assetPath)
	{
		string fullMatchString = name + SISSData.RenameRules.desieredSuffixSeparator + suffix;
		fullMatchString = RenameRules.PathToRegex(fullMatchString);
		fullMatchString = SISSUtils.ResolveKeywords(fullMatchString, assetPath);

		bool result = Regex.IsMatch(name.ToLower(), fullMatchString.ToLower());
		//Debug.Log("string to match: " + name.ToLower() + " | " + "matchPattern: " + fullMatchString.ToLower() + " | result: " + result);

		return result;
	}
}
