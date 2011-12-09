using UnityEngine;
using System.Collections;
using System.Text;

public class Logger : MonoBehaviour {
	string[] log = new string[16];
	StringBuilder logAsText = new StringBuilder(1024);
	int waterMark = 0;
	bool filled = false;

	public bool on = false;
	public GUIStyle style;

	void _Log(string format, params object[] args)
	{
		log[waterMark++] = string.Format(format, args);
		if (waterMark == log.Length) {
			waterMark = 0;
			filled = true;
		}
		logAsText.Length = 0;
		if (filled) {
			for(int i = waterMark; i<log.Length; i++)
				logAsText.AppendFormat("{0}\n",log[i]);
		}
		for(int i=0; i<waterMark; i++)
			logAsText.AppendFormat("{0}\n",log[i]);
	}

	public string Text { get { return logAsText.ToString(); } }

	void OnGUI() {
		if (on) {
			GUILayout.BeginArea(new Rect(0,Screen.height/2,Screen.width,Screen.height/2));
			GUILayout.BeginHorizontal();
			// Log
			GUILayout.Label( Text, style );
			GUILayout.BeginVertical();
			// Stats
			GUILayout.Label("Delta: " + Time.deltaTime, style);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}

	void Update() {
		if (Input.GetKeyUp("space"))
			on = !on;
	}

	static Logger singleton = null;
	void Awake() {
		if (singleton != null) {
			Debug.LogError("Multiple loggers conflict!", this);
			Debug.LogError("Previously configured logger:", singleton);
		} else {
			singleton = this;
		}
	}

	public static void Log(string format, params object[] args)
	{
		singleton._Log(format, args);
	}
}
