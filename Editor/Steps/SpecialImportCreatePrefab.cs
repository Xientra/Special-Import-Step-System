using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Object = UnityEngine.Object;

[Serializable]
public class SpecialImportCreatePrefab : SpecialImportStep
{
	public string path = "/Prefabs/";
	public bool isLocalPath = true;

	public string prefabName = "";

	public bool centerPivot = false;
	public bool alignPivot = false;
	public Vector3 alignment = new Vector3(0, -1, 0);

	public bool mergeMeshes = false;

	public bool makeStatic = false;

	public const string PREFAB_FILE_EXTENSION = ".prefab";

	public SpecialImportCreatePrefab()
	{
		prefabName = SISSData.RenameRules.fileNameKeyword;
	}
	public SpecialImportCreatePrefab(SpecialImportTarget target) : base(target)
	{
		prefabName = SISSData.RenameRules.fileNameKeyword;
	}

	public override string ToString()
	{
		return "Create Prefab";
	}

	public override string GetName()
	{
		return "Create Prefab";
	}

	public override List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { typeof(GameObject) };
	}

	public override bool DrawGUI(bool inCreation)
	{
		GUIBeginDefault(inCreation);
		{
			isLocalPath = EditorGUILayout.ToggleLeft(new GUIContent("Local Path", "Whether or not the given path is local."), isLocalPath);
			path = SISSUtils.GUI.SyntaxHighlightTextField(isLocalPath ? "Local Path:" : "Path:", path, "", false, true);

			EditorGUILayout.Separator();

			prefabName = SISSUtils.GUI.SyntaxHighlightTextField("Prefab Name", prefabName, "", false, true);

			EditorGUILayout.Separator();

			centerPivot = EditorGUILayout.ToggleLeft(new GUIContent("Center Pivot", "Translates the model so it's pivot is at the prefab center."), centerPivot);
			if (centerPivot)
			{
				EditorGUI.indentLevel++;
				alignPivot = EditorGUILayout.ToggleLeft(new GUIContent("Align Pivot", "Moves the center of the model to the furthers point in the given direction. Very usefull for putting the center at the bottom when the model is a building or similar."), alignPivot);
				if (alignPivot)
				{
					EditorGUI.indentLevel++;
					alignment = EditorGUILayout.Vector3Field(new GUIContent("Alignment", "Describes where the mesh pivot should be placed. (0, -1, 0) for example means at the bottom of the mesh in world coordinates."), alignment);
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}

			bool previouslyEnabled = GUI.enabled;
			GUI.enabled = false;
			//mergeMeshes = EditorGUILayout.ToggleLeft(new GUIContent("Merge Meshes", "Warning: Mesh-Collider might not support the resulting amount of triangles."), mergeMeshes);
			mergeMeshes = EditorGUILayout.ToggleLeft(new GUIContent("Merge Meshes", "Currently not supported."), mergeMeshes);
			GUI.enabled = previouslyEnabled;

			makeStatic = EditorGUILayout.ToggleLeft("Make Static", makeStatic);

		}
		GUIEndDefault();

		return DrawAddRemoveGUI(inCreation);
	}

	public override void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		if (importObject is GameObject g)
			SISSData.Instance.PostPostprocessingSteps.Add(new KeyValuePair<string, SpecialImportStep>(context.assetPath, this));
		else
			Debug.LogError("Create Prefab was called with a object of type " + importObject.GetType() + ". This step only works for objects of type GameObject.");
	}

	public void PostApply(string postApplyAssetPath)
	{
		string directoryPath;

		if (isLocalPath == false)
			directoryPath = SISSUtils.ValidifyUnityPath(path);
		else
			directoryPath = SISSUtils.ValidifyUnityPath(Path.GetDirectoryName(postApplyAssetPath) + SISSUtils.ValidifyUnityPath(path, true));

		SISSUtils.CreatePathIfNotExists(directoryPath);

		GameObject createPrefabFrom = AssetDatabase.LoadAssetAtPath<GameObject>(postApplyAssetPath);

		// finalize path with file name
		string filePath = directoryPath + createPrefabFrom.name;//Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(g));

		filePath += PREFAB_FILE_EXTENSION;
		filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);

		GameObject prefabObject = new GameObject(SISSUtils.ResolveKeywords(prefabName, postApplyAssetPath, createPrefabFrom.name, createPrefabFrom.GetType()));
		GameObject modelInstanceChildGO = Object.Instantiate(createPrefabFrom);
		modelInstanceChildGO.transform.parent = prefabObject.transform;


		if (centerPivot)
		{
			Renderer mr = modelInstanceChildGO.GetComponent<Renderer>();
			if (mr == null)
				mr = modelInstanceChildGO.GetComponentInChildren<Renderer>();

			//mr.bounds.center;

			Vector3 translation = Vector3.zero;
			translation -= mr.bounds.center;

			if (alignPivot)
			{
				Vector3 t = Vector3.Project(prefabObject.transform.TransformVector(mr.bounds.extents), alignment).magnitude * alignment.normalized;
				translation -= prefabObject.transform.InverseTransformVector(t);
			}

			modelInstanceChildGO.transform.position += translation;
		}
		if (mergeMeshes)
		{
			Debug.Log("Merge Meshes is Not Implemented");
		}
		if (makeStatic)
		{
			prefabObject.isStatic = true;
		}

		PrefabUtility.SaveAsPrefabAsset(prefabObject, filePath, out bool prefabSuccess);
		Object.DestroyImmediate(prefabObject);
		if (prefabSuccess == true)
		{
			Debug.Log("Prefab for " + createPrefabFrom.name + " was saved successfully");
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}
		else
			Debug.LogWarning("Prefab for " + createPrefabFrom.name + " failed to save.");
	}
}
