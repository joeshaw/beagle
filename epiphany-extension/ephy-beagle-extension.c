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

#include <epiphany/ephy-extension.h>
#include <epiphany/ephy-embed-persist.h>
#include <epiphany/ephy-shell.h>
#include <epiphany/ephy-node.h>
#include <epiphany/ephy-bookmarks.h>

#include <gmodule.h>

#define EPHY_BEAGLE_EXTENSION_GET_PRIVATE(object) (G_TYPE_INSTANCE_GET_PRIVATE ((object), EPHY_TYPE_BEAGLE_EXTENSION, EphyBeagleExtensionPrivate))

struct EphyBeagleExtensionPrivate
{
};

#define EPIPHANY_FRONTEND_IDENTIFIER	"Web Browser"

static void ephy_beagle_extension_class_init	(EphyBeagleExtensionClass *klass);
static void ephy_beagle_extension_iface_init	(EphyExtensionIface *iface);
static void ephy_beagle_extension_init	(EphyBeagleExtension *extension);

static GObjectClass *parent_class = NULL;

static GType type = 0;

GType
ephy_beagle_extension_get_type (void)
{
	return type;
}

GType
ephy_beagle_extension_register_type (GTypeModule *module)
{
	static const GTypeInfo our_info =
	{
		sizeof (EphyBeagleExtensionClass),
		NULL, /* base_init */
		NULL, /* base_finalize */
		(GClassInitFunc) ephy_beagle_extension_class_init,
		NULL,
		NULL, /* class_data */
		sizeof (EphyBeagleExtension),
		0, /* n_preallocs */
		(GInstanceInitFunc) ephy_beagle_extension_init
	};

	static const GInterfaceInfo extension_info =
	{
		(GInterfaceInitFunc) ephy_beagle_extension_iface_init,
		NULL,
		NULL
	};

	type = g_type_module_register_type (module,
					    G_TYPE_OBJECT,
					    "EphyBeagleExtension",
					    &our_info, 0);

	g_type_module_add_interface (module,
				     type,
				     EPHY_TYPE_EXTENSION,
				     &extension_info);

	return type;
}

static void
load_status_cb (EphyTab *tab,
		GParamSpec *pspec,
		EphyBeagleExtension *extension)
{
	gboolean load_status;

	/* Don't index web pages if this environment variable is set. */
	if (getenv ("BEAGLE_NO_WEB_INDEXING") != NULL)
	    return;
	
	load_status = ephy_tab_get_load_status(tab);

	/* FALSE means load is finished */
	if (load_status == FALSE)
	{
		EphyEmbed *embed;
		EphyEmbedPersist *persist;
		char *location;
		const char *page_title;
		char *content;
		int child_stdin;
		char *argv[6];

		embed = ephy_tab_get_embed (tab);
		g_return_if_fail (EPHY_IS_EMBED (embed));

		/* Get the URL from the embed, since tab may contain modified url */
		location = ephy_embed_get_location (embed, TRUE);

		/* Get page title */
		page_title = ephy_tab_get_title(tab);

		/* Get the page content. */
		persist = EPHY_EMBED_PERSIST (ephy_embed_factory_new_object ("EphyEmbedPersist"));
		ephy_embed_persist_set_embed (persist, embed);
		ephy_embed_persist_set_flags (persist, EMBED_PERSIST_NO_VIEW);
		content = ephy_embed_persist_to_string (persist);
		g_object_unref (persist);

		argv[0] = "beagle-index-url";
		argv[1] = "--url";
		argv[2] = location;
		argv[3] = "--title";
		argv[4] = (char *) page_title;
		argv[5] = NULL;

		if (g_spawn_async_with_pipes (NULL, /* inherit parent's working directory */
					      argv,
					      NULL, /* inherit parent's environment */
					      G_SPAWN_SEARCH_PATH,
					      NULL, NULL, /* no special child setup needed */
					      NULL, /* don't need the child pid */
					      &child_stdin,
					      NULL, NULL, /* don't need access to child stdout/stderr */
					      NULL))
		{
			FILE *to_child = fdopen (child_stdin, "w");
			if (to_child != NULL)
			{
				fprintf (to_child, "%s\n", content);
				fclose (to_child);
			}
		}

		g_free (location);
	} 
}

static void
tab_added_cb (GtkWidget *notebook,
	      EphyTab *tab,
	      EphyBeagleExtension *extension)
{
	g_return_if_fail (EPHY_IS_TAB (tab));

	g_signal_connect_after (tab, "notify::load-status",
				G_CALLBACK (load_status_cb), extension);
}

static void
tab_removed_cb (GtkWidget *notebook,
		EphyTab *tab,
		EphyBeagleExtension *extension)
{
	g_return_if_fail (EPHY_IS_TAB (tab));

	g_signal_handlers_disconnect_by_func
		(tab, G_CALLBACK (load_status_cb), extension);
}

static void
impl_attach_window (EphyExtension *ext,
		    EphyWindow *window)
{
	GtkWidget *notebook;

	notebook = ephy_window_get_notebook (window);

	g_signal_connect_after (notebook, "tab_added",
				G_CALLBACK (tab_added_cb), ext);
	g_signal_connect_after (notebook, "tab_removed",
				G_CALLBACK (tab_removed_cb), ext);
}

static void
impl_detach_window (EphyExtension *ext,
		    EphyWindow *window)
{
	GtkWidget *notebook;

	notebook = ephy_window_get_notebook (window);

	g_signal_handlers_disconnect_by_func
		(notebook, G_CALLBACK (tab_added_cb), ext);
	g_signal_handlers_disconnect_by_func
		(notebook, G_CALLBACK (tab_removed_cb), ext);
}

static void
ephy_beagle_extension_init (EphyBeagleExtension *extension)
{
	extension->priv = EPHY_BEAGLE_EXTENSION_GET_PRIVATE (extension);
}

static void
ephy_beagle_extension_iface_init (EphyExtensionIface *iface)
{
	iface->attach_window = impl_attach_window;
	iface->detach_window = impl_detach_window;
}

static void
ephy_beagle_extension_class_init (EphyBeagleExtensionClass *klass)
{
	GObjectClass *object_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	g_type_class_add_private (object_class, sizeof (EphyBeagleExtensionPrivate));
}
