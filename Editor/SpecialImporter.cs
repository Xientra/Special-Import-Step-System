using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImporter : AssetPostprocessor
{
	// ---------------============================ Preprocess ============================---------------//

	private void OnPreprocessAsset()
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Any Asset");
	}

	private void OnPreprocessModel()
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Model");

		ModelImporter modelImporter = assetImporter as ModelImporter;


		//Material mat = GenerateMaterialForAsset(assetPath);

		//modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(mat.GetType(), "defaultMat"), mat);
		//modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.RecursiveUp);
	}

	private void OnPreprocessAudio()
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Audio");
		AudioImporter audioImporter = assetImporter as AudioImporter;

	}

	private void OnPreprocessAnimation()
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Animation");

	}

	public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Material Description");
	}

	private void OnPreprocessTexture()
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Pre</b>process Texture");
		TextureImporter textureImporter = (TextureImporter)assetImporter;
	}

	// ---------------============================ Postprocess ============================---------------//

#if UNITY_2021_OR_NEWER
	OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
#endif
	{

		if (SISSData.Instance.printSpecialImporterDebugLogs)
		{
			foreach (string str in importedAssets)
				Debug.Log("Reimported Asset: " + str);
			foreach (string str in deletedAssets)
				Debug.Log("Deleted Asset: " + str);

			for (int i = 0; i < movedAssets.Length; i++)
				Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);

			//if (didDomainReload)
			//	Debug.Log("Domain has been reloaded");
		}



		List<KeyValuePair<string, SpecialImportStep>> pathsAndSteps = SISSData.Instance.PostPostprocessingSteps;
		for (int i = 0; i < pathsAndSteps.Count; i++)
			if (pathsAndSteps[i].Value is SpecialImportCreatePrefab pppStep)
				pppStep.PostApply(pathsAndSteps[i].Key);
		pathsAndSteps.Clear();
		AssetDatabase.Refresh();
	}


	private void OnPostprocessModel(GameObject model)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Model");

		//Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath); // always empty at this point for some reason

		OnPostprocessAnyAsset(model);
	}

	private void OnPostprocessAudio(AudioClip audioClip)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Audio");

		OnPostprocessAnyAsset(audioClip);
	}

	private void OnPostprocessAnimation(GameObject gameObject, AnimationClip animationClip)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Animation");

		OnPostprocessAnyAsset(animationClip, typeof(AnimationClip));
	}

	private void OnPostprocessMaterial(Material material)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Material");

		OnPostprocessAnyAsset(material);
	}

	private void OnPostprocessMeshHierarchy(GameObject meshHierarchy)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Mesh Hierarchy");

		OnPostprocessAnyAsset(meshHierarchy, typeof(Mesh));
	}

	private void OnPostprocessTexture(Texture2D texture)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Texture");

		OnPostprocessAnyAsset(texture);
	}

	private void OnPostprocessAnyAsset(Object importedAsset)
	{
		OnPostprocessAnyAsset(importedAsset, importedAsset.GetType());
	}
	/// <summary>
	/// Self written function. For some reason Unity does not have a function for postprocessing
	/// an arbitrary asset on import so this is called from some of the specific OnPostprocess functions.
	/// </summary>
	/// <param name="asset"></param>
	private void OnPostprocessAnyAsset(Object asset, Type assetType)
	{
		if (SISSData.Instance.printSpecialImporterDebugLogs)
			Debug.Log("SIS: <b>Post</b>process Any Asset");

		GUID guid = AssetDatabase.GUIDFromAssetPath(assetPath);

		List<SpecialImportStep> steps = SISSData.Instance.GetStepsForAsset(guid.ToString(), assetPath, assetType);
		for (int i = 0; i < steps.Count; i++)
			if (steps[i].enabled)
				steps[i].Apply(asset, context, assetImporter);
	}
}
