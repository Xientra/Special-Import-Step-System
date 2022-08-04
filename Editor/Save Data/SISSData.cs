using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SISSData : ScriptableObject
{
	// -----========== Singleton ==========----- //

	public const string PATH = "Assets/Special Import Steps System/SISSData.asset";
	public static string PATH_DIR { get => Path.GetDirectoryName(PATH); }

	public static string ICONS_DIR { get => PATH_DIR + "/Icons/"; }

	/// <summary>
	/// The key that is used to save all non guid containing steps in the Dictionary.
	/// </summary>
	public const string PATH_STEP_GUID_KEY = "";
	//public const string LOCAL_PATH_KEY = "../";

	private static SISSData instance;
	/// <summary> Loads the asset from the Database at PATH. </summary>
	public static SISSData Instance
	{
		get
		{
			// try loading instance, if singleton is not referenced yet
			if (instance == null)
			{
				// create folder if missing
				string folderPath = Path.GetDirectoryName(PATH);
				if (AssetDatabase.IsValidFolder(folderPath) == false)
					AssetDatabase.CreateFolder("Assets", new DirectoryInfo(folderPath).Name);

				instance = AssetDatabase.LoadAssetAtPath<SISSData>(PATH);

				// create new file if missing instance cannot be loaded
				if (instance == null)
				{
					SISSData newInstance = ScriptableObject.CreateInstance<SISSData>();
					AssetDatabase.CreateAsset(newInstance, PATH);
					AssetDatabase.SaveAssets();

					instance = newInstance;
				}
			}

			// everything exists already, just return the instance
			return instance;
		}
	}


	// -----========== Special Import Steps Save Data ==========----- //

	[SerializeField]
	[Tooltip("A serializable dictionary wrapper that mapps from guid (string) to a wrapper of List<SpecialimportStep> (SpecialImportStepList).")]
	private StepsDictionary objectSteps;
	public StepsDictionary ObjectSteps { get => objectSteps; }

	[SerializeField]
	[Tooltip("The list in which all steps are saved that target multiple files (using a path) instead of just one (using a guid).")]
	private SpecialImportStepList pathSteps;
	public SpecialImportStepList PathSteps { get => pathSteps; }

	public void AddStep(SpecialImportStep step)
	{
		if (step.target.IsGuidSet())
		{
			// add step with a guid
			if (objectSteps == null)
				objectSteps = new StepsDictionary();

			if (objectSteps.ContainsKey(step.target.guid) == false)
				objectSteps.Add(step.target.guid, new SpecialImportStepList());
			objectSteps[step.target.guid].Values.Add(step);
		}
		else
		{
			// add step with a path
			if (pathSteps == null)
				pathSteps = new SpecialImportStepList();

			pathSteps.Values.Add(step);
		}

		Debug.Log("Added step with " + (step.target.IsGuidSet() ? "guid" : "path"));

		// save changes
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssetIfDirty(this);
	}

	public void Save()
	{
		// save changes
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssetIfDirty(this);
	}

	public void RemoveStep(SpecialImportStep step)
	{
		if (step.target.IsGuidSet())
		{
			if (objectSteps.ContainsKey(step.target.guid))
			{
				bool stepExists = objectSteps[step.target.guid].Values.Remove(step);
				if (stepExists)
				{
					if (objectSteps[step.target.guid].Values.Count == 0)
						objectSteps.Remove(step.target.guid);

					// save changes
					EditorUtility.SetDirty(this);
					AssetDatabase.SaveAssetIfDirty(this);
				}
				else
					Debug.LogWarning("Tried to remove a (guid) step that does not exist.");
			}
			else
				Debug.LogWarning("Tried to remove step for a GUID that does not exist.");
		}
		else
		{
			bool stepExists = pathSteps.Values.Remove(step);
			if (stepExists)
			{
				// save changes
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssetIfDirty(this);
			}
			else
				Debug.LogWarning("Tried to remove a step that does not exist.");
		}
	}

	public List<SpecialImportStep> GetStepsForAsset(string guid, string path, Type type)
	{
		return GetStepsForAsset(guid, path, new Type[] { type });
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="guid">The guid of the asset.</param>
	/// <param name="path">The path the asset is located at.</param>
	/// <param name="assetTypes">The type of the asset and all the types of the assets sub assets.</param>
	/// <returns></returns>
	public List<SpecialImportStep> GetStepsForAsset(string guid, string path, Type[] assetTypes)
	{
		List<SpecialImportStep> stepsForAsset = new List<SpecialImportStep>();

		List<SpecialImportStep> stepsForGUID = GetStepsForGUID(guid, assetTypes, false);
		List<SpecialImportStep> pathSteps = GetStepsForPath(path, assetTypes, false);

		// combine + return
		stepsForAsset.AddRange(stepsForGUID);
		stepsForAsset.AddRange(pathSteps);
		stepsForAsset.Sort((sis1, sis2) => sis1.priority - sis2.priority);
		return stepsForAsset;
	}

	// O(1)
	public List<SpecialImportStep> GetStepsForGUID(string guid, Type[] assetTypes, bool sort = true)
	{
		List<SpecialImportStep> steps = (objectSteps != null && objectSteps.ContainsKey(guid)) 
			? objectSteps[guid].Values
			: new List<SpecialImportStep>();
		
		if (sort)
			steps.Sort((sis1, sis2) => sis1.priority - sis2.priority);
		return steps;
	}

	// O(n)
	public List<SpecialImportStep> GetStepsForPath(string path, Type[] assetTypes, bool sort = true)
	{
		List<SpecialImportStep> steps = (pathSteps != null)
			? pathSteps.Values.FindAll(sis =>
				assetTypes == null ? sis.WorksForOneOfTypes(new List<Type>()) : sis.WorksForOneOfTypes(new List<Type>(assetTypes)) // new List<Type>(null) gives { null }
				&& sis.target.IsTarget(path, assetTypes))
			: new List<SpecialImportStep>();
		
		if (sort)
			steps.Sort((sis1, sis2) => sis1.priority - sis2.priority);
		return steps;
	}

	public List<SpecialImportStep> GetStepsForFolder(string path, bool sort = true)
	{
		List<SpecialImportStep> steps = (pathSteps != null) ? pathSteps.Values.FindAll(sis => sis.target.IsFolderTarget(path)) : new List<SpecialImportStep>();
		if (sort)
			steps.Sort((sis1, sis2) => sis1.priority - sis2.priority);
		return steps;

	}

	public void NukeData()
	{
		objectSteps.Clear();
		pathSteps.Values.Clear();
		Debug.Log("<color=#ff1010><b>ALL SPECIAL IMPORT STEPS DATA HAS BEEN DELETED.</b></color>");
	}

	// -----========== Other Settings ==========----- //

	public bool printSpecialImporterDebugLogs = true;
	public string fileOrFolderWildcard = "*";
	public string multipleFolderWildcard = "/**";


	// \w == [a-zA-Z0-9_] == all numbers and characters
	// - . ' ' dash, dot and space are also valid characters (each has \ before so they are seen as symbols)
	// all those characters in brackets [...]
	// + after backets means any number of these characters with at least 1
	public const string FILE_OR_FOLDER_REGEX = @"[\w\-\.\ ]+"; // [\w-. ]+
	// only / is now also allowed
	public const string MULTIPLE_FOLDER_REGEX = @"[\w\-\.\ \/]+"; // [\w-. /]+



	// -----========== Project View Settings ==========----- //

	public bool drawInProjectView = true;
	public bool showTooltip = true;

	public Color iconColor = new Color(0.3f, 0.3f, 1, 0.8f);
	public Color pathStepColor = new Color(0.3f, 0.3f, 1, 0.8f);
	public Color guidStepColor = new Color(0.3f, 0.3f, 1, 0.8f);
	public float overlayOpacity = 0.1f;
	public Color suffixColor = new Color(0.8f, 0.6627451f, 0, 0.8f);

	[MenuItem("SISS/Project View Drawing")]
	public static void SwitchProjectViewDrawing()
	{
		Instance.drawInProjectView = !Instance.drawInProjectView;
	}

	// -----========== Rename Rules ==========----- //

	[SerializeField]
	public RenameRules renameRules;
	public static RenameRules RenameRules { get => Instance.renameRules; }


	// -----========== Post Postprocessing :) ==========----- //

	// this is specifically used for the Create Prefab Step to create prefabs AFTER the import process as it is impossible to do during it
	private List<KeyValuePair<string, SpecialImportStep>> postPostprocessingSteps;
	public List<KeyValuePair<string, SpecialImportStep>> PostPostprocessingSteps
	{
		get
		{
			if (postPostprocessingSteps == null)
				postPostprocessingSteps = new List<KeyValuePair<string, SpecialImportStep>>();
			return postPostprocessingSteps;
		}
	}
}
