using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SISSData))]
public class SISSDataCustomEditor : Editor
{

	private bool allStepsFoldout = true;
	private bool projectViewSettingsFoldout = true;
	private bool renameRulesFoldout = true;

	public override void OnInspectorGUI()
	{
		// get the element this inspector draws for:
		SISSData sisd = (SISSData)target;

		// ----- Rename Rules ----- //
		renameRulesFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(renameRulesFoldout, "Rename Rules");
		EditorGUILayout.EndFoldoutHeaderGroup();
		if (renameRulesFoldout)
		{
			EditorGUI.indentLevel++;
			DrawRenameRules(sisd, sisd.renameRules);
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();

		// ----- Project View Settings ----- //
		projectViewSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(projectViewSettingsFoldout, "Project View Settings");
		if (projectViewSettingsFoldout)
		{
			EditorGUI.indentLevel++;
			DrawProjectViewSettings(sisd);
			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();

		// ----- Project View Settings ----- //
		allStepsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(allStepsFoldout, "Underlying Steps Structure");
		EditorGUILayout.EndFoldoutHeaderGroup();
		if (allStepsFoldout)
		{
			EditorGUI.indentLevel++;
			DrawUnderlyingSteps();
			EditorGUI.indentLevel--;
		}

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();

		sisd.printSpecialImporterDebugLogs = EditorGUILayout.Toggle("Print Special Importer Debug Log", sisd.printSpecialImporterDebugLogs);

		EditorGUILayout.Separator();
		EditorGUILayout.Separator();


		// ----- Buttons ----- //

		if (GUILayout.Button("Open Special Import Steps Menu"))
			SpecialImportStepInspector.ShowWindow();

		if (GUILayout.Button("Nuke Data"))
			SimpleConfirmPopup.Show(result =>
			{
				if (result)
					sisd.NukeData();
			});

		serializedObject.ApplyModifiedProperties();
	}

	private void DrawRenameRules(SISSData sisd, RenameRules renameRules)
	{
		bool previouslyEnabled = GUI.enabled;
		GUI.enabled = false;
		sisd.fileOrFolderWildcard = EditorGUILayout.TextField(new GUIContent("File or Folder name Wildcard", "The symbols used to symbolise the name of a file or folder."), sisd.fileOrFolderWildcard);
		sisd.multipleFolderWildcard = EditorGUILayout.TextField(new GUIContent("Multiple Folder Wildcard", "The symbols used to symbolise any number of folders."), sisd.multipleFolderWildcard);
		GUI.enabled = previouslyEnabled;


		serializedObject.ApplyModifiedProperties();

		//EditorGUILayout.Separator();
		//EditorGUILayout.LabelField("Replace Keywords:", style: EditorStyles.boldLabel);

		//renameRules.fileNameKeyword = EditorGUILayout.TextField("File Name Keyword:", renameRules.fileNameKeyword);
		//renameRules.extensionKeyword = EditorGUILayout.TextField("Extension Keyword:", renameRules.extensionKeyword);
		//renameRules.pathKeyword = EditorGUILayout.TextField("Path Keyword:", renameRules.pathKeyword);
		//renameRules.typeKeyword = EditorGUILayout.TextField("Type Keyword:", renameRules.typeKeyword);

		//EditorGUILayout.Separator();
		//EditorGUILayout.LabelField("Prefix and Suffix Definitions:", style: EditorStyles.boldLabel);

		//renameRules.suffixPrefixSeparator = EditorGUILayout.TextField(new GUIContent("Suffix & Prefix Separator:", "The symbol(s) used to seperate name from suffix.\nFor example '_' would lead to something like \"ModelName_normal\" for the normal map."), renameRules.suffixPrefixSeparator);


		EditorGUILayout.PropertyField(this.serializedObject.FindProperty("renameRules"));

		if (GUILayout.Button("Load Default Suffix Varations"))
			SimpleConfirmPopup.Show(result =>
			{
				if (result)
				{
					renameRules.LoadDefaultSuffixVarations();
					EditorUtility.SetDirty(sisd);
					AssetDatabase.SaveAssetIfDirty(sisd);
				}
			});
	}

	private void DrawProjectViewSettings(SISSData sisd)
	{
		sisd.drawInProjectView = EditorGUILayout.Toggle("Draw In Project View", sisd.drawInProjectView);
		sisd.showTooltip = EditorGUILayout.Toggle("Show Tooltip", sisd.showTooltip);
		EditorGUILayout.Separator();
		sisd.iconColor = EditorGUILayout.ColorField("Icon Color", sisd.iconColor);
		sisd.guidStepColor = EditorGUILayout.ColorField("GUID Step Color", sisd.guidStepColor);
		sisd.pathStepColor = EditorGUILayout.ColorField("Path Step Color", sisd.pathStepColor);
		sisd.overlayOpacity = EditorGUILayout.Slider("Overlay Color Opacity", 0f, 1f, sisd.overlayOpacity);
		sisd.suffixColor = EditorGUILayout.ColorField("Suffix Color", sisd.suffixColor);
	}

	private void DrawUnderlyingSteps()
	{
		EditorGUILayout.PropertyField(this.serializedObject.FindProperty("objectSteps"));
		EditorGUILayout.PropertyField(this.serializedObject.FindProperty("pathSteps"));
	}

	private void DrawGUIDStepRepresentation(SISSData sisd)
	{
		// ----- Bad Visualization of the steps ----- //
		string stringList = "";
		if (sisd.ObjectSteps != null || sisd.ObjectSteps.Count != 0)
		{
			foreach (string targetGUID in sisd.ObjectSteps.Keys)
			{
				stringList += sisd.ObjectSteps[targetGUID].Values[0].TargetName + ":\n";
				foreach (SpecialImportStep step in sisd.ObjectSteps[targetGUID].Values)
					stringList += "  " + step.ToString() + "\n";
			}
		}
		else
			stringList = "[List is Empty]";



		bool previouslyEnabled = GUI.enabled;
		GUI.enabled = false;

		EditorGUILayout.TextArea(stringList);

		GUI.enabled = previouslyEnabled;
	}
}
