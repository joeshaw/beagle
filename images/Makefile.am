
CSC = mcs -g

TARGET = Images.dll

CSFLAGS = -target:library

CSFILES =			\
	$(srcdir)/Images.cs

ASSEMBLIES =			\
	$(BEAGLE_UI_LIBS)

RESOURCEFILES =				\
	$(srcdir)/attachment.png	\
	$(srcdir)/bookmark.png		\
	$(srcdir)/book.png		\
	$(srcdir)/bug.png		\
	$(srcdir)/contact-icon.png	\
	$(srcdir)/contact-phone.png	\
	$(srcdir)/default.png		\
	$(srcdir)/document.png		\
	$(srcdir)/evolution.png		\
	$(srcdir)/flag-for-followup.png	\
	$(srcdir)/foaf.png		\
	$(srcdir)/globe.png		\
	$(srcdir)/gnome-error.png	\
	$(srcdir)/gnome-gaim.png	\
	$(srcdir)/icon-bookmark.png	\
	$(srcdir)/icon-folder.png	\
	$(srcdir)/icon-history.png	\
	$(srcdir)/icon-search.png	\
	$(srcdir)/icon-web-large.png	\
	$(srcdir)/icon-web.png		\
	$(srcdir)/mail-message-icon.png	\
	$(srcdir)/mail-new.png		\
	$(srcdir)/mail.png		\
	$(srcdir)/mail-read.png		\
	$(srcdir)/mail-replied.png	\
	$(srcdir)/music.png		\
	$(srcdir)/person.png		\
	$(srcdir)/rss.png		\
	$(srcdir)/status-away.png	\
	$(srcdir)/status-online.png	\
	$(srcdir)/stock.png		\
	$(srcdir)/white.png

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

EXTRA_DIST = $(CSFILES) $(RESOURCEFILES)

CLEANFILES = $(TARGET)