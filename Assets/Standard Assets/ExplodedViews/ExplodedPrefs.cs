using UnityEngine;

public class ExplodedPrefs : ScriptableObject
{
	public string importedPath, incomingPath;

	static ExplodedPrefs instance = null;
	public static ExplodedPrefs Instance {
		get {
			if (instance == null)
				instance = Resources.Load("ExplodedPrefs") as ExplodedPrefs;
			return instance;
		}
	}
}

