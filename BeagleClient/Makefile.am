
CSC = mcs -g

TARGET = Beagle.dll

CSFLAGS = 					\
	-target:library				\
	-define:PREFIX=$(prefix)		\
	-define:PKGLIBDIR=$(pkglibdir)

CSFILES = 				\
	$(srcdir)/DBusisms.cs		\
	$(srcdir)/FactoryProxy.cs	\
	$(srcdir)/Factory.cs		\
	$(srcdir)/Hit.cs                \
	$(srcdir)/Indexable.cs          \
	$(srcdir)/IndexerProxy.cs	\
	$(srcdir)/Indexer.cs            \
	$(srcdir)/Property.cs           \
	$(srcdir)/Query.cs              \
	$(srcdir)/QueryProxy.cs         \
	$(srcdir)/Versioned.cs 



LOCAL_ASSEMBLIES =			\
	../Util/Util.dll


ASSEMBLIES = 				\
	$(BEAGLED_LIBS)                 \
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =

$(TARGET): $(CSFILES) $(EXTSTR) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(EXTSTR) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)	\
	$(EXTSTR_IN)

CLEANFILES =		\
	$(TARGET)	\
	$(EXTSTR)
