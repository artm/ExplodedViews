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
using System.Collections;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

[AddComponentMenu("Exploded Views/Net Receive")]
public class NetReceive : MonoBehaviour {

	public int portNo = 6666;
	
	private TcpClient incoming_client;
	private NetworkStream netStream;
	private TcpListener server;
	private bool waiting;

	// Use this for initialization
	void Start () {		
		waiting = false;
		server = new TcpListener(IPAddress.Any, portNo);
		server.Start();
	}

	char[] trimUs = " ;".ToCharArray();

	void Update () {
		string s;

		if (server.Pending()) {
			incoming_client = server.AcceptTcpClient();
			netStream = incoming_client.GetStream();
			waiting = true;
		}
		while (waiting && netStream.DataAvailable) {
			try {
				int numread = 0;
				byte[] tmpbuf = new byte[1024];
				numread = netStream.Read(tmpbuf, 0, tmpbuf.Length);

				s = Encoding.ASCII.GetString(tmpbuf, 0, numread);
				foreach(string line in s.Split('\n')) {
					//Debug.Log("Got: " + line);
					string[] parms = line.TrimEnd(trimUs).Split(' ');
					BroadcastMessage("NetReceive", parms, SendMessageOptions.DontRequireReceiver);
				}
			}
			//Called when netStream fails to read from the stream.
			catch (IOException) {
				waiting = false;
				netStream.Close();
				incoming_client.Close();
			}
			//Called when netStream has been closed already.
			catch (ObjectDisposedException) {
				waiting = false;
				incoming_client.Close();
			}
		}	
	}
	

}
