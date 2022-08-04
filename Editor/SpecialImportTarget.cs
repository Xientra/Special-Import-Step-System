using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[System.Serializable]
public class SpecialImportTarget : ISerializationCallbackReceiver
{
	[SerializeField]
	public string guid = "";

	[SerializeField]
	public string path = "";

	[SerializeField]
	public string subAssetMatchString = "";

	[SerializeField]
	public List<Type> objectTypes = new List<Type>();
	private List<Type> ObjectTypes
	{
		get
		{
			if (objectTypes == null)
				objectTypes = new List<Type>();
			return objectTypes;
		}
	}
	[SerializeField]
	private List<string> objectTypeStrings = new List<string>();
	private List<string> ObjectTypeStrings
	{
		get
		{
			if (objectTypeStrings == null)
				objectTypeStrings = new List<string>();
			return objectTypeStrings;
		}
	}

	public SpecialImportTarget(string guid)
	{
		this.guid = guid;
	}
	public SpecialImportTarget(string path, string objectTypeString) : this(path, Type.GetType(objectTypeString)) { }
	public SpecialImportTarget(string path, Type objectType)
	{
		this.path = path;
		if (objectType != null)
			objectTypes.Add(objectType);
	}

	#region OnBeforeSerialize & OnAfterDeserialize
	public void OnBeforeSerialize()
	{
		ObjectTypeStrings.Clear();

		for (int i = 0; i < ObjectTypes.Count; i++)
		{
			objectTypeStrings.Add(objectTypes[i] == null ? "" : (objectTypes[i].ToString() + ", " + objectTypes[i].Assembly.ToString()));
			//Debug.Log("SpecialImportTarget serialized objectType " + (objectTypes[i] == null ? "null" : objectTypes[i].ToString()) + " to " + objectTypeStrings[i]);
		}
	}

	public void OnAfterDeserialize()
	{
		ObjectTypes.Clear();

		for (int i = 0; i < ObjectTypeStrings.Count; i++)
		{
			objectTypes.Add(string.IsNullOrEmpty(objectTypeStrings[i]) ? null : Type.GetType(objectTypeStrings[i], true));
			//Debug.Log("SpecialImportTarget deserialized objectTypeString " + (objectTypeStrings[i] == null ? "null" : objectTypeStrings[i]) + " to " + objectTypes[i].ToString());
		}

		//if (IsGuidSet() == false && objectType == null) Debug.LogError("SpecialImportTarget failed to deserialize objectType using Type.GetType.\nobjectTypeString: " + objectTypeString);
	}
	#endregion

	/// <summary>
	/// Adds the type to the list if missing. Removes the type from the list if it's part of it allready. <br/>
	/// If the type equals null it is taken that this Target object should target any type and objectTypes will be cleared.
	/// </summary>
	public void SwitchType(Type type)
	{
		if (type == null)
			return;

		if (objectTypes.Contains(type))
			objectTypes.Remove(type);
		else
			objectTypes.Add(type);
	}

	public void SetAllTypes(List<Type> types)
	{
		objectTypes = types;
	}

	public void ClearAllTypes()
	{
		objectTypes.Clear();
	}

	public bool TargetsNoTypes()
	{
		return objectTypes.Count == 0;
	}

	/// <summary>
	/// Specifically only checks if the given type is in the list of targeted types <b>NOT</b> if this Target object targets all types.
	/// </summary>
	public bool TargetsContainType(Type type)
	{
		return ObjectTypes.Contains(type);
	}

	/// <summary>
	/// Check if one of the given types is in the list of targeted types.
	/// </summary>
	public bool CheckType(Type checkType)
	{
		return CheckType(new Type[] { checkType });
	}
	public bool CheckType(Type[] checkTypes)
	{
		if (checkTypes == null)
			return false;

		for (int i = 0; i < checkTypes.Length; i++)
			if (objectTypes.FindAll((t) => checkTypes[i] == t || checkTypes[i].IsSubclassOf(t)).Count > 0)
				return true;
		return false;
	}

	public string GetStringOfTargets()
	{
		return GetStringOfTargets(null);
	}

