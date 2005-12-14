
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Beagle.Util;

namespace Bludgeon {

	static public class Daemon {

		public delegate void StartedHandler (string daemon_version);
		public delegate void IdleHandler ();
		public delegate void VerifiedHandler (bool index_is_sane);

		static public bool UseHeapBuddy = false;

		/////////////////////////////////////////////////////////////

		static Beagle.Query index_listener_query = null;

		static private void SetupIndexListener ()
		{
			index_listener_query = new Beagle.Query ();
			index_listener_query.IsIndexListener = true;

			index_listener_query.HitsAddedEvent += OnHitsAdded;
			index_listener_query.HitsSubtractedEvent += OnHitsSubtracted;

			index_listener_query.SendAsync ();
		}

		static private void OnHitsAdded (Beagle.HitsAddedResponse response)
		{
			//Log.Spew ("Added {0} hits!", response.Hits.Count);
			//foreach (Beagle.Hit hit in response.Hits)
			//Log.Spew ("   {0}", hit.Uri);
		}
		
		static private void OnHitsSubtracted (Beagle.HitsSubtractedResponse response)
		{
			//Log.Spew ("Subtracted {0} uris!", response.Uris.Count);
		}

		/////////////////////////////////////////////////////////////

		private class WaitForStartClosure {
			
			Process process;
			StartedHandler started;
			int failure_count = 0;

			public WaitForStartClosure (Process process, StartedHandler started)
			{
				this.process = process;
				this.started = started;
			}

			public void Start ()
			{
				// Wait a little bit before we start pinging the daemon.  This is
				// an attempt to work around a mono crash.
				//Thread.Sleep (200);

				Action.Add (50, new GLib.TimeoutHandler (OurTimeoutHandler));
			}

			private bool OurTimeoutHandler ()
			{
#if false
				// Oops, process.HasExited doesn't work in mono.  Grrr.
				if (process.HasExited) {
					Log.Failure ("Beagle daemon has terminated unexpectedly - exit code {0}",
						     process.ExitCode);
					if (started != null)
						started (null);
					return false;
				}
#endif

				Beagle.RequestMessage request;
				request = new Beagle.DaemonInformationRequest ();

				Beagle.ResponseMessage response = null;
				try {
					//Log.Spew ("Pinging daemon!");
					response = request.Send ();
				} catch { }
				
				if (response == null) {

					++failure_count;
					if (failure_count % 20 == 0)
						Log.Info ("Ping attempt {0} failed", failure_count);
					
					// If we've already tried a bunch of times, just give up.
					if (failure_count >= 100) {
						Log.Failure ("Could not contact daemon after {0} pings", failure_count);
						if (started != null)
							started (null);
						return false;
					}

					return true; // wait a bit, then try again
				}
				
				Beagle.DaemonInformationResponse info;
				info = (Beagle.DaemonInformationResponse) response;

				Log.Spew ("Successfully pinged daemon (version={0})", info.Version);

				SetupIndexListener ();

				if (started != null)
					started (info.Version);
				
				return false; // all done
			}
		}

		static public void Start (StartedHandler started)
		{
			Log.Spew ("Starting daemon");
			
			string beagled;
			beagled = Environment.GetEnvironmentVariable ("BEAGLED_COMMAND");

			string args;
			args = "--debug --bg --allow-backend files";

			if (UseHeapBuddy)
				args += " --heap-buddy";
			
			Process p;
			p = new Process ();
			p.StartInfo.UseShellExecute = false;
			
			p.StartInfo.FileName = beagled;
			p.StartInfo.Arguments = args;

			p.StartInfo.EnvironmentVariables ["BEAGLE_HOME"] = Beagle.Util.PathFinder.HomeDir;
			p.StartInfo.EnvironmentVariables ["BEAGLE_EXERCISE_THE_DOG"] = "1";
			p.StartInfo.EnvironmentVariables ["BEAGLE_UNDER_BLUDGEON"] = "1";
			p.StartInfo.EnvironmentVariables ["BEAGLE_HEAP_BUDDY_DIR"] = Path.Combine (Beagle.Util.PathFinder.HomeDir,
												   ".bludgeon");

			p.Start ();
			
			WaitForStartClosure closure;
			closure = new WaitForStartClosure (p, started);
			closure.Start ();
		}

		/////////////////////////////////////////////////////////////

		private class WaitUntilIdleClosure {
			
			IdleHandler idle;
			int failure_count = 0;
			int busy_count = 0;
			bool optimized = false;
			Stopwatch sw = null;

			public WaitUntilIdleClosure (IdleHandler idle)
			{
				this.idle = idle;
			}

			public void Start ()
			{
				Action.Add (200, new GLib.TimeoutHandler (OurTimeoutHandler));
			}

			private bool OurTimeoutHandler ()
			{
				if (sw == null) {
					sw = new Stopwatch ();
					sw.Start ();
				}

				Beagle.RequestMessage request;
				request = new Beagle.DaemonInformationRequest ();

				Beagle.ResponseMessage response = null;
				try {
					response = request.Send ();
				} catch { }

				if (response == null) {
					++failure_count;
					// FIXME: we should abort if we have too many failures
					if (failure_count > 9)
						Log.Info ("Status request attempt {0} failed", failure_count);
					return true; // wait a bit, then try again
				}

				string status_str;
				status_str = ((Beagle.DaemonInformationResponse) response).HumanReadableStatus;

				if (status_str.IndexOf ("Waiting on empty queue") == -1) {
					if (busy_count == 0)
						Log.Spew ("Waiting for daemon to become idle");
					++busy_count;
					if (busy_count % 10 == 0)
						Log.Spew ("Still waiting for daemon to become idle...");
					return true; // wait a bit, then try again
				}

				if (failure_count > 0 || busy_count > 0)
					Log.Spew ("Daemon is idle after {0}", sw);
				else
					Log.Spew ("Daemon is idle");

				if (! optimized) {
					Log.Spew ("Requesting index optimization");
					optimized = true;
					failure_count = 0;
					busy_count = 0;
					sw.Reset ();

					request = new Beagle.OptimizeIndexesRequest ();
					try {
						request.Send ();
					} catch {
						Log.Failure ("Optimize request failed");
						// FIXME: we should probably terminate here, or something
					}
					
					return true; // wait for optimize to finish
				}

				if (idle != null)
					idle ();

				return false; // all done
			}
		}

		static public void WaitUntilIdle (IdleHandler idle)
		{
			WaitUntilIdleClosure closure;
			closure = new WaitUntilIdleClosure (idle);
			closure.Start ();
		}

		/////////////////////////////////////////////////////////////

		private class WaitUntilVerifiedClosure {

			FileSystemObject root;
			VerifiedHandler verified;

			public WaitUntilVerifiedClosure (FileSystemObject root,
							 VerifiedHandler verified)
			{
				this.root = root;
				this.verified = verified;
			}
			
			public void Start ()
			{
				WaitUntilIdle (OurIdleHandler);
			}

			private void OurIdleHandler ()
			{
				bool is_correct;
				is_correct = SanityCheck.VerifyIndex (root);
				if (verified != null)
					verified (is_correct);
			}
		}

		static public void WaitUntilVerified (FileSystemObject root, VerifiedHandler verified)
		{
			WaitUntilVerifiedClosure closure;
			closure = new WaitUntilVerifiedClosure (root, verified);
			closure.Start ();
		}

		/////////////////////////////////////////////////////////////
		
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

