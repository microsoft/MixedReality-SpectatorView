using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class RevertPropertyChanges : Attribute
{
	public string PropertyName = null;
	public Type TypeOfProperty;

	public RevertPropertyChanges(Type type, string propertyName)
	{
		if (type != typeof(float) && type != typeof(Vector3) && type != typeof(Color))
		{
			Debug.LogErrorFormat("RevertPropertyChanges with unsupported type: {0}", type);
			return;
		}
		this.PropertyName = propertyName;
		this.TypeOfProperty = type;
	}
}

