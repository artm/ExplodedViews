using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class Logger : MonoBehaviour {
	string[] log = new string[16];
	StringBuilder logAsText = new StringBuilder(1024);
	int waterMark = 0;
	bool filled = false;
	Dictionary<string, RingBufferPlot> plots = new Dictionary<string, RingBufferPlot>();
	Dictionary<string, string> states = new Dictionary<string, string>();

	public bool on = false;
	public GUIStyle style;
	public int plotWidth = 64, plotHeight = 32;

	class RingBuffer : IEnumerable<float>
	{
		float[] buffer;
		int len, idx;
		public RingBuffer(int capacity) {
			buffer = new float[ capacity ];
			len = idx = 0;
		}
		public void Write(float val) {
			buffer[idx++] = val;
			if (idx >= buffer.Length) idx=0;
			if (len <  buffer.Length) len++;
		}
		public void Shrink() {
			if (len>0) len--;
		}
		public IEnumerator<float> GetEnumerator()
		{
			for(int i = idx; i<len; i++)
				yield return buffer[i];
			for(int i = 0; i<idx; i++)
				yield return buffer[i];
		}
		System.Collections.IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
	}

	class RingBufferPlot
	{
		RingBuffer buffer;
		Texture2D texture;
		Color32[] pixels;
		string minFmt, maxFmt;

		public static bool update = true;
		public Color32 bgColor = new Color32(0,0,0,128);
		public Color32 color = new Color32(128,255,128,128);

		float min, max;
		public float Min { get {return min;}}
		public float Max { get {return max;}}
		public RingBufferPlot(int width, int height, string label, params string[] fmt)
		{
			texture = new Texture2D(width,height,TextureFormat.RGBA32, false);
			pixels = new Color32[width*height];
			buffer = new RingBuffer(width);
			min = Mathf.Infinity;
			max = Mathf.NegativeInfinity;

			string format = fmt.Length > 0 ? fmt[0] : "{0}";
			maxFmt = label + " max: " + format;
			minFmt = "min: " + format;
		}
		public void Write(float val) {
			buffer.Write(val);
			// FIXME: make sure texture is updated only when necessary
			if (update)
				UpdateTexture();
		}
		public void Shrink() {
			buffer.Shrink();
			// FIXME: make sure texture is updated only when necessary
			if (update)
				UpdateTexture();
		}
		void UpdateTexture() {
			min = Mathf.Infinity;
			max = Mathf.NegativeInfinity;
			foreach(float val in buffer) {
				min = System.Math.Min(min, val);
				max = System.Math.Max(max, val);
			}
			for(int i = 0; i < pixels.Length; i++) {
				pixels[i] = bgColor;
			}
			int x = 0,y0 = 0;
			foreach(float val in buffer) {
				int y = Mathf.FloorToInt( (texture.height - 1) * (val - min) / (max - min) );
				if (x==0) y0 = y;
				while(y0<y) pixels[ x + texture.width*y0++ ] = color;
				while(y0>y) pixels[ x + texture.width*y0-- ] = color;
				pixels[ x + y*texture.width ] = color;
				x++;
			}
			texture.SetPixels32(pixels,0);
			texture.Apply();
		}
		public void Draw(GUIStyle style) {
			GUILayout.Label(string.Format(maxFmt, Pretty.Count(max)), style);
			GUILayout.Box(texture, style);
			GUILayout.Label(string.Format(minFmt, Pretty.Count(min)), style);
		}
	}

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

	void _Plot(string label, float val, params string[] fmt)
	{
		if (!plots.ContainsKey(label)) {
			plots[label] = new RingBufferPlot(plotWidth,plotHeight,label,fmt);
		}
		plots[label].Write(val);
	}
	
	void _State(string label, string state) {
		if (state != null)
			states[label] = state;
		else
			states.Remove(label);
	}
	
	public string Text { get { return logAsText.ToString(); } }

	Rect[] wrect = new Rect[1];
	void OnGUI() {
		if (on) {
			wrect[0] = GUILayout.Window(0,wrect[0],LoggerWindow,"ExplodedLogs");

		}
	}
	void LoggerWindow(int id) {
		GUILayout.BeginHorizontal();
		// Log
		GUILayout.Label( Text, style );
		GUILayout.BeginVertical();
		// States
		foreach(string label in states.Keys) {
			GUILayout.BeginHorizontal();
			GUILayout.Label( label, style );
			GUILayout.Label( states[label], style );
			GUILayout.EndHorizontal();
		}
		// Stats
		foreach(RingBufferPlot plot in plots.Values) {
			plot.Draw(style);
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUI.DragWindow();
	}
	
	void Update() {
		if (Input.GetKeyUp("space")) {
			on = !on;
			RingBufferPlot.update = on;
		}
		_Plot("dt",Time.deltaTime*1000f,"{0:##0.#} ms");
	}

	static Logger singleton = null;
	void Awake() {
		if (singleton != null) {
			Debug.LogError("Multiple loggers conflict!", this);
			Debug.LogError("Previously configured logger:", singleton);
		} else {
			singleton = this;
		}
		RingBufferPlot.update = on;
		wrect[0] = new Rect(0,Screen.height/2,10,10);
	}

	public static void Log(string format, params object[] args)
	{
		singleton._Log(format, args);
	}

	public static void Plot(string label, float val, params string[] fmt)
	{
		singleton._Plot(label, val, fmt);
	}
	
	public static void State(string label, string state) {
		singleton._State(label, state);
	}
}
