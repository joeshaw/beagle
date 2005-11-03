
# Warnings we don't want to see
# 0067 = event defined but not used
# 0618 = Mono.Posix.Syscall is obsolete

CSC = mcs -debug -nowarn:0067,0618

TARGET = Beagle.dll

CSFLAGS = 					\
	-target:library				\
	-define:PREFIX=$(prefix)		\
	-define:PKGLIBDIR=$(pkglibdir)		\
	$(BEAGLE_DEFINES)

CSFILES = 					\
	$(srcdir)/Client.cs			\
	$(srcdir)/Hit.cs                	\
	$(srcdir)/IIndexableGenerator.cs       	\
	$(srcdir)/Indexable.cs          	\
	$(srcdir)/IndexingService.cs		\
	$(srcdir)/Message.cs			\
	$(srcdir)/Property.cs           	\
	$(srcdir)/Query.cs              	\
	$(srcdir)/QueryPart.cs			\
	$(srcdir)/QueryResponses.cs            	\
	$(srcdir)/QueryableStatus.cs		\
	$(srcdir)/RemoteControl.cs		\
	$(srcdir)/Snippet.cs			\
	$(srcdir)/Versioned.cs

if ENABLE_WEBSERVICES
CSFILES +=	$(srcdir)/NetworkHit.cs
endif

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll


ASSEMBLIES = 				\
	$(BEAGLED_LIBS)                 \
	-r:Mono.Posix			\
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =			\
	$(CSFILES)		\
	$(srcdir)/NetworkHit.cs

CLEANFILES =		\
	$(TARGET)	\
	$(TARGET).mdb
