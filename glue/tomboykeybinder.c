

#include <gdk/gdk.h>
#include <gdk/gdkkeysyms.h>
#include <gdk/gdkwindow.h>
#include <gdk/gdkx.h>
#include <X11/Xlib.h>

#include "eggaccelerators.h"
#include "tomboykeybinder.h"

static GSList *bindings = NULL;
static guint ignored_modifiers = 0;


/* Uncomment the next line to print a debug trace. */
/* #define DEBUG */

#ifdef DEBUG
#  define TRACE(x) x
#else
#  define TRACE(x) do {} while (FALSE);
#endif

typedef struct _Binding {
	TomboyBindkeyHandler  handler;
	gpointer              user_data;
	char                 *keystring;
	uint                  keycode;
	uint                  modifiers;
} Binding;

/*
 * Adapted from eggcellrenderkeys.c.
 */
static guint
get_modifier_mask_for_keycode (guint keycode)
{
	gint i;
	gint map_size;
	XModifierKeymap *mod_keymap;
	guint retval = 0;

	mod_keymap = XGetModifierMapping (gdk_display);
	
	map_size = 8 * mod_keymap->max_keypermod;

	for (i = 0; i < map_size; i++) {
		if (keycode == mod_keymap->modifiermap[i]) {
			retval = i;
			break;
		}
	}

	XFreeModifiermap (mod_keymap);

	return retval;
}

gboolean
tomboy_keybinder_is_modifier (guint keycode)
{
	return (get_modifier_mask_for_keycode (keycode) != 0);
}

static guint
get_ignored_modifiers (void)
{
	guint keyvals[] = { GDK_Num_Lock, GDK_Scroll_Lock, 0 };
	int i;
	GdkKeymapKey *keys;
	int num_keys;

	if (ignored_modifiers != 0)
		return ignored_modifiers;

	ignored_modifiers = GDK_LOCK_MASK | 0x2000; /* Xkb modifier */

	for (i = 0; keyvals[i] != 0; i++) {
		if (gdk_keymap_get_entries_for_keyval (NULL, keyvals[i], &keys, &num_keys)) {
			int j;

			for (j = 0; j < num_keys; j++)
				ignored_modifiers |= get_modifier_mask_for_keycode (keys[j].keycode);

			g_free (keys);
		}
	}

	return ignored_modifiers;
}

static gboolean 
do_grab_key (Binding *binding)
{
	GdkKeymap *keymap = gdk_keymap_get_default ();
	GdkWindow *rootwin = gdk_get_default_root_window ();

	EggVirtualModifierType virtual_mods = 0;
	guint keysym = 0;
	int i;

	if (keymap == NULL || rootwin == NULL)
		return FALSE;

	if (!egg_accelerator_parse_virtual (binding->keystring, 
					    &keysym, 
					    &virtual_mods))
		return FALSE;

	TRACE (g_print ("Got accel %d, %d\n", keysym, virtual_mods));

	binding->keycode = XKeysymToKeycode (GDK_WINDOW_XDISPLAY (rootwin), 
					     keysym);
	if (binding->keycode == 0)
		return FALSE;

	TRACE (g_print ("Got keycode %d\n", binding->keycode));

	egg_keymap_resolve_virtual_modifiers (keymap,
					      virtual_mods,
					      &binding->modifiers);

	TRACE (g_print ("Got modmask %d\n", binding->modifiers));

	for (i = 0; i <= get_ignored_modifiers (); i++) {
		/* Check to ensure we're not including non-ignored modifiers */
		if (i & ~(get_ignored_modifiers ()))
			continue;

		XGrabKey (GDK_WINDOW_XDISPLAY (rootwin),
			  binding->keycode,
			  binding->modifiers | i,
			  GDK_WINDOW_XWINDOW (rootwin),
			  True,
			  GrabModeAsync, 
			  GrabModeAsync);
	}

	return TRUE;
}

static gboolean 
do_ungrab_key (Binding *binding)
{
	GdkWindow *rootwin = gdk_get_default_root_window ();
	int i;

	TRACE (g_print ("Removing grab for '%s'\n", binding->keystring));

	for (i = 0; i <= get_ignored_modifiers (); i++) {
		/* Check to ensure we're not including non-ignored modifiers */
		if (i & ~(get_ignored_modifiers ()))
			continue;

		XUngrabKey (GDK_WINDOW_XDISPLAY (rootwin),
			    binding->keycode,
			    binding->modifiers | i,
			    GDK_WINDOW_XWINDOW (rootwin));
	}

	return TRUE;
}

static GdkFilterReturn
filter_func (GdkXEvent *gdk_xevent, GdkEvent *event, gpointer data)
{
	GdkFilterReturn return_val = GDK_FILTER_CONTINUE;
	XEvent *xevent = (XEvent *) gdk_xevent;
	GSList *iter;

	TRACE (g_print ("Got Event! %d, %d\n", xevent->type, event->type));

	switch (xevent->type) {
	case KeyPress:
		TRACE (g_print ("Got KeyPress! keycode: %d, modifiers: %d\n", 
				xevent->xkey.keycode, 
				xevent->xkey.state));

		for (iter = bindings; iter != NULL; iter = iter->next) {
			Binding *binding = (Binding *) iter->data;

			if (binding->keycode == xevent->xkey.keycode &&
			    binding->modifiers == (xevent->xkey.state & ~get_ignored_modifiers ())) {

				TRACE (g_print ("Calling handler for '%s'...\n", 
						binding->keystring));

				(binding->handler) (binding->keystring, 
						    binding->user_data);
			}
		}
		break;
	case KeyRelease:
		TRACE (g_print ("Got KeyRelease! \n"));
		break;
	}

	return return_val;
}

void 
keymap_changed (GdkKeymap *map)
{
	GSList *iter;

	TRACE (g_print ("Keymap changed! Regrabbing keys..."));

	ignored_modifiers = 0;

	for (iter = bindings; iter != NULL; iter = iter->next) {
		Binding *binding = (Binding *) iter->data;

		do_ungrab_key (binding);
		do_grab_key (binding);
	}
}

void 
tomboy_keybinder_init (void)
{
	GdkKeymap *keymap = gdk_keymap_get_default ();
	GdkWindow *rootwin = gdk_get_default_root_window ();

	gdk_window_add_filter (rootwin, 
			       filter_func, 
			       NULL);

	g_signal_connect (keymap, 
			  "keys_changed",
			  G_CALLBACK (keymap_changed),
			  NULL);
}

void 
tomboy_keybinder_bind (const char           *keystring,
		       TomboyBindkeyHandler  handler,
		       gpointer              user_data)
{
	Binding *binding;
	gboolean success;

	binding = g_new0 (Binding, 1);
	binding->keystring = g_strdup (keystring);
	binding->handler = handler;
	binding->user_data = user_data;

	/* Sets the binding's keycode and modifiers */
	success = do_grab_key (binding);

	if (success) {
		bindings = g_slist_prepend (bindings, binding);
	} else {
		g_free (binding->keystring);
		g_free (binding);
	}
}

void
tomboy_keybinder_unbind (const char           *keystring, 
			 TomboyBindkeyHandler  handler)
{
	GSList *iter;

	for (iter = bindings; iter != NULL; iter = iter->next) {
		Binding *binding = (Binding *) iter->data;

		if (strcmp (keystring, binding->keystring) != 0 ||
		    handler != binding->handler) 
			continue;

		do_ungrab_key (binding);

		bindings = g_slist_remove (bindings, binding);

		g_free (binding->keystring);
		g_free (binding);
		break;
	}
}
