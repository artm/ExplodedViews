using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Progressor
{
    StopWatch swatch;
    string title;
    float lastProgress, lastElasped;

    Progressor stack = null;
    float progFrom, progTo;

	// used by Iterate<> and Sub() version without arguments
	class IterState {
		public int i;
		public int total;
		public IterState() { i=0; total=0; }
	}
	IterState iter = new IterState();

    public float elapsed { get { return swatch.elapsed; } }

    public class Cancel : System.ApplicationException
    {
        public Cancel() : base("Canceled by user") {}
    }

    public Progressor(string title)
    {
        this.title = title;
        swatch = new StopWatch();
        lastProgress = 0f;
        lastElasped = 0f;
    }

    public Progressor(Progressor stack, float progFrom, float progTo)
    {
        this.stack = stack;
        this.progFrom = progFrom;
        this.progTo = progTo;
		this.swatch = stack.swatch;
    }

    public Progressor Sub(float progFrom, float progTo)
    {
        return new Progressor(this, progFrom, progTo);
    }

	/// <summary>
	/// Sub-progressor for iterating progressor
	/// </summary>
	public Progressor Sub()
	{
		return Sub( (float)iter.i / (float)iter.total , (float)(iter.i+1) / (float)iter.total );
	}

	/// <summary>
	/// Advance progress bar
	/// </summary>
	/// <param name="progress">
	/// The measure of progress - from 0.0f to 1.0f.
	/// </param>
	/// <param name="format">
	/// Current message format. May contain {eta} token which will be
	/// substituted with estimated time until the end of operation.
	/// </param>
	/// <param name="args">
	/// Values for format (as per System.String.Format).
	/// </param>
    public void Progress(float progress, string format, params object[] args)
    {
        if (stack != null) {
            stack.Progress(progFrom + progress*(progTo-progFrom), format, args);
        } else {
            if (((progress-lastProgress) < 0.01) && ((elapsed-lastElasped) < 1.0))
                return; // don't bother

            lastProgress = progress;
            lastElasped = elapsed;

            format = format.Replace("{eta}", Pretty.Seconds(swatch.ETA(progress)));
            if (EditorUtility.DisplayCancelableProgressBar(title,string.Format(format, args), progress))
                throw new Cancel();
        }
    }

	public delegate string Printer<T>(T obj);

	public IEnumerable<T> Iterate<T>(IEnumerable<T> collection, Printer<T> print) {
		T[] array = collection.ToArray();
		try {
			iter.total = array.Length;

			for(iter.i = 0; iter.i<iter.total; ++iter.i) {
				T obj = array[iter.i];
				Progress((float) iter.i / (float) iter.total, "Processing {0}, ETA: {eta}", print(obj));
				yield return obj;
			}
		} finally {
			Done();
		}
	}
	public IEnumerable<T> Iterate<T>(IEnumerable<T> collection) {
		return Iterate<T>(collection, x => x.ToString());
	}

    public void Done()
    {
        if (stack == null)
            EditorUtility.ClearProgressBar();
    }

    /// <summary>
    /// Clears progress bar and logs the last words according to format.
    /// </summary>
    /// <param name='format'>
    /// Last words format. May contain {tt} token which will be substituted with total elapsed time.
    /// </param>
    /// <param name='lastWords'>
    /// Last words contents.
    /// </param>
    public void Done(string format, params object[] lastWords)
    {
        format = format.Replace("{tt}",Pretty.Seconds(elapsed));
        Debug.Log(string.Format(format, lastWords));
        Done();
    }
}
#endif