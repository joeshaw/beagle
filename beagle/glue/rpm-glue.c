/*
 * rpm-glue.c
 *
 * Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
 *
 */

/*
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

#include <stdio.h>
#include <rpmlib.h>
#include <rpmio.h>
#include <rpmdb.h>
#include <popt.h>
#include <stdlib.h>
#include <unistd.h>

/* MISSING declarations in rpm-devel */
int readLead(FD_t fd, struct rpmlead *lead);

/*
 * Does anyone know why rpm API keeps changing ?!
 * And why does it not distribute header files with important functions!
 */

#ifdef HAVE_RPM42
typedef enum sigType_e {
        RPMSIGTYPE_HEADERSIG= 5
} sigType;

int rpmReadSignature(FD_t fd, Header *sighp, sigType sig_type, const char ** msg);
#define _rpmReadSignature(X,Y,Z) rpmReadSignature (X, Y, Z, NULL)
#else
int rpmReadSignature(FD_t fd, Header *header, short sig_type);
#define _rpmReadSignature(X,Y,Z) rpmReadSignature (X, Y, Z)
#endif

/* Changing this will require changing FilterRPM.cs */
enum TagType {
	NAME = 1,
	VERSION,
	SUMMARY,
	DESCRIPTION,
	GROUP,
	LICENSE,
	PACKAGER,
	URL,
	SIZE
};

/* For string properties */
typedef void (* StringCallback) (TagType type, char* value);
/* For integer properties */
typedef void (* IntCallback) (TagType type, int value);
/* For text data */
typedef void (* TextCallback) (char* value);

int rpm_parse (char *file_path, StringCallback string_callback,
	       IntegerCallback int_callback, TextCallback text_callback)
{
	struct rpmlead lead;
	FD_t fd;
	Header header;
	HeaderIterator iter;
	int ret = 0;
	rpmRC rc;

	fd = Fopen (file_path, "r.fdio");
	if (fd == 0)
		return 1; // Cannot open file

    	ret = readLead (fd, &lead);
    	if (ret != 0)
    	    return 2; // Corrupt RPM file

	rc = _rpmReadSignature (fd, NULL, lead.signature_type);
    	if (rc != RPMRC_OK)
    	    return 3; // Bad signature

	header = headerRead (fd, (lead.major >= 3 ? HEADER_MAGIC_YES : HEADER_MAGIC_NO));
    	iter = headerInitIterator (header);

	while (headerNextIterator (iter, &itertag, &type, &p, &count) ) {
		//printf( "itertag=%04d, type=%08lX, p=%08lX, c=%08lX\n",
		//	(int)itertag, (long)type, (long)p, (long)count );
		switch (itertag) {
		case RPMTAG_NAME:
			if (type == RPM_STRING_TYPE)
				string_callback (NAME, p);
			break;

		case RPMTAG_VERSION:
			if (type == RPM_STRING_TYPE)
				string_callback (VERSION, p);
			break;

		case RPMTAG_SUMMARY:
			if (type == RPM_I18NSTRING_TYPE) {
				/* We'll only print out the first string  if there's an array */
				string_callback (SUMMARY, *(char **) p);
			} else if (type == RPM_STRING_TYPE) {
				string_callback (SUMMARY, p);
			}
			break;

		case RPMTAG_DESCRIPTION:
			if (type == RPM_I18NSTRING_TYPE) {
				/* We'll only print out the first string  if there's an array */
				string_callback (DESCRIPTION, *(char **) p);
			} else if (type == RPM_STRING_TYPE) {
				string_callback (DESCRIPTION, p);
			}
			break;

		case RPMTAG_GROUP:
			if (type == RPM_I18NSTRING_TYPE) {
				/* We'll only print out the first string  if there's an array */
				string_callback (GROUP, *(char **) p);
			} else if (type == RPM_STRING_TYPE) {
				string_callback (GROUP, p);
			}
			break;

		case RPMTAG_LICENSE:
			if (type == RPM_STRING_TYPE)
				string_callback (LICENSE, p);
			break;

		case RPMTAG_PACKAGER:
			if (type == RPM_STRING_TYPE)
				string_callback (PACKAGER, p);
			break;

		case RPMTAG_URL:
			if (type == RPM_STRING_TYPE)
				string_callback (URL, p);
			break;

		case RPMTAG_SIZE:
			if (type == RPM_INT32_TYPE)
				int_callback (SIZE, *(int *)p);
			break;

		case RPMTAG_OLDFILENAMES:
		case RPMTAG_BASENAMES:
			if (type == RPM_STRING_ARRAY_TYPE) {
				char **files = p;
				for (i = 0; i < count; ++i)
					text_callback (*(files + i));
			}
			break;

		}

		if (type == RPM_STRING_ARRAY_TYPE || type == RPM_I18NSTRING_TYPE)
		    free (p);
	}
	
	headerFreeIterator (iter);
	header = headerFree (header);
	Fclose(fd);
	
	return 0;
}

