
CSC = mcs -g

TARGET = Best.exe
WRAPPER = best

CSFLAGS = -target:exe

CSFILES =				\
	$(srcdir)/Best.cs		\
	$(srcdir)/BestWindow.cs		\
	$(srcdir)/HitContainer.cs	\
	$(srcdir)/HitView.cs

ASSEMBLIES =				\
	-r:glib-sharp.dll		\
	-r:gtk-sharp.dll		\
	-r:gnome-sharp.dll		\
	-r:../Util/Util.dll		\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../Filters/Filters.dll	\
	-r:../indexer/Indexer.dll

$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" < $(srcdir)/$(WRAPPER).in > $(WRAPPER)
	chmod +x $(WRAPPER)

EXTRA_DIST =		\
	$(CSFILES)	\
	$(WRAPPER).in

CLEANFILES =		\
	$(TARGET)	\
	$(WRAPPER)
