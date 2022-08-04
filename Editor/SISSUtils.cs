using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Special Import Steps System Utilities
/// </summary>
public static class SISSUtils
{
	public static Vector3 AbsVector(Vector3 vector)
	{
		return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
	}

	/// <summary>
	/// Return a Vector3 with either 1, 0 or -1 in x, y, z depending on the sign of the input vector
	/// </summary>
	/// <param name="vector"></param>
	/// <returns></returns>
	public static Vector3 SignVector(Vector3 vector)
	{
		return new Vector3(Mathf.Sign(vector.x), Mathf.Sign(vector.y), Mathf.Sign(vector.z));
	}

	public static GameObject[] GetChilrendOfGameObject(GameObject gameObject)
	{
		GameObject[] children = new GameObject[gameObject.transform.childCount];

		int nextIndex = 0;
		foreach (Transform child in gameObject.transform)
			if (child != gameObject.transform)
				children[nextIndex++] = child.gameObject;

		return children;
	}

	public static void DumpSubAssets(string assetPath)
	{
		Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
		for (int i = 0; i < subAssets.Length; i++)
			Debug.Log("Name: " + subAssets[i].name + " | Type: " + subAssets[i].GetType());
	}

	public static string GetTypeName(System.Type type)
	{
		return type.ToString().Substring(type.ToString().LastIndexOf('.') + 1);
	}

	/// <summary>
	/// Takes a string and replaces all keywords like <see cref="SISSData.fileNameKeyword"/> with the respective values taken from the second parameter path.<br/>
	/// If type is null <see cref="SISSData.typeKeyword"/> will not be replaced.
	/// </summary>
	/// <returns>The string to resolve with all keywords replaced with the actual values.</returns>
	public static string ResolveKeywords(string stringToResolve, string path, /*int counter,*/ string objectName = "", System.Type type = null)
	{
		if (objectName == "")
			objectName = Path.GetFileNameWithoutExtension(path);

		stringToResolve = stringToResolve.Replace(SISSData.RenameRules.fileNameKeyword, Path.GetFileNameWithoutExtension(path));
		stringToResolve = stringToResolve.Replace(SISSData.RenameRules.extensionKeyword, Path.GetExtension(path));
		stringToResolve = stringToResolve.Replace(SISSData.RenameRules.pathKeyword, Path.GetDirectoryName(path));
		//stringToResolve = stringToResolve.Replace(SISSData.RenameRules.counterKeyword, counter.ToString());
		if (type != null)
			stringToResolve = stringToResolve.Replace(SISSData.RenameRules.typeKeyword, GetTypeName(type));
		return stringToResolve;
	}

	/// <summary>
	/// Checks if the path exists and if not creates all missing folders.
	/// </summary>
	/// <returns>True if any new folders were created.</returns>
	public static bool CreatePathIfNotExists(string path)
	{
		bool newFolderCreated = false;

		string[] pathParts = path.Split('/');

		int continueIndex = 0;
		for (int i = 1; i < pathParts.Length; i++)
		{
			continueIndex = path.IndexOf('/', continueIndex);

			string subPath = path.Substring(0, continueIndex++);

			if (Directory.Exists(subPath) == false)
			{
				string newFolderGUID = AssetDatabase.CreateFolder(Directory.GetParent(subPath).ToString(), new DirectoryInfo(subPath).Name);
				AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(newFolderGUID));
				newFolderCreated = true;
			}
		}

		AssetDatabase.Refresh();

