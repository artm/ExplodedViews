using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class Logger : MonoBehaviour {
	string[] log = new string[16];
	StringBuilder logAsText = new StringBuilder(1024);
	int waterMark = 0;
	bool filled = false;

	public bool on = false;
	public GUIStyle style;
	
	// Todo: abstract this away
	Texture2D dtPlot;
	Color32[] dtPlotPixels;
	float[] dtBuffer;
	float dtMin, dtMax;
	int dtBufferLen, dtBufferIdx;

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
			GUILayout.Label(string.Format("{0:##0.0} ms ({1:00} FPS)", 
			                              1000f*dtMax, 1f/dtMax), style);
			GUILayout.Box(dtPlot,style);
			GUILayout.Label(string.Format("{0:##0.0} ms ({1:00} FPS)", 
			                              1000f*dtMin, 1f/dtMin), style);
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}
	}
	
	IEnumerable<float> DtBuffer {
		get {
			for(int i = dtBufferIdx; i<dtBufferLen; i++)
				yield return dtBuffer[i];
			for(int i = 0; i<dtBufferIdx; i++)
				yield return dtBuffer[i];
		}
	}
	
	void Update() {
		if (Input.GetKeyUp("space"))
			on = !on;
		// log delta time
		dtBuffer[dtBufferIdx++] = Time.deltaTime;
		if (dtBufferIdx >= dtBuffer.Length) dtBufferIdx=0;
		if (dtBufferLen < dtBuffer.Length) dtBufferLen++;
		if (on) {
			// plot delta time
			bool started = false;
			dtMin = 0.0f;
			dtMax = 0.0f;
			foreach(float dt in DtBuffer) {
				if (!started) {
					dtMin = dtMax = dt;
					started = true;
				} else {
					if (dt < dtMin) dtMin = dt;
					if (dt > dtMax) dtMax = dt;
				}
			}
			for(int i = 0; i < dtPlotPixels.Length; i++) {
				dtPlotPixels[i] = new Color32(0,0,0,128);
			}
			int x = 0;
			foreach(float dt in DtBuffer) {
				int y = Mathf.FloorToInt( (dtPlot.height-1) * (dt-dtMin) / (dtMax-dtMin) );
				dtPlotPixels[ x + y*dtPlot.width ] = new Color32(0,255,0,255);
				x++;
			}
			dtPlot.SetPixels32(dtPlotPixels,0);
			dtPlot.Apply();
		}
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
	
	void Start() {
		dtPlot = new Texture2D(256,64);
		dtPlotPixels = new Color32[ dtPlot.width*dtPlot.height ];
		dtBuffer = new float[ dtPlot.width ];
		Debug.Log(string.Format("dtBuffer.Length: {0}", dtBuffer.Length));
		dtBufferLen = dtBufferIdx = 0;
	}

	public static void Log(string format, params object[] args)
	{
		singleton._Log(format, args);
	}
}
