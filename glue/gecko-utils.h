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

#ifndef __BLAM_GECKO_UTILS_H__
#define __BLAM_GECKO_UTILS_H__

G_BEGIN_DECLS

enum {
	BLAM_GECKO_PREF_FONT_VARIABLE = 1,
	BLAM_GECKO_PREF_FONT_FIXED = 2
};

void            blam_gecko_utils_set_font      (gint          font_type,
                                                const gchar  *fontname);

void            blam_gecko_utils_init_services (void);
void            blam_gecko_utils_set_proxy     (gboolean      use_proxy, 
                                                gchar *       host,
                                                gint          port);

G_END_DECLS

#endif /* __DH_HTML_H__ */

