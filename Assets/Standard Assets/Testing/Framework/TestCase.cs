using System.Diagnostics;
using System.Text.RegularExpressions;

using UnityEngine;

namespace Test
{
	public abstract class Case : System.IDisposable
	{
		/// <summary>
		/// Test case set up should be done in parameterless constructor
		/// </summary>
		public Case()
		{
			asserts_run = asserts_succeeded = asserts_failed = 0;
		}

		/// <summary>
		/// Test case tear down should be done in an override of Dispose
		/// </summary>
		public abstract void Dispose();

		#region Asserts
		// each of the following should call assert()
		public void Assert_NotNull(Object obj)
		{
			assert( obj != null, () => string.Format("{0} is not null") );
		}
		public void Assert_Equal<T> (T expected, T actual)
			where T : System.IEquatable<T>
		{
			assert( expected.Equals(actual), () => string.Format("Expected {0}, got {1}", expected, actual) );
		}
		public void Assert_Approximately (float expected, float actual)
		{
			assert( Mathf.Approximately(expected, actual),
			       () => string.Format("Expected approx. {0}, got {1}", expected, actual));
		}
		public void Assert_Approximately (Vector3 expected, Vector3 actual)
		{
			assert(Mathf.Approximately(Vector3.Distance(expected, actual), 0),
			       () => string.Format("Expected approx. {0}, got {1}", expected, actual));
		}
		public void Assert_Approximately (Color expected, Color actual)
		{
			assert(Mathf.Approximately(expected.r, actual.r)
			       && Mathf.Approximately(expected.g, actual.g)
			       && Mathf.Approximately(expected.b, actual.b),
			       () => string.Format("Expected approx. {0}, got {1}", expected, actual));
		}

		public void Assert_Approximately (float expected, float actual, float maxdiff)
		{
			assert( Mathf.Abs(expected - actual) <= maxdiff,
			       () => string.Format("Expected {0} +/- {2}, got {1}", expected, actual, maxdiff));
		}
		public void Assert_Approximately (Vector3 expected, Vector3 actual, float maxdiff)
		{
			assert( Vector3.Distance(expected, actual) <= maxdiff,
			       () => string.Format("Expected {0} +/- {2}, got {1}", expected, actual, maxdiff));
		}
		public void Assert_Approximately (Color expected, Color actual, float maxdiff)
		{
			assert( Mathf.Abs(expected.r - actual.r) <= maxdiff
			       && Mathf.Abs(expected.g - actual.g) <= maxdiff
			       && Mathf.Abs(expected.b - actual.b) <= maxdiff,
			       () => string.Format("Expected {0} +/- {2}, got {1}", expected, actual, maxdiff));
		}


		public void Assert_True(bool boolean)
		{
			assert( boolean, () => "Expected True, got False" );
		}
		public void Assert_False(bool boolean)
		{
			assert( !boolean, () => "Expected False got True" );
		}
		#endregion

		#region Assertion Stats (used by the harness)
		public int AssertsRun { get {return asserts_run; } }
		public int AssertsFailed { get {return asserts_failed; } }
		public int AssertsSucceeded { get {return asserts_succeeded; } }
		#endregion

		#region Implementation details
		int asserts_run, asserts_succeeded, asserts_failed;
		delegate string AssertionTextGenerator();
		void assert(bool assertion, AssertionTextGenerator genText) {
			asserts_run++;
			if (assertion)
				asserts_succeeded++;
			else {
				asserts_failed++;
				StackTrace st = new StackTrace(2, true); // minus assert() and Assert_...()
				StackFrame sf = st.GetFrame(0);

				/*
				 * If you got here by double clicking on the error message in Unity's console, you probably wanted to
				 * get to the failed test. Look at test / filename / line number reported in the Log. Unfortunatelly
				 * we can't get you there automatically.
				 */
				UnityEngine.Debug.LogError(string.Format("Assertion {0} in {1} (at {2}:{3}) failed\n",
				                                         // extra new line pushes Unity's stack trace reporting down
				                                         genText(),
				                                         sf.GetMethod(),
				                                         Regex.Replace(sf.GetFileName(), "^.*/(?=Assets/)",""),
				                                         sf.GetFileLineNumber()));
			}
		}
		#endregion
	}
}

