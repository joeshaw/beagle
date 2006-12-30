/*
 *  Copyright (C) 2003 Marco Pesenti Gritti
 *  Copyright (C) 2003, 2004 Christian Persch
 *  Copyright (C) 2003, 2004 Lee Willis
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 *
 *  $Id$
 */

/*
  This is all copied from Dashboard's Epiphany Extension.
*/

#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include "ephy-beagle-extension.h"

#include <gmodule.h>

G_MODULE_EXPORT GType register_module (GTypeModule *module);

G_MODULE_EXPORT GType
register_module (GTypeModule *module)
{
	return ephy_beagle_extension_register_type (module);
}
