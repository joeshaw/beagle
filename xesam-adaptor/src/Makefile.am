CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

TARGET = xesam-adaptor.exe
WRAPPER = xesam-adaptor

DIR_DBUS =  $(top_builddir)/dbus-sharp

XESAMADAPTOR_DIST_CSFILES =			\
	$(srcdir)/Session.cs			\
	$(srcdir)/Search.cs			\
	$(srcdir)/Searcher.cs			\
	$(srcdir)/Parser.cs			\
	$(srcdir)/Ontologies.cs			\
	$(srcdir)/XesamAdaptor.cs		

XESAMADAPTOR_CSFILES = $(XESAMADAPTOR_DIST_CSFILES)

XESAMADAPTOR_ASSEMBLIES =			

DBUS_ASSEMBLIES = 				\
	-r:$(DIR_DBUS)/src/NDesk.DBus.dll

GLIBSHARP_ASSEMBLIES = ${GLIBSHARP_LIBS}
BEAGLE_ASSEMBLIES = ${BEAGLE_LIBS}

XESAMADAPTOR_RESOURCES =				

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e"s|\@prefix\@|$(prefix)|g"		\
	    -e"s|\@pkglibdir\@|$(pkglibdir)|g"		\
	    -e"s|\@target\@|$(TARGET)|g"		\
	    -e"s|\@mono\@|$(MONO)|g"			\
	    -e"s|\@mono_flags\@|$(MONO_FLAGS)|g"	\
	    < $^ > $@

$(TARGET).config: $(srcdir)/$(TARGET).config.in
	sed -e "s|\@pkglibdir\@|$(pkglibdir)|" 		\
	    < $^ > $@

$(TARGET): $(XESAMADAPTOR_CSFILES)
	$(CSC) -out:$@ $(XESAMADAPTOR_CSFILES) $(XESAMADAPTOR_ASSEMBLIES) $(DBUS_ASSEMBLIES) $(XESAMADAPTOR_RESOURCES) ${GLIBSHARP_ASSEMBLIES} ${BEAGLE_ASSEMBLIES}

all: $(TARGET)

xesamadaptorlibdir = $(pkglibdir)
xesamadaptorlib_DATA = $(TARGET) $(TARGET).config

bin_SCRIPTS = $(WRAPPER)

EXTRA_DIST =					\
	$(XESAMADAPTOR_DIST_CSFILES)		\
	$(WRAPPER).in				\
	$(TARGET).config.in			

CLEANFILES = 					\
	$(TARGET)				\
	$(TARGET).config			\
	$(TARGET).mdb				\
	$(WRAPPER)				