	public string GetStringOfTargets(List<Type> everyPossibleType)
	{
		if (objectTypes == null || objectTypes.Count == 0)
			return "Nothing";

		if (everyPossibleType != null && everyPossibleType.Count > 1)
			if (objectTypes.Count == everyPossibleType.Count) // TODO: ensure this is correct
				return "Everything";

		string result = "";
		for (int i = 0; i < objectTypes.Count; i++)
			result += SISSUtils.GetTypeName(objectTypes[i]) + (i == objectTypes.Count - 1 ? "" : " | ");

		return result;
	}

	public bool IsGuidSet()
	{
		return string.IsNullOrEmpty(guid) == false;
	}

	public bool IsEmpty()
	{
		return string.IsNullOrEmpty(guid) && string.IsNullOrEmpty(path) && (objectTypes == null || objectTypes.Count == 0);
	}

	public bool IsTarget(string testPath, Type[] testTypes)
	{
		// change all wildcards to actual regex wildcards
		string regexPath = RenameRules.PathToRegex(path);

		// if this path is NOT including a filename
		if (regexPath.EndsWith("/"))
			regexPath += SISSData.FILE_OR_FOLDER_REGEX;

		//if (Regex.IsMatch(testPath, regexPath) && CheckType(testType) == false)
		//	Debug.Log("regex: " + regexPath + " | testPath: " + testPath +
		//	" | testType: " + SISSUtils.GetTypeName(testType) + " | types: " + GetStringOfTargets() +
		//	"\nRegexMatch: " + Regex.IsMatch(testPath, regexPath) + " | TypeMatch: " + CheckType(testType));

		return Regex.IsMatch(testPath, regexPath) && CheckType(testTypes);
		//return Regex.IsMatch(testPath, regexPath);
	}

	private string folderTargetPath = null;
	public string FolderTargetPath
	{
		get
		{
			//if (string.IsNullOrEmpty(folderTargetPath))
			{
				int index = -1;

				int ffIndex = path.IndexOf(SISSData.Instance.fileOrFolderWildcard);
				if (ffIndex != -1)
					index = ffIndex;
				int mfIndex = path.IndexOf(SISSData.Instance.multipleFolderWildcard);
				if (mfIndex != -1 && mfIndex > index)
					index = mfIndex;

				folderTargetPath = path;
				if (index > 1)
					folderTargetPath = path.Substring(0, index - 1);

				folderTargetPath = SISSUtils.GetExistingSubPath(folderTargetPath);
			}

			return folderTargetPath;
		}
	}

	/// <summary>
	/// Returns true for the path that corresponds to the targetPath up to the point of the first wildcard. For example for "Assets/Models/**/Prefabs/*" would be "Assets/Models"/
	/// </summary>
	public bool IsFolderTarget(string testPath)
	{
		bool result = SISSUtils.ValidifyUnityPath(testPath) == SISSUtils.ValidifyUnityPath(FolderTargetPath);
		//Debug.Log("testPath: " + SISSUtils.ValidifyUnityPath(testPath) + " | path: " + path + " | folderPath: " + SISSUtils.ValidifyUnityPath(FolderTargetPath) + "\nresult: " + result);
		return result;
	}

	public bool IsSubAssetTarget(string subAssetName, string assetPath, Type subAssetType)
	{
		string matchString = subAssetMatchString;
		matchString = SISSUtils.ResolveKeywords(matchString, assetPath, subAssetName, subAssetType);
		matchString = RenameRules.PathToRegex(matchString);

		if (string.IsNullOrWhiteSpace(matchString))
			return false;

		//Debug.Log("regex: " + matchString + " | testName: " + subAssetName +
		//	" | testType: " + SISSUtils.GetTypeName(subAssetType) + " | types: " + GetStringOfTargets() +
		//	"\nRegexMatch: " + Regex.IsMatch(subAssetName, matchString) + " | TypeMatch: " + CheckType(subAssetType));

		return CheckType(subAssetType) && Regex.IsMatch(subAssetName, matchString);
	}
}
