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
	public int bufferSize = 1024;
	
	private TcpListener server = null;
	private TcpClient tcpClient = null;
	private NetworkStream netStream = null;

	byte[] tmpbuf;

	// Use this for initialization
	void Start () {
		tmpbuf = new byte[bufferSize];
		server = new TcpListener(IPAddress.Any, portNo);
		server.Start();
	}

	void Update () {
		string s;

		if (!netStream) {
			if (server.Pending()) {
				tcpClient = server.AcceptTcpClient();
				netStream = tcpClient.GetStream();
			}
		}

		while (netStream && netStream.DataAvailable) {
			try {
				int numread = netStream.Read(tmpbuf, 0, tmpbuf.Length);
				s = Encoding.ASCII.GetString(tmpbuf, 0, numread);
				BroadcastMessage("NetReceive", s, SendMessageOptions.DontRequireReceiver);
			}
			//Called when netStream fails to read from the stream.
			catch (IOException) {
				netStream.Close();
				netStream = null;
				tcpClient.Close();
			}
			//Called when netStream has been closed already.
			catch (ObjectDisposedException) {
				netStream = null;
				tcpClient.Close();
			}
		}
	}
}