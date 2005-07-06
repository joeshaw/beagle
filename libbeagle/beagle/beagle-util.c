#include "beagle-util.h"

GQuark
beagle_error_quark (void)
{
	static GQuark quark;

	if (!quark)
		quark = g_quark_from_static_string ("BEAGLE_ERROR");

	return quark;
}
