/// <summary>
/// Uses an iPhone as an orientation tracker. This code relies on the SensorLog app by Bernd Thomas.
/// The SensorLog app should be set up to:
///  1) log data to 'socket'
///  2) output to the IP address and port defined below
///  3) csv separator a comma with no fill
///  4) recording rate at 31 Hz
///  5) record type as DM
/// This code attempts to talk to the SensorLog TCP/IP server to get orientation data.
/// </summary>

using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class iPhoneHandTracker : MonoBehaviour {

	public string iPhoneIPAdress = "192.168.1.2" ;
	public int iPhonePort = 49482;
	
	// These are public just so that I can see them for debugging.
	public bool connectedToServer;
	public string line;
	public int retryCount;
	public int iPhoneFrameCount;
	public int bytesReceived;
	public int fieldsInRecord;
	
	private Socket server;
	

	// Use this for initialization
	void OnEnable () {
	
		// Establish communication with the SensorLog TCP/IP server on the iPhone.
		bytesReceived = 0;
		connectedToServer = false;
		retryCount = 0;
		IPEndPoint ipep = new IPEndPoint( IPAddress.Parse( iPhoneIPAdress ), iPhonePort );
		server = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
		server.Blocking = false;
		try
		{
			server.Connect(ipep);
			// connectedToServer = true;
		} catch (SocketException e)
		{
			Debug.Log("Connect to iPhone at " + iPhoneIPAdress + " on port " + iPhonePort.ToString () + " did not complete. (This is normal.)" + Environment.NewLine + e.ToString() );
			connectedToServer = false;
			return;
		}
	}
	
	// Update is called once per frame
	void Update () {
	
		byte[] data = new byte[1024];
		string[] fields;
		char[] delimiters = { ',' };
			
		if ( !connectedToServer ) {
		// If not already connected, try to connect here.
			try {
			
  				if ( server.Poll ( 200, SelectMode.SelectWrite ) ) {
					connectedToServer = true;
					server.Blocking = true;
					Debug.Log("Connected to iPhone at " + iPhoneIPAdress + " on port " + iPhonePort.ToString () + ".");
					// Read one line and throw it away, in case a header was sent.
					bytesReceived = server.Receive( data );
				}
				else {
					retryCount++;
					if ( (retryCount % 250 ) == 0 ) {
					// Periodically output a debug message to say that we are still trying to connect to the iPhone.
						Debug.Log ( "Connection to iPhone at " + iPhoneIPAdress + " on port " + iPhonePort.ToString () + " did not complete after " + retryCount.ToString () + " tries." );
					}
					// If we have tried too many times, then give up.
					if ( retryCount > 1000 ) this.enabled = false;
				}
			}
			catch ( Exception e ) {
				Debug.LogWarning ( e.ToString () + " exception raised." );
				this.enabled = false;
				return;
			}
			
		}
		else {
			// Get a line with the tracking data from the iPhone.
			try
			{
				bytesReceived = server.Receive( data );
			} catch (SocketException e)
			{
				Debug.Log("Unable to read from iPhone at " + iPhoneIPAdress + " on port " + iPhonePort.ToString () + "." + Environment.NewLine + e.ToString() );
				return;
			}		
			iPhoneFrameCount++;
			line = Encoding.ASCII.GetString( data, 0, bytesReceived );	
			try
			{
				// Parse the line into separate orientation components.
				fields = line.Split ( delimiters );
				fieldsInRecord = fields.Length;
				// Empirically I have observed that whe SensorLog is in DM mode it outputs 24 fields per line
				//  and that the 4th, 5th and 6th fields contain the orientation in radians.
				if ( fieldsInRecord == 24 ) {				
					Vector3 radians = new Vector3 (0.0f, 0.0f, 0.0f );
					// The order that the values are placed into the vector depends on what is the 0 orientation
					//  of the tracker reference frame. Feel free to switch the order and to re-align with
					//  whatever reference orientation that you would like.
					radians.x = float.Parse( fields[3] );
					radians.y = float.Parse( fields[4] );
					radians.z = float.Parse( fields[5] );
					// Convert to degrees.
					this.transform.eulerAngles = 180.0f / Mathf.PI * radians;
				}	
			}
			catch ( Exception e ) {
				Debug.Log("Warning: Unable to parse iPhone tracker input line: " + line + Environment.NewLine + e.ToString() );
				return;
			}		
		}	
	}
	
	void OnDisable () {
		server.Close ();
		Debug.LogWarning ( "Connection to iPhone has been disabled." );
	}
		
}
