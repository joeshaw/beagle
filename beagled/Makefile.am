
CSC = mcs -g

TARGET = BeagleDaemon.exe
WRAPPER = beagled

CSFLAGS = 					\
	-target:exe				\
	-define:PREFIX=$(prefix)		\
	-define:PKGLIBDIR=$(pkglibdir)

CSFILES = 				\
	$(srcdir)/BeagleDaemon.cs       \
	$(srcdir)/LiveQuery.cs          \
	$(srcdir)/QueryManager.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../Lucene.Net/Lucene.Net.dll    \
	../BeagleClient/Beagle.dll         \
	../indexer/Indexer.dll

ASSEMBLIES = 				\
	$(BEAGLED_LIBS)                 \
	-r:Mono.Posix			\
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =


$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

TestClient.exe: TestClient.cs 
	$(CSC) -o $@ $(CSFLAGS) TestClient.cs $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(bindir)/$(TARGET)

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" < $^ > $@
	chmod +x $(WRAPPER)

noinst_SCRIPTS=testclient

testclient: $(srcdir)/testclient.in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|TestClient.exe|g" < $^ > $@
	chmod +x testclient


EXTRA_DIST =		\
	$(CSFILES)      \
	$(WRAPPER).in   \
	TestClient.cs

CLEANFILES =		\
	$(TARGET)       \
	$(WRAPPER)      \
	testclient      \
	TestClient.exe
