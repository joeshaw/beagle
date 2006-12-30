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

#include <dlfcn.h>
#include <gtk/gtk.h>
#include "gtkfilesystembeagle.h"

GtkFileSystem *
_gtk_file_system_create (const char *file_system_name)
{
    g_print ("Using Beagle File Chooser Hack\n");
    return gtk_file_system_beagle_new ();
}

GtkWidget *
_gtk_file_chooser_default_new (const char *file_system)
{
    void *chooser_default = NULL;
    GtkWidget *label;
    GtkWidget *chooser;

    if (chooser_default == NULL) {
        void *gtk_handle = dlopen ("/opt/gnome/lib/libgtk-x11-2.0.so", RTLD_LAZY);
        g_assert (gtk_handle != NULL);
        chooser_default = dlsym (gtk_handle, "_gtk_file_chooser_default_new");
        g_assert (chooser_default != NULL);
    }

    chooser = ((GtkWidget *(*)(const char *)) chooser_default) (file_system);

    return chooser;
}

