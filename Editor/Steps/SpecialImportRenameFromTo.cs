using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;

[Serializable]
public class SpecialImportRenameFromTo : SpecialImportStep
{
	public string renameTo;

	public SpecialImportRenameFromTo() { }
	public SpecialImportRenameFromTo(SpecialImportTarget target) : base(target)
	{
		target.subAssetMatchString = TargetName;
	}

	public override string ToString()
	{
		return "Rename " + target.subAssetMatchString + " of type " + target.GetStringOfTargets() + " to " + renameTo;
	}

	public override string GetName()
	{
		return "Rename";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(GameObject), typeof(Mesh), typeof(Material), typeof(AnimationClip), typeof(Texture), typeof(AudioClip) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			target.subAssetMatchString = SISSUtils.GUI.SyntaxHighlightTextField("Rename from:", target.subAssetMatchString, "", true, true);
			renameTo = SISSUtils.GUI.SyntaxHighlightTextField("Rename to:", renameTo, "", false, true);
		}
		GUIEndDefault();

		SISSUtils.GUI.SettingsButtonLine("Keywords and Wildcards can be seen and changed here: ");

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		string assetPath = context.assetPath;
		string renameToResolved = SISSUtils.ResolveKeywords(renameTo, assetPath, importObject.name, importObject.GetType());

		bool isSubAsset = AssetDatabase.IsSubAsset(importObject);

		bool DEBUG_LOG = true;

		if (DEBUG_LOG)
			Debug.Log("type: " + importObject.GetType());

		if (importObject is GameObject importGameObject)
		{
			// rename GameObjects
			if (target.CheckType(typeof(GameObject)))
			{
				foreach (Transform t in importGameObject.transform)
					if (target.IsSubAssetTarget(t.gameObject.name, assetPath, typeof(GameObject)))
						t.gameObject.name = SISSUtils.ResolveKeywords(renameTo, assetPath, t.gameObject.name, typeof(GameObject));
			}

			// rename Meshes
			if (target.CheckType(typeof(Mesh)))
			{
				List<Mesh> meshes = new List<MeshFilter>(importGameObject.GetComponentsInChildren<MeshFilter>()).ConvertAll(mf => mf.sharedMesh);
				meshes.AddRange(new List<SkinnedMeshRenderer>(importGameObject.GetComponentsInChildren<SkinnedMeshRenderer>()).ConvertAll(smr => smr.sharedMesh));

				for (int i = 0; i < meshes.Count; i++)
					if (target.IsSubAssetTarget(meshes[i].name, assetPath, typeof(Mesh)))
						meshes[i].name = SISSUtils.ResolveKeywords(renameTo, assetPath, meshes[i].name, typeof(Mesh));
			}

			// DOES NOT WORK. why? no fucking idea
			// rename Materials
			if (target.CheckType(typeof(Material)))
			{
				Debug.Log("material check");

				Renderer[] renderer = importGameObject.GetComponentsInChildren<Renderer>();
				for (int i = 0; i < renderer.Length; i++)
				{
					Debug.Log("renderern on " + renderer[i].gameObject.name);
					for (int j = 0; j < renderer[i].materials.Length; j++)
					{
						Debug.Log("Material Name: " + renderer[i].materials[j].name);
						if (target.IsSubAssetTarget(renderer[i].materials[j].name, assetPath, typeof(Material)))
							renderer[i].materials[j].name = SISSUtils.ResolveKeywords(renameTo, assetPath, renderer[i].materials[j].name, typeof(Material));
					}
				}
			}

			// rename Animations
			if (target.CheckType(typeof(AnimationClip)))
			{
				if (assetImporter is ModelImporter modelImporter)
				{
					for (int i = 0; i < modelImporter.clipAnimations.Length; i++)
						if (target.IsSubAssetTarget(modelImporter.clipAnimations[i].name, assetPath, typeof(AnimationClip)))
							modelImporter.clipAnimations[i].name = SISSUtils.ResolveKeywords(renameTo, assetPath, modelImporter.clipAnimations[i].name, typeof(AnimationClip));
				}
			}
		}
		else if (isSubAsset == false)
		{
			Debug.Log("Renamed " + importObject.name + " to " + renameToResolved);
			//AssetDatabase.RenameAsset(assetPath, renameToResolved);
			importObject.name = renameToResolved;
			/*
			if (importObject is Mesh importMesh)
			{
				Debug.Log("Renamed " + importAnimation.name + " to " + renameToResolved);
				importMesh.name = renameToResolved;
			}

			else if (importObject is Material importMaterial)
			{
				Debug.Log("Renamed " + importAnimation.name + " to " + renameToResolved);
				importMaterial.name = renameToResolved;
			}

			else if (importObject is AnimationClip importAnimation)
			{
				Debug.Log("Renamed " + importAnimation.name + " to " + renameToResolved);
				importAnimation.name = renameToResolved;
			}
			*/


		}

		//AssetDatabase.SaveAssetIfDirty(new GUID(targetGUID));
		AssetDatabase.Refresh();
	}
}
