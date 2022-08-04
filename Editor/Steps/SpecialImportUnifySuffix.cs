using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImportUnifySuffix : SpecialImportStep
{
	public bool ignoreCasing = true;
	public bool analyseTextures = false;

	public SpecialImportUnifySuffix() { }
	public SpecialImportUnifySuffix(SpecialImportTarget target) : base(target) { }

	public override string ToString()
	{
		return "Unify Suffix";
	}

	public override string GetName()
	{
		return "Unify Suffix";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(GameObject), typeof(Mesh), typeof(Material), typeof(AnimationClip), typeof(Texture), typeof(AudioClip) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			ignoreCasing = EditorGUILayout.Toggle(new GUIContent("Ignore Casing", ""), ignoreCasing);
			if (target.TargetsContainType(typeof(Texture)))
				analyseTextures = EditorGUILayout.Toggle(new GUIContent("Analyse Textures", "Tries to analyses the textures contents and give it the correct suffix if missing. Only works on assets of type Texture2D, all others will be ignored."), analyseTextures);
		}
		GUIEndDefault();

		SISSUtils.GUI.SettingsButtonLine("Unifies Suffixes based on the settings that can be found here:");

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		string name = Path.GetFileNameWithoutExtension(context.assetPath);

		AssetDatabase.RenameAsset(context.assetPath, ResolveSuffix(name));

		string newName = ResolveSuffix(importObject.name);
		//Debug.Log("regex resolve: " + newName);

		if (analyseTextures && importObject is Texture2D importTexture)
		{
			if (CheckAnySuffix(name) == false)
			{
				TextureType analysedTextureType = SISSTextureAnalyser.AnalyseTextureType(importTexture);

				if (analysedTextureType != TextureType.Other)
				{
					newName = name + SISSData.RenameRules.desieredSuffixSeparator + SISSData.RenameRules.GetSuffix(analysedTextureType);
					Debug.Log("Added " + SISSData.RenameRules.desieredSuffixSeparator + SISSData.RenameRules.GetSuffix(analysedTextureType) + " to texture.");
				}
				else
					Debug.Log("Could not analyse texture " + name);
			}
		}

		if (importObject.name != newName)
		{
			AssetDatabase.RenameAsset(context.assetPath, newName);
			//Debug.Log("Unify Suffix: old name: " + name + " => new name: " + newName);
		}
		AssetDatabase.Refresh();
	}

	private bool CheckAnySuffix(string name)
	{
		RenameRules rr = SISSData.RenameRules;
		for (int i = 0; i < rr.Suffixes.Length; i++)
			if (name.EndsWith(rr.desieredSuffixSeparator + rr.Suffixes[i].desieredSuffixName))
				return true;
		return false;
	}

	private string ResolveSuffix(string name)
	{
		RenameRules rr = SISSData.RenameRules;
		string resolvedName = name;

		for (int i = 0; i < rr.Suffixes.Length; i++)
		{
			string desieredSuffix = rr.desieredSuffixSeparator + rr.Suffixes[i].desieredSuffixName;
			string replaceRegex = rr.GetSuffixSeparatorVariationsRegex() + RenameRules.GetSuffixVariationRegex(rr.Suffixes[i].suffixVariations);

			// (?i) starts case INSENSITIVE matching
			if (ignoreCasing)
				replaceRegex = "(?i)" + replaceRegex;

			// $ requires the name to end after the regex OR have a new line
			resolvedName = Regex.Replace(resolvedName, replaceRegex + "$", desieredSuffix);
		}

		return string.IsNullOrWhiteSpace(resolvedName) ? name : resolvedName;
	}
}
