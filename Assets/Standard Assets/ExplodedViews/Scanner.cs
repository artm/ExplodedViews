// derived from code of myu toolkit, which is GPL licensed and has the following notice:
// TODO: get rid of their code and drop their copyright
//
// mu (myu) Max-Unity Interoperability Toolkit
// Ivica Ico Bukvic <ico@vt.edu> <http://ico.bukvic.net>
// Ji-Sun Kim <hideaway@vt.edu>
// Keith Wooldridge <kawoold@vt.edu>
// With thanks to Denis Gracanin
//
// Virginia Tech Department of Music
// DISIS Interactive Sound & Intermedia Studio
// Collaborative for Creative Technologies in the Arts and Design
//
// Copyright DISIS 2008.
// mu is distributed under the GPL license v3 (http://www.gnu.org/licenses/gpl.html)

using UnityEngine;
using System;
using System.Collections;
using System.Text;

[AddComponentMenu("Exploded Views/Scanner")]
public class Scanner : MonoBehaviour {
	
	public bool setCenterPoint = false;
	public bool enableScanner = true;
	public bool useSizeAsWeight = false;
	public float centerPointForward = 0.0f; /* in millimeters */
	public float centerPointSideways = 0.0f;
	public float measurementWeight = 0.2f;
	
	private ArrayList blobCoordsCartesian;
	private Vector3 lpCog;
	private bool hasStart;

	// Use this for initialization
	void Start () {		
		hasStart = false;
		lpCog = new Vector3(0,0,0);
		blobCoordsCartesian = new ArrayList();
	}

	char[] trimUs = " ;".ToCharArray();

	void NetReceive(String s) {
		
		if (!enableScanner) {
			return;
		}
				
		foreach(string line in s.Split('\n')) {
			string[] parms = line.TrimEnd(trimUs).Split(' ');
			
			if (parms.Length > 0) {
				if (String.Compare("blob", parms[0], true) == 0) {
					if (parms.Length == 4) {
						Vector3 coords = new Vector3(float.Parse(parms[2]), float.Parse(parms[1]), 1);
						
						if (useSizeAsWeight) {
							/* store blob size in z */
							coords.z = float.Parse(parms[3]);
						}
						
						blobCoordsCartesian.Add(coords);
						
					} else { 
						Debug.Log("Wrong blob payload; length " + parms.Length + "!=4");
					}	
				} else if (String.Compare("bang", parms[0], true) == 0) {
					blobCoordsCartesian.Clear();
					hasStart = true;	
				} else if (String.Compare("end", parms[0], true) == 0) {
					if (hasStart) {
						if (blobCoordsCartesian.Count > 0) {
							Vector3 result = new Vector3(0, 0, 0);
							

							foreach (Vector3 v in blobCoordsCartesian) {
								//Debug.Log("blob: " + v);

								Vector3 t = v;
								t.x *= v.z;
								t.y *= v.z;
								result += t;
							}
							
							/* result.z containing total mass, convert to meters */
							result = result / result.z;
							lpCog = lpCog * (1.0f - measurementWeight) + result * measurementWeight;
						
							if (setCenterPoint) {
								centerPointForward = lpCog.x;
								centerPointSideways = lpCog.y;
								setCenterPoint = false;
							}
							
							result.x = lpCog.x - centerPointForward;
							result.y = lpCog.y - centerPointSideways;
							
							hasStart = false;
								
							BroadcastMessage("BlobsCenterOfGravity", result, SendMessageOptions.DontRequireReceiver);							
						}
					} else {
						/* If end of list is reached without start condition we probably are missing
					       blobs. Ignore this list. */	
						Debug.Log("Incomplete blob list received");	
					}
				}
			}
		}	
	}
}