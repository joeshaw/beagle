
CSC = mcs -g

TARGET = Best.exe
WRAPPER = best

CSFLAGS = -target:exe

CSFILES =		\
	Vfs.cs		\
	Best.cs		\
	BestWindow.cs	\
	HitContainer.cs	\
	HitView.cs

ASSEMBLIES =			\
	-r:glib-sharp		\
	-r:gtk-sharp		\
	-r:gnome-sharp		\
	-L ../Lucene.Net	\
	-r:Lucene.Net		\
	-L ../Filters		\
	-r:Filters		\
	-L ../indexer		\
	-r:Indexer

$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" < $(srcdir)/$(WRAPPER).in > $(WRAPPER)

EXTRA_DIST =		\
	$(CSFILES)	\
	$(WRAPPER).in

CLEANFILES =		\
	$(TARGET)	\
	$(WRAPPER)
