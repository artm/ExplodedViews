using UnityEngine;

public class TemporaryObject_Test : Test.Case
{
	GameObject obj;

	public TemporaryObject_Test()
	{
	}

	public override void Dispose()
	{
	}

	void Test_using() {
		using (TemporaryObject tmp = new TemporaryObject(obj = new GameObject())) {
			Assert_True( obj );
		}
		Assert_False( obj );
	}

	void Test_CleanupOnException() {
		try {
			using (TemporaryObject tmp = new TemporaryObject(obj = new GameObject())) {
				Assert_True( obj );
				throw new System.Exception();
			}
		} catch (System.Exception) {
			// by now the object is destroyed already
			Assert_False( obj );
		} finally {
			// and here too
			Assert_False( obj );
		}
	}

	void Test_ManualDisposal() {
		TemporaryObject tmp = new TemporaryObject(obj = new GameObject());
		Assert_True( obj );
		tmp.Dispose();
		Assert_False( obj );
	}

	void Test_Leak() {
		using (TemporaryObject tmp = new TemporaryObject(obj = new GameObject())) {
			Assert_True( obj );
			tmp.Leak();
		}
		Assert_True( obj );
		// force cleanup
		Object.DestroyImmediate( obj );
		Assert_False( obj );
	}
}

