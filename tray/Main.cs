
using System;

namespace Beagle 
{
	public class TrayMain 
	{
		static BeagleTray instance;

		public static void Main (string [] args) 
		{
			Gnome.Program program = new Gnome.Program ("Beagle", 
								   "0.0", 
								   Gnome.Modules.UI, 
								   args);

			Beagle.Util.GeckoUtils.Init ();
			Beagle.Util.GeckoUtils.SetSystemFonts ();

			/* Restart if we are running when the session ends */
			Gnome.Client client = Gnome.Global.MasterClient ();
			client.RestartStyle = Gnome.RestartStyle.IfRunning;

			instance = new BeagleTray ();

			program.Run ();
		}
	}
}
