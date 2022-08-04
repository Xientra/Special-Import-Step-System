using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A Unity seralizable dictionary using <see cref="ISerializationCallbackReceiver"/> from <see href="http://answers.unity.com/answers/809221/view.html">this page</see>.
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
	[SerializeField]
	private List<TKey> keys = new List<TKey>();

	[SerializeField]
	private List<TValue> values = new List<TValue>();

	// save the dictionary to lists
	public void OnBeforeSerialize()
	{
		keys.Clear();
		values.Clear();
		foreach (KeyValuePair<TKey, TValue> pair in this)
		{
			keys.Add(pair.Key);
			values.Add(pair.Value);
		}
	}

	// load dictionary from lists
	public void OnAfterDeserialize()
	{
		this.Clear();

		if (keys.Count != values.Count)
			throw new System.Exception(string.Format("there are " + keys.Count + " keys and " + values.Count + " values after deserialization. Make sure that both key and value types are serializable."));

		for (int i = 0; i < keys.Count; i++)
			this.Add(keys[i], values[i]);
	}
}

/// <summary>
/// A Unity-serializable and non generic Dictionary inheriting from <see cref="SerializableDictionary{TKey, TValue}"/> that maps from <see cref="string"/> to <see cref="SpecialImportStepList"/>.
/// </summary>
[Serializable]
public class StepsDictionary : SerializableDictionary<string, SpecialImportStepList> { }

/// <summary>
/// A Unity-serializable wrapper for a List&lt;<see cref="SpecialImportStep"/>&gt;
/// </summary>
[Serializable]
public class SpecialImportStepList : ISerializationCallbackReceiver
{
	[SerializeField]
	private List<SerializableSpecialImportStep> savedValues;

	private List<SpecialImportStep> values;
	public List<SpecialImportStep> Values
	{
		get
		{
			if (values == null)
				values = new List<SpecialImportStep>();
			return values;
		}
	}

	public void OnBeforeSerialize()
	{
		savedValues = values.ConvertAll(sis => new SerializableSpecialImportStep(sis));
	}

	public void OnAfterDeserialize()
	{
		values = savedValues.ConvertAll(ssis => ssis.DeserializeStep());
	}
}
