
CSC = $(MCS) -debug

TARGET = Images.dll

CSFLAGS = -target:library

CSFILES =			\
	$(srcdir)/Images.cs

ASSEMBLIES =			\
	-r:../Util/Util.dll     \
	$(BEAGLE_UI_LIBS)

RESOURCEFILES =					\
	$(srcdir)/bug.png			\
	$(srcdir)/contact-icon.png		\
	$(srcdir)/emblem-blog.png		\
	$(srcdir)/emblem-bugzilla.png		\
	$(srcdir)/emblem-calendar.png		\
	$(srcdir)/emblem-contact.png		\
	$(srcdir)/emblem-file.png		\
	$(srcdir)/emblem-folder.png		\
	$(srcdir)/emblem-fspot.png		\
	$(srcdir)/emblem-google.png		\
	$(srcdir)/emblem-im-log.png		\
	$(srcdir)/emblem-mail-message.png	\
	$(srcdir)/emblem-music.png		\
	$(srcdir)/emblem-note.png		\
	$(srcdir)/emblem-picture.png		\
	$(srcdir)/emblem-web-history.png	\
	$(srcdir)/gnome-gaim.png		\
	$(srcdir)/icon-blog.png			\
	$(srcdir)/icon-monodoc.png		\
	$(srcdir)/icon-search.png		\
	$(srcdir)/icon-web.png			\
	$(srcdir)/mail.png			\
	$(srcdir)/music.png			\
	$(srcdir)/no-match.png			\
	$(srcdir)/note.png			\
	$(srcdir)/person.png			\
	$(srcdir)/quick-tips.png		\
	$(srcdir)/status-away.png		\
	$(srcdir)/status-online.png		\
	$(srcdir)/system-search.png		\
	$(srcdir)/tip-arrow.png

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(RESOURCEFILES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES) 

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(TARGET).mdb $(DESTDIR)$(pkglibdir)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET).mdb

otherpixmapsdir = $(datadir)/pixmaps
otherpixmaps_DATA =	\
	system-search.png

EXTRA_DIST = $(CSFILES) $(RESOURCEFILES) $(otherpixmaps_DATA)

CLEANFILES = \
	$(TARGET) 	\
	$(TARGET).mdb
