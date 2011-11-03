using UnityEngine;
using System.Collections;

/// <summary>
/// Measure elapsed and estimate remaining time.
/// </summary>
public class StopWatch
{
    const float epsilon = 1e-6f;
    float start;

	/// <summary>
	/// Initializes a new instance of the <see cref="StopWatch"/> class. The instance's stop watch starts.
	/// </summary>
    public StopWatch() {
        start = Time.realtimeSinceStartup;
    }

	/// <summary>
	/// Time elapsed since stop watch was created.
	/// </summary>
    public float elapsed {
        get { return Time.realtimeSinceStartup - start; }
    }

	/// <summary>
	/// Estimates remaining time given current progress.
	/// </summary>
	/// <param name='progress'>
	/// Progress: current progress in [0.0 : 1.0] range.
	/// </param>
    public float ETA(float progress) {
        return progress>epsilon ? elapsed*(1.0f/progress-1.0f) : -1.0f;
    }
}

