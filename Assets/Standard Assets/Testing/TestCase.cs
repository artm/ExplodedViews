using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Test
{
	public abstract class Case : IDisposable
	{
		int asserts_run, asserts_succeeded, asserts_failed;

		public int AssertsRun { get {return asserts_run; } }
		public int AssertsFailed { get {return asserts_failed; } }
		public int AssertsSucceeded { get {return asserts_succeeded; } }

		public Case()
		{
			asserts_run = asserts_succeeded = asserts_failed = 0;
		}

		public abstract void Dispose();

		void assert(bool assertion) {
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
				UnityEngine.Debug.LogError(string.Format("Assertion in {0} (at {1}:{2}) failed\n",
				                                         // extra new line pushes Unity's stack trace reporting down
				                                         sf.GetMethod(),
				                                         Regex.Replace(sf.GetFileName(), "^.*/(?=Assets/)",""),
				                                         sf.GetFileLineNumber()));
			}
		}

		// each of the following should call assert()
		public void Assert_NotNull(Object obj) { assert( obj != null ); }
		public void Assert_Equal<T> (T a, T b) where T : IEquatable<T> { assert( a.Equals(b) ); }
		public void Assert_True(bool boolean) { assert( boolean ); }

	}
}

