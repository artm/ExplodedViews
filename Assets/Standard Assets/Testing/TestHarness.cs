using System;
using System.Collections.Generic;
using System.Reflection;

using Debug = UnityEngine.Debug;

namespace Test {
	public class Harness : IDisposable
	{
		int tests_run, tests_succeded, tests_failed;
		int asserts_run, asserts_succeded, asserts_failed;

		public Harness(params Type[] suite)
		{
			tests_run = tests_succeded = tests_failed = 0;
			asserts_run = asserts_succeded = asserts_failed = 0;
			foreach(Type type in suite) {
				if (!type.IsSubclassOf(typeof(Test.Case))) {
					Debug.LogError(string.Format("{0} is not a Test.Case", type.FullName));
					continue;
				}

				Type[] noArgs = {};
				ConstructorInfo ctor = type.GetConstructor(noArgs);
				// find and run all test methods
				foreach(MethodInfo method in type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)) {
					if (method.Name.Substring(0,4).ToLower() == "test") {
						try {
							Case test_module = ctor.Invoke(null) as Case;
							try {
								method.Invoke(test_module, null);
								if (test_module.AssertsFailed > 0)
									tests_failed++;
								else
									tests_succeded++;
							} catch(Exception ex) {
								Debug.LogError(string.Format("Test {0} in {1} failed: {2}\n{3}\n{4}\n",
								                             method.Name, type.FullName, ex.Message,
								                             ex.GetType().FullName, ex.StackTrace));
								tests_failed++;
							} finally {
								tests_run++;
								asserts_run += test_module.AssertsRun;
								asserts_succeded += test_module.AssertsSucceeded;
								asserts_failed += test_module.AssertsFailed;
								test_module.Dispose();
							}
						} catch (Exception ex) {
							Debug.LogError(string.Format("Constructing {0} failed: {1}\n{2}\n{3}\n",
							                             type.FullName, ex.Message,
							                             ex.GetType().FullName, ex.StackTrace));
							break;
						}

					}
				}
			}
		}

		public void Run() {
			Dispose();
		}

		public void Dispose()
		{
			string test_report = string.Format("{0} tests run, {1} tests succeeded, {2} tests failed",
			                                   tests_run, tests_succeded, tests_failed);
			if (tests_run != tests_succeded + tests_failed)
				Debug.LogError("Test.Harness internal inconsistency: " + test_report);
			else
				Debug.Log(test_report);

			string assert_report = string.Format("{0} asserts run, {1} asserts succeeded, {2} asserts failed",
			                                   asserts_run, asserts_succeded, asserts_failed);
			if (asserts_run != asserts_succeded + asserts_failed)
				Debug.LogError("Test.Harness internal inconsistency: " + assert_report);
			else
				Debug.Log(assert_report);

		}
	}
}
