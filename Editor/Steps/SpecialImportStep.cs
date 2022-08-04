using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class SpecialImportStep
{
	[SerializeField]
	public SpecialImportTarget target = null;

	public bool enabled = true;
	public int priority = 0;

	private string targetName = null;
	public string TargetName
	{
		get
		{
			if (string.IsNullOrWhiteSpace(targetName))
			{
				Object parent = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(new GUID(target.guid)));
				if (parent == null)
					return "[COULD NOT LOAD NAME]";
				targetName = parent.name;
			}

			return targetName;
		}
	}
	// a varaible for the gui that remembers if the GUI can currently edit the settings
	protected bool GUI_canEdit = false;

	public SpecialImportStep() { }
	public SpecialImportStep(SpecialImportTarget target)
	{
		this.target = target;
		this.target.SetAllTypes(GetWorksForTypesList());
	}

	public override string ToString()
	{
		return "Special Import Step Example that does nothing";
	}

	public virtual string GetName()
	{
		return "Empty Special Import Step";
	}

	/// <summary> Returns true if this step can be applied to at least one of the types given. Is hardcoded into every class. </summary>
	public virtual List<Type> GetWorksForTypesList()
	{
		return new List<Type>() { }; // this step works for nothing, ever other step can use part of the following:
									 //return new List<Type>() { typeof(GameObject), typeof(Mesh), typeof(Material), typeof(AnimationClip), typeof(Texture), typeof(AudioClip) };
	}

	public bool WorksForOneOfTypes(List<Type> checkTypes)
	{
		if (checkTypes == null || checkTypes.Count == 0)
			return false;

		List<Type> worksForTypes = GetWorksForTypesList();


		for (int i = 0; i < checkTypes.Count; i++)
		{
			if (checkTypes[i] != null)
				if (worksForTypes.FindAll((t) => checkTypes[i] == t || checkTypes[i].IsSubclassOf(t)).Count > 0)
				return true;
		}
		return false;
	}

	/// <summary>
	/// GUI to display when creating this step.
	/// </summary>
	/// <returns><c>false</c> while the user is editing it and <c>true</c> once creation is finished or canceled.</returns>
	public virtual bool DrawGUI(bool inCreation)
	{
		EditorGUILayout.LabelField("Example Special Import Step");

		return DrawAddRemoveGUI(inCreation);
	}

	public Rect GUIBeginDefault(bool inCreation)
	{
		EditorGUILayout.Separator();
		Rect stepAreaRect = EditorGUILayout.BeginHorizontal(); // for some space on the right
		EditorGUILayout.BeginVertical(); // to encapsulat the whole UI to draw a box around
		EditorGUILayout.Separator();

		// draw box
		stepAreaRect = EditorGUI.IndentedRect(stepAreaRect);
		SISSUtils.GUI.DrawBox(stepAreaRect, target.IsGuidSet());

		EditorGUI.indentLevel++;

		// header + origin
		EditorGUILayout.BeginHorizontal();
		{
			enabled = EditorGUILayout.ToggleLeft(GetName() + ":", enabled, EditorStyles.boldLabel);

			if (target.IsGuidSet() == false)
			{
				if (GUILayout.Button("Step Origin", options: GUILayout.MaxWidth(100)))
				{
					Object origin = AssetDatabase.LoadAssetAtPath<Object>(target.FolderTargetPath);
					EditorGUIUtility.PingObject(origin);
					Selection.activeObject = origin;
				}
			}
			else
			{
				GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
				EditorGUILayout.LabelField("Original Step", style);
			}
		}
		EditorGUILayout.EndHorizontal();
		SISSUtils.GUI.SeparatorLine();

		EditorGUI.BeginDisabledGroup(!(inCreation || GUI_canEdit));

		priority = EditorGUILayout.IntField(new GUIContent("Priority:", "Determines what order this step should be executed.\nLower numbers = earlier, higher numbers = later."), priority);

		// draw type dropdown and path or guid
		EditorGUILayout.BeginHorizontal();
		{
			if (target.IsGuidSet() == false)
				target.path = SISSUtils.GUI.SyntaxHighlightTextField("Target Path", target.path, "A path that targets all assets this steps should be applied to. The Wildcard * is also possible.", true, false);
			else
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.TextField(new GUIContent("GUID:", "This step is applied specifically for this asset using its GUID.\nEven if the asset is moved or renamed this step still applies."), target.guid);
				EditorGUI.EndDisabledGroup();
			}

			if (EditorGUILayout.DropdownButton(new GUIContent(target.GetStringOfTargets(GetWorksForTypesList())), FocusType.Keyboard))
			{
				GenericMenu menu = new GenericMenu();

				menu.AddItem(new GUIContent("Everything"), false, () => target.SetAllTypes(GetWorksForTypesList()));
				menu.AddItem(new GUIContent("Nothing"), false, () => target.ClearAllTypes());
				menu.AddSeparator("");

				List<Type> worksForTypes = GetWorksForTypesList();
				for (int i = 0; i < worksForTypes.Count; i++)
				{
					Type t = worksForTypes[i];
					menu.AddItem(new GUIContent(SISSUtils.GetTypeName(t)), target.TargetsContainType(t), () => target.SwitchType(t));
				}

				menu.ShowAsContext();
			}
		}
		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();

		return stepAreaRect;
	}
	public void GUIEndDefault()
	{
		EditorGUI.EndDisabledGroup();
		EditorGUI.indentLevel--;

		EditorGUILayout.Separator();
		EditorGUILayout.EndVertical();

		EditorGUILayout.LabelField("", options: GUILayout.Width(5));

		EditorGUILayout.EndHorizontal();
		EditorGUILayout.Separator();
	}
	public bool DrawAddRemoveGUI(bool inCreation)
	{
		if (inCreation)
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("", options: GUILayout.ExpandWidth(true)); // to force the add button to the right side

			if (GUILayout.Button("Add", options: GUILayout.MaxWidth(100)))
			{
				OnFinishEdit();
				SISSData.Instance.AddStep(this);
				GUI_canEdit = false;
				return true;
			}
			if (GUILayout.Button("Cancel", options: GUILayout.MaxWidth(100)))
				return true;
			EditorGUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("", options: GUILayout.ExpandWidth(true)); // to force the buttons to the right side

			if (GUILayout.Button(GUI_canEdit ? "Save" : "Edit", options: GUILayout.MaxWidth(100)))
			{
				if (GUI_canEdit)
					OnFinishEdit();
				GUI_canEdit = !GUI_canEdit;
				SISSData.Instance.Save();
			}
			if (GUILayout.Button("Remove", options: GUILayout.MaxWidth(100)))
				SISSData.Instance.RemoveStep(this);


			EditorGUILayout.EndHorizontal();
		}

		return false;
	}

	protected virtual void OnFinishEdit()
	{
		if (target.TargetsNoTypes())
		{
			enabled = false;
			Debug.LogWarning("Special Import Step " + GetName() + " targets no type. The step is disabled until that is changed.");
		}
	}

	/// <summary>
	/// Called on import and enables this Object to apply the special import step this class implements.
	/// </summary>
	public virtual void Apply(Object importObject, UnityEditor.AssetImporters.AssetImportContext context, AssetImporter assetImporter)
	{
		Debug.Log("Example Import Step on " + TargetName);
	}
}
