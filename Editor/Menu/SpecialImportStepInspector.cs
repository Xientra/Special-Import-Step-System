using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SpecialImportStepInspector : EditorWindow
{
	[MenuItem("Assets/Special Import Step Inspector", priority = 100020)]
	[MenuItem("Window/Special Import Step Inspector", priority = 100021)]
	public static void ShowWindow()
	{
		SpecialImportStepInspector window = EditorWindow.GetWindow(typeof(SpecialImportStepInspector), false, "Special Import Step Inspector", true) as SpecialImportStepInspector;
		window.newStep = null;
	}

	private string targetPathSuggestion = "";
	private SpecialImportStep newStep = null;
	private Vector2 scroll;

	private void OnSelectionChange()
	{
		// ----- get a lot of data ----- //
		Object selectedAsset = Selection.activeObject;
		string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
		//Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

		if (AssetDatabase.IsValidFolder(assetPath))
			targetPathSuggestion = assetPath;
	}

	private void OnGUI()
	{
		if (newStep != null && newStep.target.IsEmpty())
			newStep = null;

		scroll = EditorGUILayout.BeginScrollView(scroll);

		// ----- get a lot of data ----- //
		Object selectedAsset = Selection.activeObject;
		string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
		Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);

		// ----- DrawStepCreation ----- //
		EditorGUI.indentLevel++;

		// check if something is currently selected in the Hierarchy View (and not the scene or something)
		if (selectedAsset != null && AssetDatabase.Contains(selectedAsset))
		{
			EditorGUILayout.ObjectField(Selection.activeObject, typeof(GameObject), false);

			if (AssetDatabase.IsValidFolder(assetPath))
			{
				DrawFolderStepCreation(selectedAsset, assetPath); // Folder Menu
			}
			else
			{
				DrawGuidStepCreation(selectedAsset, assetPath, subAssets); // Asset Menu
			}
		}
		EditorGUI.indentLevel--;

		EditorGUILayout.EndScrollView();

		this.Repaint();
	}

	private void DrawGuidStepCreation(Object selectedAsset, string assetPath, Object[] subAssets)
	{
		// ----- get data ----- //
		GUID assetGUID = AssetDatabase.GUIDFromAssetPath(assetPath);
		GameObject parentObject = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

		if (subAssets == null)
			subAssets = new Object[0];

		List<Type> types = new List<Type>(new HashSet<Type>(new List<Object>(subAssets).FindAll(sa => sa != null).ConvertAll(sa => sa.GetType())));
		types.Add(selectedAsset.GetType());

		List<SpecialImportStep> stepsForThisAsset = SISSData.Instance.GetStepsForAsset(assetGUID.ToString(), assetPath, types.ToArray());


		// ----- Selected Asset Hierachy ----- //
		if (selectedAsset is GameObject)
		{
			EditorGUI.indentLevel++;
			ShowObject(parentObject, assetPath, subAssets, stepsForThisAsset);
			EditorGUI.indentLevel--;
		}

		// ----- List of Steps for this Asset ----- //
		EditorGUILayout.LabelField("Steps for this Asset:");


		EditorGUI.indentLevel++;
		{
			DrawStepsView(stepsForThisAsset);

			EditorGUILayout.Separator();

			if (SISSUtils.GUI.CenteredButton("Add Step"))
				GetStepsDropdown(assetGUID.ToString(), types).ShowAsContext();
		}
		EditorGUI.indentLevel--;
	}

	private void DrawFolderStepCreation(Object selectedAsset, string assetPath)
	{
		EditorGUILayout.LabelField("Path Steps:");

		List<SpecialImportStep> stepsOriginatingFromThisFolder = SISSData.Instance.GetStepsForFolder(assetPath);

		// draw step currently in creation (and stop creating once DrawCreationGUI returns true
		EditorGUI.indentLevel++;
		{
			DrawStepsView(stepsOriginatingFromThisFolder);

			EditorGUILayout.Separator();

			if (SISSUtils.GUI.CenteredButton("Add Path Step"))
				GetStepsDropdown(targetPathSuggestion, null).ShowAsContext();
		}
		EditorGUI.indentLevel--;
	}

	private void DrawStepsView(List<SpecialImportStep> steps)
	{
		// draw existing steps
		for (int i = 0; i < steps.Count; i++)
		{
			steps[i].DrawGUI(false);

			// draw arrow
			if (i != steps.Count - 1 || newStep != null)
			{
				Rect arrowBox = EditorGUILayout.BeginVertical();
				EditorGUILayout.LabelField("", options: GUILayout.Width(5));
				SISSUtils.GUI.DrawArrowDown(arrowBox, Color.gray);
				EditorGUILayout.EndVertical();
			}
		}

		// draw step currently in creation (and stop creating once DrawCreationGUI returns true)
		if (newStep != null)
			if (newStep.DrawGUI(true))
				newStep = null;
	}

	private void ShowObject(Object parentObj, string assetPath, Object[] objects, List<SpecialImportStep> steps)
	{
		// Show entry for parent object
		EditorGUILayout.LabelField(parentObj.name + " (" + SISSUtils.GetTypeName(parentObj.GetType()) + ")");

		EditorGUI.indentLevel++;

		GUIStyle guiS = EditorStyles.label;
		guiS.richText = true;


		for (int i = 0; i < objects.Length; i++)
		{
			bool drawColor = steps.FindAll(sis => sis.target.IsSubAssetTarget(objects[i].name, assetPath, objects[i].GetType())).Count > 0;
			string color = ColorUtility.ToHtmlStringRGB(SISSData.Instance.guidStepColor);

			string label = objects[i].name + " (" + SISSUtils.GetTypeName(objects[i].GetType()) + ")";
			if (drawColor)
				label = "<color=#" + color + ">" + label + "</color>";

			EditorGUILayout.LabelField(label);
		}

		EditorGUI.indentLevel--;
	}

	/// <summary>
	/// Creates a generic menu with options for all for this instance applicable <see cref="SpecialImportStep"/>.
	/// </summary>
	/// <param name="guidOrPath">A string that either hold information about the GUID of the target asset or the path of the target folder.<br/>
	/// This difference depends on what is given for types.</param>
	/// <param name="types">When not null or empty the function assumes a GUID in guidOrPath and shows only steps applicable to the types given.<br/>
	/// Otherwise all types are shown and guidOrPath is assumed to hold a path.</param>
	private GenericMenu GetStepsDropdown(string guidOrPath, List<Type> types)
	{
		bool forFolder = types == null || types.Count == 0;

		List<SpecialImportStep> menuSteps = new List<SpecialImportStep>();

		// this can probably be done in a robust way using reflections
		menuSteps.Add(new SpecialImportCreatePrefab(GetTargetObject(guidOrPath, forFolder)));
		menuSteps.Add(new SpecialImportCreateStandardMaterial(GetTargetObject(guidOrPath, forFolder)));
		menuSteps.Add(new SpecialImportMoveAsset(GetTargetObject(guidOrPath, forFolder)));
		menuSteps.Add(new SpecialImportPostprocessMesh(GetTargetObject(guidOrPath, forFolder)));
		menuSteps.Add(new SpecialImportRenameFromTo(GetTargetObject(guidOrPath, forFolder)));
		menuSteps.Add(new SpecialImportUnifySuffix(GetTargetObject(guidOrPath, forFolder)));
		//menuSteps.Add(new MeasureDimensions(GetTargetObejct(guidOrPath, forFolder)));

		GenericMenu menu = new GenericMenu();

		for (int i = 0; i < menuSteps.Count; i++)
		{
			SpecialImportStep sis = menuSteps[i];
			if (forFolder || sis.WorksForOneOfTypes(types))
				menu.AddItem(new GUIContent(sis.GetName()),
					newStep != null && newStep.GetType() == sis.GetType(), // check to enable checkmark
					() => newStep = sis); // on click
		}

		return menu;
	}

	private SpecialImportTarget GetTargetObject(string guidOrPath, bool isPath)
	{
		if (isPath)
			return new SpecialImportTarget(guidOrPath, "");
		else
			return new SpecialImportTarget(guidOrPath);
	}
}
