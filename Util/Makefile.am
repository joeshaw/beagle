
CSC = mcs -g

TARGET = Util.dll

CSFLAGS = -target:library

GOOGLE_WSDL = $(srcdir)/GoogleSearch.wsdl
GOOGLE_CS   = GoogleSearch.cs

$(GOOGLE_CS): $(GOOGLE_WSDL)
	wsdl -namespace:Beagle.Util -out:$@ $^

CSFILES = 			        	\
	$(srcdir)/camel.cs              	\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/ExifData.cs			\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FlacReader.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GConfThreadHelper.cs          \
	$(srcdir)/GeckoUtils.cs                 \
	$(srcdir)/GtkUtils.cs			\
	$(srcdir)/gnome.cs              	\
	$(srcdir)/Id3.cs                	\
	$(srcdir)/ImLog.cs              	\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/Note.cs               	\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/UriFu.cs			\
	$(srcdir)/Vfs.cs                        \
	$(srcdir)/XKeybinder.cs

ASSEMBLIES =					\
	$(BEAGLE_UI_LIBS)			\
	-r:System.Web.Services			\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix


$(TARGET): $(CSFILES) $(GOOGLE_CS)
	$(CSC) --unsafe -o $@ $(CSFLAGS) $^ $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(GOOGLE_WSDL)	\
	$(CSFILES)

CLEANFILES =		\
	$(GOOGLE_CS)	\
	$(TARGET)	\
	$(TARGET).mdb