# if 0

void print_string (char *name, int_32 type, char *data);

int main()
{
    Header header;
    HeaderIterator iter;
    FD_t fd;
    struct rpmlead lead;
    int ret, i;
    int_32 itertag, type, count;
    rpmRC rc;
    void *p = NULL;
    
    fd = Fopen ("b.rpm", "r.fdio");

    ret = readLead (fd, &lead);
    if (ret != 0) {
	printf ("Error reading lead!\n");
	return 2;
    }

    //printf ("magic = %c%c%c%c\n", lead.magic[0], lead.magic[1], lead.magic[2], lead.magic[3]);
    //printf ("major = %d, minor = %d\n", lead.major, lead.minor);
    //printf ("name = %s\n", lead.name);

    rc = _rpmReadSignature (fd, NULL, lead.signature_type);
    if (rc != RPMRC_OK) {
	printf ("Bad signature\n!");
	return 3;
    }

    header = headerRead (fd, (lead.major >= 3 ? HEADER_MAGIC_YES : HEADER_MAGIC_NO));
    
    iter = headerInitIterator (header);
    while( headerNextIterator( iter, &itertag, &type, &p, &count ) ) {
        //printf( "itertag=%04d, type=%08lX, p=%08lX, c=%08lX\n",
        //	(int)itertag, (long)type, (long)p, (long)count );
	switch (itertag) {
	    case RPMTAG_NAME:
		print_string ("name", RPM_STRING_TYPE, p);
		break;
	    case RPMTAG_VERSION:
		print_string ("version", RPM_STRING_TYPE, p);
		break;
	    case RPMTAG_RELEASE:
		print_string ("release", RPM_STRING_TYPE, p);
		break;

	    case RPMTAG_SUMMARY:
		if( type == RPM_I18NSTRING_TYPE ) {
		    /* We'll only print out the first string  if there's an array */
		    printf( "summary: \"%s\"\n", *(char** )p );
		} else {
		    print_string ("summary", RPM_STRING_TYPE, p);
		}
		break;
		
	    case RPMTAG_DESCRIPTION:
		if( type == RPM_I18NSTRING_TYPE ) {
		    /* We'll only print out the first string  if there's an array */
		    printf( "description: \"%s\"\n", *(char** )p );
		} else {
		    print_string ("description", RPM_STRING_TYPE, p);
		}
		break;

	    case RPMTAG_GROUP:
		if( type == RPM_I18NSTRING_TYPE ) {
		    /* We'll only print out the first string  if there's an array */
		    printf( "category: \"%s\"\n", *(char** )p );
		} else {
		    print_string ("category", RPM_STRING_TYPE, p);
		}
		break;

	    case RPMTAG_LICENSE:
		print_string ("License", RPM_STRING_TYPE, p);
		break;
	    case RPMTAG_PACKAGER:
		print_string ("Packager", RPM_STRING_TYPE, p);
		break;
	    case RPMTAG_URL:
		print_string ("URL", RPM_STRING_TYPE, p);
		break;
	    case RPMTAG_SIZE:
		if (type ==  RPM_INT32_TYPE)
		    printf ("Size: %d bytes\n", *(int *)p);
		break;
	    case RPMTAG_OLDFILENAMES:
	    case RPMTAG_BASENAMES:
			if (type == RPM_STRING_ARRAY_TYPE) {
			    printf( "There are %d files:\t", count);
			    char **files = p;
			    for (i=0; i<count; ++i)
			        printf ("%s\t", *(files+i));
			    printf ("\n");
			    break;
			}
	}
        if( type == RPM_STRING_ARRAY_TYPE || type == RPM_I18NSTRING_TYPE )
            free (p);
    }

    headerFreeIterator (iter);
    header = headerFree (header);
    Fclose(fd);
    
    return 1;
}

void print_string (char *name, int_32 type, char *data)
{
    if (type != RPM_STRING_TYPE)
	return;
    printf ("%s = [%s]\n", name, data);
}

# endif
