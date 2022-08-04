using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImportMoveAsset : SpecialImportStep
{
	public string moveToPath = "";

	public SpecialImportMoveAsset() { }
	public SpecialImportMoveAsset(SpecialImportTarget target) : base(target)
	{
		moveToPath = target.path;
	}

	public override string ToString()
	{
		return "Moves assets to " + moveToPath;
	}

	public override string GetName()
	{
		return "Move Assets";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(GameObject), typeof(Mesh), typeof(Material), typeof(AnimationClip), typeof(Texture), typeof(AudioClip) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			moveToPath = SISSUtils.GUI.SyntaxHighlightTextField("Move to Path:", moveToPath, "The path the asset should be moved to.", false, true);
		}
		GUIEndDefault();

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		string assetPath = context.assetPath;
		string resolvedTargetPath = SISSUtils.ResolveKeywords(SISSUtils.ValidifyUnityPath(moveToPath), assetPath, type: importObject.GetType());

		bool createdNewFolder = SISSUtils.CreatePathIfNotExists(resolvedTargetPath);

		if (importObject is AudioClip audioClip)
		{
			if (audioClip.length > 10)
			{
				// is sound effect
			}
		}
		else if (importObject is Texture texture)
		{
			if (texture.height > 1024)
			{
				// is large texture
			}
		}

		// debug
		//Debug.Log("move asset to " + resolvedTargetPath);
		//AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		//Debug.Log("meta file: " + AssetDatabase.GetTextMetaFilePathFromAssetPath(resolvedTargetPath));
		//Debug.Log("is valid folder: " + AssetDatabase.IsValidFolder(resolvedTargetPath));

		string moveResult = AssetDatabase.MoveAsset(assetPath, resolvedTargetPath + Path.GetFileName(context.assetPath));
		if (moveResult != "")
		{
			Debug.LogError("Move Failed with reason: " + moveResult);
			AssetDatabase.ImportAsset(context.assetPath);
		}
		else
			Debug.Log(importObject.name + " was moved on Import to the path " + resolvedTargetPath);

		AssetDatabase.Refresh();
	}
}
