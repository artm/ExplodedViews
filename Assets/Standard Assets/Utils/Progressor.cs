using UnityEngine;
using UnityEditor;
using System.Collections;

public class Progressor
{
    StopWatch swatch;
    string title;
    float lastProgress, lastElasped;

    Progressor stack = null;
    float progFrom, progTo;

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

