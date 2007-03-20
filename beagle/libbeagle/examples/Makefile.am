INCLUDES =				\
	-I$(top_srcdir)/libbeagle	\
	$(LIBBEAGLE_CFLAGS)		\
	$(WARN_CFLAGS)

LDADD = $(top_builddir)/libbeagle/beagle/libbeagle.la

noinst_PROGRAMS =			\
	beagle-search			\
	beagle-shutdown			\
	beagle-info			\
	beagle-external-indexer

beagle_search_SOURCES   	= beagle-search.c
beagle_shutdown_SOURCES 	= beagle-shutdown.c
beagle_info_SOURCES		= beagle-info.c
beagle_external_indexer_SOURCES	= beagle-external-indexer.c

