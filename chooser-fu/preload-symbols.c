/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 4 -*- */


/*
 * preload_symbols.c
 *
 * Copyright (C) 2004 Novell, Inc.
 *
 */

/*
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
 * USA.
 */

#ifdef CONFIG_H
#include <config.h>
#endif

#include "gtkfilesystembeagle.h"

GtkFileSystem *
_gtk_file_system_create (const char *file_system_name)
{
    g_print ("Using Beagle File Chooser Hack\n");
    return gtk_file_system_beagle_new ();
}

