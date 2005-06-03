
CSC = mcs -debug

TARGET = Images.dll

CSFLAGS = -target:library

CSFILES =			\
	$(srcdir)/Images.cs

ASSEMBLIES =			\
	-r:../Util/Util.dll     \
	$(BEAGLE_UI_LIBS)

RESOURCEFILES =					\
	$(srcdir)/attachment.png		\
	$(srcdir)/best.png			\
	$(srcdir)/bookmark.png			\
	$(srcdir)/book.png			\
	$(srcdir)/bug.png			\
	$(srcdir)/contact-icon.png		\
	$(srcdir)/contact-phone.png		\
	$(srcdir)/default.png			\
	$(srcdir)/document.png			\
	$(srcdir)/emblem-bugzilla.png		\
	$(srcdir)/emblem-blog.png		\
	$(srcdir)/emblem-contact.png		\
	$(srcdir)/emblem-file.png		\
	$(srcdir)/emblem-folder.png		\
	$(srcdir)/emblem-google.png		\
	$(srcdir)/emblem-im-log.png		\
	$(srcdir)/emblem-mail-message.png	\
	$(srcdir)/emblem-music.png		\
	$(srcdir)/emblem-note.png		\
	$(srcdir)/emblem-picture.png		\
	$(srcdir)/emblem-web-history.png	\
	$(srcdir)/evolution.png			\
	$(srcdir)/flag-for-followup.png		\
	$(srcdir)/foaf.png			\
	$(srcdir)/globe.png			\
	$(srcdir)/gnome-error.png		\
	$(srcdir)/gnome-gaim.png		\
	$(srcdir)/icon-blog.png			\
	$(srcdir)/icon-monodoc.png		\
	$(srcdir)/icon-bookmark.png		\
	$(srcdir)/icon-folder.png		\
	$(srcdir)/icon-history.png		\
	$(srcdir)/icon-search.png		\
	$(srcdir)/icon-web-large.png		\
	$(srcdir)/icon-web.png			\
	$(srcdir)/im-aim.png			\
	$(srcdir)/mail-large-new.png		\
	$(srcdir)/mail-large-read.png		\
	$(srcdir)/mail-large-read-replied.png	\
	$(srcdir)/mail-message-icon.png		\
	$(srcdir)/mail-new.png			\
	$(srcdir)/mail.png			\
	$(srcdir)/mail-read.png			\
	$(srcdir)/mail-replied.png		\
	$(srcdir)/music.png			\
	$(srcdir)/note.png			\
	$(srcdir)/person.png			\
	$(srcdir)/rss.png			\
	$(srcdir)/smalldog.png			\
	$(srcdir)/smalldogalert.png		\
	$(srcdir)/status-away.png		\
	$(srcdir)/status-online.png		\
	$(srcdir)/stock.png			\
	$(srcdir)/trayicon.png			\
	$(srcdir)/white.png			\
	$(srcdir)/emblem-calendar.png

if ENABLE_WEBSERVICES
RESOURCEFILES +=	$(srcdir)/netbeagle.png	
endif

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(RESOURCEFILES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES) 

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

otherpixmapsdir = $(datadir)/pixmaps
otherpixmaps_DATA = 	\
	best.png	\
	dog.png

EXTRA_DIST = $(CSFILES) $(RESOURCEFILES) $(otherpixmaps_DATA)

CLEANFILES = \
	$(TARGET) 	\
	$(TARGET).mdb
