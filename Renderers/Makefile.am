
CSC = mcs -g

TARGET = Renderers.dll

CSFLAGS = -target:library

CSFILES = 					\
	$(srcdir)/Favicons.cs			\
	$(srcdir)/DataBarn.cs			\
	$(srcdir)/HitRenderer.cs		\
	$(srcdir)/HitRendererControl.cs		\
	$(srcdir)/HitRendererHtml.cs		\
	$(srcdir)/Tile.cs


#	$(srcdir)/DefaultHitRenderer.cs		\
#	$(srcdir)/ContactHitRenderer.cs		\
#	$(srcdir)/FileHitRenderer.cs		\
#	$(srcdir)/MailMessageHitRenderer.cs	\
#	$(srcdir)/WebLinkHitRenderer.cs

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	-r:../Util/Util.dll		\
	-r:../Filters/Filters.dll	\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../indexer/Indexer.dll

imagedir = $(top_srcdir)/images
templatedir = $(top_srcdir)/templates

RESOURCEFILES =					\
	$(templatedir)/contact.html		\
	$(templatedir)/email.html		\
	$(templatedir)/file-generic.html	\
	$(templatedir)/google.html		\
	$(templatedir)/im-log.html		\
	$(templatedir)/web-history.html		\
	$(templatedir)/weblink.html		\
						\
	$(imagedir)/attachment.png		\
	$(imagedir)/bookmark.png		\
	$(imagedir)/book.png			\
	$(imagedir)/bug.png			\
	$(imagedir)/contact-icon.png		\
	$(imagedir)/contact-phone.png		\
	$(imagedir)/default.png			\
	$(imagedir)/document.png		\
	$(imagedir)/evolution.png		\
	$(imagedir)/flag-for-followup.png	\
	$(imagedir)/foaf.png			\
	$(imagedir)/globe.png			\
	$(imagedir)/gnome-error.png		\
	$(imagedir)/gnome-gaim.png		\
	$(imagedir)/icon-bookmark.png		\
	$(imagedir)/icon-folder.png		\
	$(imagedir)/icon-history.png		\
	$(imagedir)/icon-search.png		\
	$(imagedir)/icon-web-large.png		\
	$(imagedir)/icon-web.png		\
	$(imagedir)/mail-message-icon.png	\
	$(imagedir)/mail-new.png		\
	$(imagedir)/mail.png			\
	$(imagedir)/mail-read.png		\
	$(imagedir)/mail-replied.png		\
	$(imagedir)/music.png			\
	$(imagedir)/person.png			\
	$(imagedir)/rss.png			\
	$(imagedir)/status-away.png		\
	$(imagedir)/status-online.png		\
	$(imagedir)/stock.png			\
	$(imagedir)/white.png

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(RESOURCEFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES) 

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
