
CSC = mcs -g

TARGET = Beagle.dll

CSFLAGS = 					\
	-target:library				\
	-define:PREFIX=$(prefix)		\
	-define:PKGLIBDIR=$(pkglibdir)		\
	$(BEAGLE_DEFINES)

CSFILES = 					\
	$(srcdir)/DBusisms.cs			\
	$(srcdir)/FactoryProxy.cs		\
	$(srcdir)/Factory.cs			\
	$(srcdir)/Versioned.cs			\
	$(srcdir)/Hit.cs                	\
	$(srcdir)/Indexable.cs          	\
	$(srcdir)/IIndexableGenerator.cs       	\
	$(srcdir)/Property.cs           	\
	$(srcdir)/QueryProxy.cs         	\
	$(srcdir)/Query.cs              	\
	$(srcdir)/RemoteControlProxy.cs		\
	$(srcdir)/RemoteControl.cs		\
	$(srcdir)/WebHistoryIndexerProxy.cs	\
	$(srcdir)/WebHistoryIndexer.cs



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
	$(TARGET).mdb	\
	$(EXTSTR)
