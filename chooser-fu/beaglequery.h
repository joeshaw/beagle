/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 4 -*- */


/*
 * beaglequery.h
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

#ifndef __BEAGLEQUERY_H__
#define __BEAGLEQUERY_H__

#include <glib.h>

typedef struct _BeagleHit BeagleHit;
struct _BeagleHit {
    char *uri;
    char *type;
    char *mime_type;
    char *source;
    float score;
};

void beagle_hit_free (BeagleHit *);

#define beagle_hit_get_uri(hit)       ((hit)->uri)
#define beagle_hit_get_type(hit)      ((hit)->type)
#define beagle_hit_get_mime_type(hit) ((hit)->mime_type)
#define beagle_hit_get_source(hit)    ((hit)->source)
#define beagle_hit_get_score(hit)     ((hit)->score)



typedef struct _BeagleQueryResult BeagleQueryResult;
struct _BeagleQueryResult {
    int count;
    GSList *all;
    GHashTable *by_uri;
};

void beagle_query_result_free (BeagleQueryResult *bqr);

#define beagle_query_result_get_count(bqr) ((bqr)->count)
#define beagle_query_result_get_all(bqr)   ((bqr)->all)

BeagleHit *beagle_query_result_get_by_uri (BeagleQueryResult *bqr,
                                           const char *uri);

BeagleQueryResult *beagle_query (const char *query_string);

#endif /* __BEAGLEQUERY_H__ */

