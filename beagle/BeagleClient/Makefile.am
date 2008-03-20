CSC = $(MCS) -debug

TARGET = Beagle.dll

CSFLAGS = 						\
	-target:library					\
	$(BEAGLE_DEFINES)

if ENABLE_AVAHI
CSFLAGS += -define:ENABLE_AVAHI
endif

SERIALIZER_CSFILES = 					\
	$(srcdir)/Hit.cs                		\
	$(srcdir)/Indexable.cs          		\
	$(srcdir)/IndexingService.cs			\
	$(srcdir)/IndexingStatusResponse.cs		\
	$(srcdir)/InformationalMessagesRequest.cs	\
	$(srcdir)/Message.cs				\
	$(srcdir)/Property.cs           		\
	$(srcdir)/Query.cs              		\
	$(srcdir)/QueryPart.cs				\
	$(srcdir)/QueryResponses.cs            		\
	$(srcdir)/QueryableStatus.cs			\
	$(srcdir)/RemoteControl.cs			\
	$(srcdir)/Snippet.cs				\
	$(srcdir)/Versioned.cs

OTHER_CSFILES =	\
	$(srcdir)/AssemblyInfo.cs			\
	$(srcdir)/IIndexableGenerator.cs       		\
	$(srcdir)/Transport.cs				\
	$(srcdir)/UnixTransport.cs

CSFILES = $(SERIALIZER_CSFILES)	\
	  $(OTHER_CSFILES)

LOCAL_ASSEMBLIES =					\
	../Util/Util.dll

ASSEMBLIES = 						\
	$(BEAGLED_LIBS)                 		\
	$(SHARPZIPLIBS_LIBS)				\
	-r:Mono.Posix					\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =

DUMMY_TARGET = Beagle.Dummy.dll
GENERATED_DIR = generated
GENERATED_TARGET = $(GENERATED_DIR)/$(DUMMY_TARGET:%.dll=%.XmlSerializers.dll)

$(DUMMY_TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -out:$@ $(CSFLAGS) -define:GENERATE_SERIALIZE $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

$(GENERATED_TARGET): $(SERIALIZER_CSFILES) $(DUMMY_TARGET)
	DUMMY_TARGET="$(DUMMY_TARGET)" $(MAKE) -e -C $(GENERATED_DIR)

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES) $(GENERATED_TARGET)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES) $(GENERATED_DIR)/*.cs

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(TARGET).mdb $(DESTDIR)$(pkglibdir)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET).mdb

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =			\
	$(DUMMY_TARGET)		\
	$(DUMMY_TARGET).mdb 	\
	$(GENERATED_TARGET)	\
	$(GENERATED_TARGET).mdb	\
	$(TARGET)		\
	$(TARGET).mdb
