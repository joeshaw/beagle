INCLUDES = \
	-I$(top_srcdir)		\
	$(LIBBEAGLE_CFLAGS)     \
	$(WARN_CFLAGS)

lib_LTLIBRARIES = libbeagle.la

BUILT_SOURCES =			\
	beagle-marshal.c	\
	beagle-marshal.h

libbeagle_la_SOURCES = 				\
	beagle-client.c				\
	beagle-daemon-information-request.c	\
	beagle-daemon-information-response.c	\
	beagle-empty-response.c			\
	beagle-error-response.c			\
	beagle-finished-response.c		\
	beagle-hit.c				\
	beagle-hits-added-response.c		\
	beagle-hits-subtracted-response.c	\
	beagle-indexable.c			\
	beagle-indexing-service-request.c	\
	beagle-indexing-status-response.c	\
	beagle-informational-messages-request.c	\
	beagle-property.c			\
	beagle-marshal.c			\
	beagle-marshal.h			\
	beagle-parser.c				\
	beagle-parser.h				\
	beagle-private.h			\
	beagle-query.c				\
	beagle-query-part.c			\
	beagle-query-part-date.c		\
	beagle-query-part-human.c		\
	beagle-query-part-or.c			\
	beagle-query-part-property.c		\
	beagle-query-part-text.c		\
	beagle-query-part-wildcard.c		\
	beagle-query-part-uri.c			\
	beagle-queryable-status.c		\
	beagle-scheduler-information.c		\
	beagle-request.c			\
	beagle-response.c			\
	beagle-search-term-response.c		\
	beagle-snippet-request.c		\
	beagle-snippet-response.c		\
	beagle-shutdown-request.c		\
	beagle-timestamp.c			\
	beagle-util.c				

libbeagle_la_LDFLAGS =				\
	-no-undefined				\
	-export-symbols-regex "^[^_].*"		\
	-version-info $(LIBBEAGLE_VERSION_INFO)

libbeagle_la_LIBADD =		\
	$(LIBBEAGLE_LIBS)

libbeagleincludedir = $(includedir)/libbeagle/beagle
libbeagleinclude_HEADERS = 			\
	beagle.h				\
	beagle-client.h				\
	beagle-daemon-information-request.h	\
	beagle-daemon-information-response.h	\
	beagle-empty-response.h			\
	beagle-error-response.h			\
	beagle-finished-response.h		\
	beagle-hit.h				\
	beagle-hits-added-response.h		\
	beagle-hits-subtracted-response.h	\
	beagle-indexable.h			\
	beagle-indexing-service-request.h	\
	beagle-indexing-status-response.h	\
	beagle-informational-messages-request.h	\
	beagle-property.h			\
	beagle-query.h				\
	beagle-query-part.h			\
	beagle-query-part-date.h		\
	beagle-query-part-human.h		\
	beagle-query-part-or.h			\
	beagle-query-part-property.h		\
	beagle-query-part-text.h		\
	beagle-query-part-property.h		\
	beagle-query-part-wildcard.h		\
	beagle-query-part-uri.h			\
	beagle-queryable-status.h		\
	beagle-scheduler-information.h		\
	beagle-request.h			\
	beagle-response.h			\
	beagle-search-term-response.h		\
	beagle-shutdown-request.h		\
	beagle-snippet-request.h		\
	beagle-snippet-response.h		\
	beagle-timestamp.h			\
	beagle-util.h

EXTRA_DIST = beagle-marshal.list

CLEANFILES  = $(BUILT_SOURCES)

beagle-marshal.h: beagle-marshal.list
	cd $(srcdir) \
	&& glib-genmarshal --prefix=beagle_marshal beagle-marshal.list --header >> xgen-gmh \
	&& (cmp -s xgen-gmh beagle-marshal.h || cp xgen-gmh beagle-marshal.h) \
	&& rm -f xgen-gmh xgen-gmh~

beagle-marshal.c: beagle-marshal.list
	cd $(srcdir) \
	&& glib-genmarshal --prefix=beagle_marshal beagle-marshal.list --body >> xgen-gmc \
	&& (cmp -s xgen-gmc beagle-marshal.c || cp xgen-gmc beagle-marshal.c) \
	&& rm -f xgen-gmc xgen-gmc~

noinst_PROGRAMS = test-client

test_client_SOURCES = test-client.c
test_client_LDADD = libbeagle.la


