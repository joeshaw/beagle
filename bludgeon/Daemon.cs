
using System;
using System.Diagnostics;
using System.Threading;

using Beagle.Util;

namespace Bludgeon {

	public class Daemon {

		private Daemon () { }

		// Returns the daemon's version, or null
		static public string PingOnce ()
		{
			Beagle.RequestMessage request;
			request = new Beagle.DaemonInformationRequest ();

			Beagle.ResponseMessage response = null;

			while (response == null) {
				try {
					response = request.Send ();
				} catch {
					return null;
				}
			}

			Beagle.DaemonInformationResponse info;
			info = (Beagle.DaemonInformationResponse) response;

			return info.Version;
		}

		static public string Ping ()
		{
			string version;

			while ((version = PingOnce ()) == null) {
				Log.Spew ("ERROR: Daemon is not responding");
				Thread.Sleep (1000);
			}

			return version;
		}
		
		static public void Start ()
		{
			Log.Spew ("Starting daemon");
			
			string beagled;
			beagled = Environment.GetEnvironmentVariable ("BEAGLED_COMMAND");

			string args;
			args = "--debug --bg --allow-backend files";
			
			Process p;
			p = new Process ();
			p.StartInfo.UseShellExecute = false;
			
			p.StartInfo.FileName = beagled;
			p.StartInfo.Arguments = args;

			p.StartInfo.EnvironmentVariables ["BEAGLE_HOME"] = Beagle.Util.PathFinder.HomeDir;
			p.StartInfo.EnvironmentVariables ["BEAGLE_EXERCISE_THE_DOG"] = "1";
			p.StartInfo.EnvironmentVariables ["BEAGLE_UNDER_BLUDGEON"] = "1";

			Thread.Sleep (2000); // wait 2s to let the daemon get started

			p.Start ();

			string version;
			version = Ping (); // Then try to ping the daemon

			Log.Spew ("Successfully started daemon (version={0})", version);
		}

		static public string GetStatus ()
		{
			Beagle.RequestMessage request;
			request = new Beagle.DaemonInformationRequest ();

			Beagle.ResponseMessage response = null;

			int failure_count = 0;
			while (response == null) {
				try {
					response = request.Send ();
				} catch {
					++failure_count;
					if (failure_count > 10)
						Log.Spew ("Daemon is not responding");
					Thread.Sleep (1000);
				}
			}

			Beagle.DaemonInformationResponse info;
			info = (Beagle.DaemonInformationResponse) response;
			
			return info.HumanReadableStatus;
		}

		static public void WaitUntilIdle ()
		{
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			bool first = true;

			while (true) {
				string status;
				status = GetStatus ();
				if (status.IndexOf ("Waiting on empty queue") != -1) 
					break;

				if (first)
					Log.Spew ("Waiting for daemon to become idle");
				first = false;

				Thread.Sleep (1000);
			}
			sw.Stop ();

			if (! first)
				Log.Spew ("Waited for {0}", sw);
		}

		static public void OptimizeIndexes ()
		{
			Beagle.RequestMessage request;
			request = new Beagle.OptimizeIndexesRequest ();

			Log.Spew ("Optimizing Indexes");

			try {
				request.Send ();
			} catch {
				Log.Failure ("Optimize request failed");
			}
		}

		static public void Shutdown ()
		{
			Beagle.RequestMessage request;
			request = new Beagle.ShutdownRequest ();

			Log.Spew ("Shutting down daemon");

			try {
				request.Send ();
			} catch {
				Log.Failure ("beagled shutdown failed");
			}
		}
	}
}

