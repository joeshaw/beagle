/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 4 -*- */


/*
 * searchomatic.c
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

/*
  Federico rules.
*/

#ifdef CONFIG_H
#include <config.h>
#endif
#include <gtk/gtk.h>
#include <string.h>

static void 
text_received_cb (GtkClipboard *clipboard, const char *text, gpointer data)
{
    char *cpy;
    int i;

    if (text != NULL && strlen (text) > 2) {

        cpy = g_strdup (text);

        /* Remove trailing gunk */
        i = strlen (cpy) - 1;
        while (i > 0 && cpy [i] == 10) {
            cpy [i] = '\0';
            --i;
        }

        /* Remove excess whitespace. */
        g_strstrip (cpy);
        
        /* Searchs for best in the PATH. */
        execlp ("best", "best", "--no-tray", cpy, NULL);
    }

    gtk_exit (0);
        
}

static gboolean
request_text (gpointer data)
{
    GtkWidget *my_widget;
    GtkClipboard *clipboard;

    my_widget = GTK_WIDGET (data);
    clipboard = gtk_widget_get_clipboard (my_widget, GDK_SELECTION_PRIMARY);
    gtk_clipboard_request_text (clipboard, text_received_cb, my_widget);

    return FALSE;
}

int
main (int argc, char *argv[])
{
    GtkWidget *window;

    gtk_init (&argc, &argv);

    /* We need a widget, so we create (but don't show) a window. */
    window = gtk_window_new (GTK_WINDOW_TOPLEVEL);

    g_idle_add (request_text, window);
    
    gtk_main ();

    return 0;
}