		return newFolderCreated;
	}


	/// <summary>
	/// Takes a path and returns a sub path of it to the point where it exists. So in the best case it returns the same string, in the worst case it returns "Assets/".
	/// </summary>
	public static string GetExistingSubPath(string path)
	{
		string[] pathParts = path.Split('/');

		string previousSubPath = "Assets/";
		int continueIndex = 0;
		for (int i = 1; i < pathParts.Length; i++)
		{
			continueIndex = path.IndexOf('/', continueIndex);
			string subPath = path.Substring(0, continueIndex++);

			if (Directory.Exists(subPath) == false)
				return ValidifyUnityPath(previousSubPath);

			previousSubPath = subPath;
		}

		return path;
	}

	/// <summary>
	/// Add "Assets" to the beginning if missing. <br/>
	/// Add "/" to the end if missing.<br/>
	/// Replace "\" with "/".<br/>
	/// Replace "//" with "/".<br/>
	/// If String NullOrWhiteSpace it returns "/" or "Assets/". 
	/// </summary>
	public static string ValidifyUnityPath(string path, bool localPath = false)
	{
		if (string.IsNullOrWhiteSpace(path))
			return localPath ? "/" : "Assets/";

		// add "Assets" to the beginning if missing
		if (localPath == false)
		{
			if (path.StartsWith("Assets") == false)
				path = "Assets/" + path;
		}
		else if (path.StartsWith("/") == false)
			path = "/" + path;

		// add "/" to the end if missing
		if (path[path.Length - 1] != '/')
			path += '/';

		// replace all "\" with "/"
		path = path.Replace("\\", "/");

		// change double "//" to "/"
		path = path.Replace("//", "/");

		return path;
	}


	// ----------========== GUI Functions ==========---------- //

	public static class GUI
	{
		public static bool CenteredButton(string label, int maxWidth = 200)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("", options: GUILayout.MinWidth(0)); // to force the add button to the middle

			bool result = GUILayout.Button(label, options: GUILayout.MaxWidth(maxWidth));

			EditorGUILayout.LabelField("", options: GUILayout.MinWidth(0)); // to force the add button to the middle
			EditorGUILayout.EndHorizontal();

			return result;
		}

		/// <summary>
		/// Draws a line with a label on the left and a button the right that links to the SISSData instance in the project view.
		/// </summary>
		public static void SettingsButtonLine(string label, bool indent = true)
		{
			if (indent)
				EditorGUI.indentLevel++;
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField(label);
			if (GUILayout.Button("Settings", options: GUILayout.MaxWidth(203)))
			{
				EditorGUIUtility.PingObject(SISSData.Instance);
				Selection.activeObject = SISSData.Instance;
			}

			EditorGUILayout.EndHorizontal();
			if (indent)
				EditorGUI.indentLevel--;
		}

		public static string SyntaxHighlightTextField(string label, string text, string tooltip = "", bool highlightWildcards = true, bool highlightKeywords = true, string customHighlighSuffix = "")
		{
			GUIStyle guiS = EditorStyles.textField;
			guiS.richText = true;

			if (text == null)
				text = "";


			if (highlightWildcards == true)
				tooltip += "\nSupports Wildcards (like " + SISSData.Instance.fileOrFolderWildcard + ").";
			if (highlightKeywords == true)
				tooltip += "\nSupports Keywords (like " + SISSData.RenameRules.fileNameKeyword + ").";



			// ----- actual text field ----- //
			Rect r = EditorGUILayout.BeginVertical();
			string newText = EditorGUILayout.TextField(new GUIContent(label, tooltip), text);
			EditorGUILayout.EndVertical();


			// ----- string replace ----- //
			string highlighText = newText;

			string cPATH = "<color=#" + ColorUtility.ToHtmlStringRGB(SISSData.Instance.pathStepColor) + ">";
			string cGUID = "<color=#" + ColorUtility.ToHtmlStringRGB(SISSData.Instance.guidStepColor) + ">";
			string cSuffix = "<color=#" + ColorUtility.ToHtmlStringRGB(SISSData.Instance.suffixColor)+ ">";
			string cEnd = "</color>";

			if (highlightWildcards)
			{
				highlighText = highlighText.Replace(SISSData.Instance.fileOrFolderWildcard, cPATH + SISSData.Instance.fileOrFolderWildcard + cEnd);
				highlighText = highlighText.Replace(SISSData.Instance.multipleFolderWildcard, cPATH + SISSData.Instance.multipleFolderWildcard + cEnd);
			}
			if (highlightKeywords)
			{
				highlighText = highlighText.Replace(SISSData.RenameRules.objectNameKeyword, cGUID + SISSData.RenameRules.objectNameKeyword + cEnd);
				highlighText = highlighText.Replace(SISSData.RenameRules.fileNameKeyword, cGUID + SISSData.RenameRules.fileNameKeyword + cEnd);
				highlighText = highlighText.Replace(SISSData.RenameRules.extensionKeyword, cGUID + SISSData.RenameRules.extensionKeyword + cEnd);
				highlighText = highlighText.Replace(SISSData.RenameRules.pathKeyword, cGUID + SISSData.RenameRules.pathKeyword + cEnd);
				highlighText = highlighText.Replace(SISSData.RenameRules.typeKeyword, cGUID + SISSData.RenameRules.typeKeyword + cEnd);
				//highlighText = highlighText.Replace(SISSData.RenameRules.counterKeyword, cGUID + SISSData.RenameRules.counterKeyword + cEnd);
			}
			if (string.IsNullOrEmpty(customHighlighSuffix) == false)
			{
				highlighText += cSuffix + customHighlighSuffix + cEnd;
			}

			// ----- highlight label field ----- //
			bool previouslyEnabled = UnityEngine.GUI.enabled;
			UnityEngine.GUI.enabled = false;
			EditorGUI.LabelField(r, label, highlighText, guiS);
			UnityEngine.GUI.enabled = previouslyEnabled;

			return newText;
		}


		public static void DrawBox(Rect area, bool isGUIDStep)
		{
			DrawBox(area, 1f, isGUIDStep ? SISSData.Instance.guidStepColor : SISSData.Instance.pathStepColor, new Color(0, 0, 0, 0.1f));
		}
		public static void DrawBox(Rect area, float lineWidth, Color lineColor, Color backgroundColor)
		{
			DrawBox(area, lineWidth / 2, lineWidth / 2, lineColor, backgroundColor);
		}
		public static void DrawBox(Rect area, float lineWidthIn, float lineWidthOut, Color lineColor, Color backgroundColor)
		{
			EditorGUI.DrawRect(area, backgroundColor); // background

			float lwIn = lineWidthIn;
			float lwOut = lineWidthOut;
			float lw = lwIn + lwOut;

			EditorGUI.DrawRect(new Rect(area.x, area.y - lwOut, area.width, lw), lineColor); // top
			EditorGUI.DrawRect(new Rect(area.x, area.y + area.height - lwIn, area.width, lw), lineColor); // bottom
			EditorGUI.DrawRect(new Rect(area.x - lwOut, area.y, lw, area.height), lineColor); // left
			EditorGUI.DrawRect(new Rect(area.x + area.width - lwIn, area.y, lw, area.height), lineColor); // right
		}

		public static void DrawProjectViewIcon(Rect area, Color color)
		{
			Texture2D arrow = EditorGUIUtility.Load(SISSData.ICONS_DIR + "/SISSIcon.png") as Texture2D;

			UnityEngine.GUI.DrawTexture(area, arrow, ScaleMode.ScaleToFit, true, 1, color, 0, 0);
		}

		public static void DrawArrowDown(Rect area, Color color)
		{
			Texture2D arrow = EditorGUIUtility.Load(SISSData.ICONS_DIR + "/arrow_down.png") as Texture2D;

			UnityEngine.GUI.DrawTexture(area, arrow, ScaleMode.ScaleToFit, true, 1, color, 0, 0);
		}

		public static void SeparatorLine()
		{
			Rect r = EditorGUI.IndentedRect(EditorGUILayout.BeginHorizontal());
			EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2), new Color(0, 0, 0, 0.2f));
			EditorGUILayout.Separator();
			EditorGUILayout.EndHorizontal();
		}
	}
}
