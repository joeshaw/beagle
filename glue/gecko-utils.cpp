
// -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
//
// Copyright (C) 2004 Imendio AB
// Copyright (C) 2004 Marco Pesenti Gritti
//
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

#include <config.h>

#include <gtkmozembed.h>

#include <stdlib.h>

#include <nsEmbedString.h>
#include <nsIPrefService.h>
#include <nsIServiceManager.h>

#if defined (HAVE_CHROME_NSICHROMEREGISTRYSEA_H)
#include <chrome/nsIChromeRegistrySea.h>
#elif defined(MOZ_NSIXULCHROMEREGISTRY_SELECTSKIN)
#include <nsIChromeRegistry.h>
#endif

#ifdef ALLOW_PRIVATE_API
// FIXME: For setting the locale. hopefully gtkmozembed will do itself soon
#include <nsILocaleService.h>
#endif


#include "gecko-utils.h"

static gboolean
blam_util_split_font_string (const gchar *font_name, gchar **name, gint *size)
{
	gchar *tmp_name, *ch;
	
	tmp_name = g_strdup (font_name);

	ch = g_utf8_strrchr (tmp_name, -1, ' ');
	if (!ch || ch == tmp_name) {
		return FALSE;
	}

	*ch = '\0';

	*name = g_strdup (tmp_name);
	*size = strtol (ch + 1, (char **) NULL, 10);

        /* Temporary hack to make the font a bit bigger ;) */
        /* *size = *size + 3; */
	
	return TRUE;
}

static gboolean
gecko_prefs_set_string (const gchar *key, const gchar *value)
{
	nsCOMPtr<nsIPrefService> prefService =
		do_GetService (NS_PREFSERVICE_CONTRACTID);
	nsCOMPtr<nsIPrefBranch> pref;
	prefService->GetBranch ("", getter_AddRefs (pref));

	if (pref) {
		nsresult rv = pref->SetCharPref (key, value);
		return NS_SUCCEEDED (rv) ? TRUE : FALSE;
	}
	
	return FALSE;

}

static gboolean
gecko_prefs_set_int (const gchar *key, gint value)
{
	nsCOMPtr<nsIPrefService> prefService =
		do_GetService (NS_PREFSERVICE_CONTRACTID);
	nsCOMPtr<nsIPrefBranch> pref;
	prefService->GetBranch ("", getter_AddRefs (pref));

	if (pref) {
		nsresult rv = pref->SetIntPref (key, value);
		return NS_SUCCEEDED (rv) ? TRUE : FALSE;
	}
	
	return FALSE;
}

extern "C" void 
blam_gecko_utils_set_font (gint type, const gchar *fontname)
{
	gchar *name;
	gint   size;

	name = NULL;
	if (!blam_util_split_font_string (fontname, &name, &size)) {
		g_free (name);
		return;
	}
	
	switch (type) {
	case BLAM_GECKO_PREF_FONT_VARIABLE:
		gecko_prefs_set_string ("font.name.variable.x-western", 
                                        name);
		gecko_prefs_set_int ("font.size.variable.x-western", 
				     size);
		break;
	case BLAM_GECKO_PREF_FONT_FIXED:
		gecko_prefs_set_string ("font.name.fixed.x-western", 
					name);
		gecko_prefs_set_int ("font.size.fixed.x-western", 
				     size);
		break;
	}

	g_free (name);
}	

static nsresult
getUILang (nsAString& aUILang)
{
	nsresult rv;

	nsCOMPtr<nsILocaleService> localeService = do_GetService (NS_LOCALESERVICE_CONTRACTID);
	if (!localeService)
	{
		g_warning ("Could not get locale service!\n");
		return NS_ERROR_FAILURE;
	}

	rv = localeService->GetLocaleComponentForUserAgent (aUILang);

	if (NS_FAILED (rv))
	{
		g_warning ("Could not determine locale!\n");
		return NS_ERROR_FAILURE;
	}

	return NS_OK;
}

static nsresult 
gecko_utils_init_chrome (void)
{
/* FIXME: can we just omit this on new-toolkit ? */
#if defined(MOZ_NSIXULCHROMEREGISTRY_SELECTSKIN) || defined(HAVE_CHROME_NSICHROMEREGISTRYSEA_H)
        nsresult rv;
        nsEmbedString uiLang;

#ifdef HAVE_CHROME_NSICHROMEREGISTRYSEA_H
        nsCOMPtr<nsIChromeRegistrySea> chromeRegistry = do_GetService (NS_CHROMEREGISTRY_CONTRACTID);
#else
        nsCOMPtr<nsIXULChromeRegistry> chromeRegistry = do_GetService (NS_CHROMEREGISTRY_CONTRACTID);
#endif
        NS_ENSURE_TRUE (chromeRegistry, NS_ERROR_FAILURE);

        // Set skin to 'classic' so we get native scrollbars.
        rv = chromeRegistry->SelectSkin (nsEmbedCString("classic/1.0"), PR_FALSE);
        NS_ENSURE_SUCCESS (rv, NS_ERROR_FAILURE);

        // set locale
        rv = chromeRegistry->SetRuntimeProvider(PR_TRUE);
        NS_ENSURE_SUCCESS (rv, NS_ERROR_FAILURE);

        rv = getUILang(uiLang);
        NS_ENSURE_SUCCESS (rv, NS_ERROR_FAILURE);

        nsEmbedCString cUILang;
        NS_UTF16ToCString (uiLang, NS_CSTRING_ENCODING_UTF8, cUILang);

        return chromeRegistry->SelectLocale (cUILang, PR_FALSE);
#else
        return NS_OK;
#endif
}

extern "C" void
blam_gecko_utils_init_services (void)
{
	gchar *profile_dir;
	
	gtk_moz_embed_set_comp_path (MOZILLA_HOME);
	
	profile_dir = g_build_filename (g_getenv ("HOME"), 
					".gnome2",
					"blam",
					"mozilla", NULL);

	gtk_moz_embed_set_profile_path (profile_dir, "blam");
	g_free (profile_dir);

	gtk_moz_embed_push_startup ();
        
	gecko_prefs_set_string ("font.size.unit", "pt");
	gecko_utils_init_chrome ();
}

extern "C" void
blam_gecko_utils_set_proxy (gboolean use_proxy, gchar *host, gint port)
{
    if (!use_proxy) {
        gecko_prefs_set_int ("network.proxy.type", 0);
    } else {
        gecko_prefs_set_int ("network.proxy.type", 1);
        gecko_prefs_set_string ("network.proxy.http", host);
        gecko_prefs_set_int ("network.proxy.http_port", port);
    }
}

