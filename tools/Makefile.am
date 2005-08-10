
CSC = mcs -debug

applicationsdir = $(datadir)/applications
applications_DATA = beagle-settings.desktop

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	-r:Mono.Posix			\
	$(LOCAL_ASSEMBLIES:%=-r:%)

WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@bindir\@|$(bindir)|g"			\
	-e "s|\@sbindir\@|$(sbindir)|g"			\
	-e "s|\@sysconfdir\@|$(sysconfdir)|g"		\
	-e "s|\@localstatedir\@|$(localstatedir)|g"     \
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@bash\@|$(BASH)|"

CRAWL_WRAPPER = beagle-crawl-system
CRAWL_WRAPPER_IN = $(CRAWL_WRAPPER).in

$(CRAWL_WRAPPER) : $(CRAWL_WRAPPER_IN)
	$(WRAPPER_SED) < $(srcdir)/$(CRAWL_WRAPPER_IN) > $@
	chmod +x $(CRAWL_WRAPPER)

CRAWL_RULES = \
	$(srcdir)/crawl-rules/crawl-windows		\
	$(srcdir)/crawl-rules/crawl-applications	\
	$(srcdir)/crawl-rules/crawl-documentation

# Install the system crawler into cron.daily and 
# set up the crawler configuration files
crondir = $(sysconfdir)/cron.daily
cron_SCRIPTS = $(CRAWL_WRAPPER)

# FIXME: Per-distribution specific crawl rules
configdir = $(sysconfdir)/beagle
config_DATA = $(CRAWL_RULES)

INFO_TARGET  = Info.exe
INFO_WRAPPER = beagle-info
INFO_CSFILES = $(srcdir)/Info.cs

$(INFO_TARGET): $(INFO_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(INFO_CSFILES) $(ASSEMBLIES)

$(INFO_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INFO_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(INFO_WRAPPER)

SHUTDOWN_TARGET  = Shutdown.exe
SHUTDOWN_WRAPPER = beagle-shutdown
SHUTDOWN_CSFILES = $(srcdir)/Shutdown.cs

$(SHUTDOWN_TARGET): $(SHUTDOWN_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(SHUTDOWN_CSFILES) $(ASSEMBLIES)

$(SHUTDOWN_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(SHUTDOWN_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(SHUTDOWN_WRAPPER)

QUERY_TARGET  = Query.exe
QUERY_WRAPPER = beagle-query
QUERY_CSFILES = $(srcdir)/Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(QUERY_CSFILES) $(ASSEMBLIES)

$(QUERY_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(QUERY_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(QUERY_WRAPPER)

EXERCISE_FILE_SYSTEM_TARGET = ExerciseFileSystem.exe
EXERCISE_FILE_SYSTEM_WRAPPER = beagle-exercise-file-system
EXERCISE_FILE_SYSTEM_CSFILES = $(srcdir)/ExerciseFileSystem.cs

$(EXERCISE_FILE_SYSTEM_TARGET): $(EXERCISE_FILE_SYSTEM_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(EXERCISE_FILE_SYSTEM_CSFILES) $(ASSEMBLIES)

$(EXERCISE_FILE_SYSTEM_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(EXERCISE_FILE_SYSTEM_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(EXERCISE_FILE_SYSTEM_WRAPPER)

CONFIG_TARGET = Config.exe
CONFIG_WRAPPER = beagle-config
CONFIG_CSFILES = $(srcdir)/Config.cs

$(CONFIG_TARGET): $(CONFIG_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(CONFIG_CSFILES) $(ASSEMBLIES)

$(CONFIG_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(CONFIG_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(CONFIG_WRAPPER)

if ENABLE_WEBSERVICES
SETTINGS_CSFLAGS = -define:ENABLE_WEBSERVICES
endif

SETTINGS_TARGET    = Settings.exe
SETTINGS_WRAPPER   = beagle-settings
SETTINGS_CSFILES   = $(srcdir)/Settings.cs
SETTINGS_RESOURCES = $(srcdir)/settings.glade
SETTINGS_RESOURCES_BUILD = $(foreach res,$(SETTINGS_RESOURCES), $(addprefix -resource:,$(srcdir)/$(res)),$(notdir $(res)))

$(SETTINGS_TARGET): $(SETTINGS_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(SETTINGS_RESOURCES_BUILD) $(CSFLAGS) $(SETTINGS_CSFLAGS) $(SETTINGS_CSFILES) $(ASSEMBLIES)

$(SETTINGS_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(SETTINGS_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(SETTINGS_WRAPPER)

TARGETS = 				\
	$(INFO_TARGET)			\
	$(SHUTDOWN_TARGET)		\
	$(QUERY_TARGET)			\
	$(EXERCISE_FILE_SYSTEM_TARGET)	\
	$(CONFIG_TARGET)		\
	$(SETTINGS_TARGET)

WRAPPERS = 				\
	$(INFO_WRAPPER)			\
	$(SHUTDOWN_WRAPPER)		\
	$(QUERY_WRAPPER)		\
	$(EXERCISE_FILE_SYSTEM_WRAPPER)	\
	$(CONFIG_WRAPPER)		\
	$(SETTINGS_WRAPPER)		

bin_SCRIPTS = 			\
	$(WRAPPERS) 		\
	beagle-index-info	\
	beagle-ping             \
	beagle-status

man_MANS = 			\
	beagle-query.1		\
	beagle-shutdown.1	\
	beagle-status.1		\
	beagle-config.1

all: $(TARGETS) $(WRAPPERS)

install-data-local: $(TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGETS)

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(CRAWL_WRAPPER_IN)		\
	$(INFO_CSFILES)			\
	$(SHUTDOWN_CSFILES)		\
	$(QUERY_CSFILES)		\
	$(EXERCISE_FILE_SYSTEM_CSFILES)	\
	$(CONFIG_CSFILES)		\
	$(SETTINGS_CSFILES)		\
	$(SETTINGS_RESOURCES)		\
	$(man_MANS)			\
	$(CRAWL_CONFIG)			\
	beagle-index-info		\
	beagle-ping			\
	beagle-status			\
	boot.inotify.init		\
	beagle-settings.desktop

CLEANFILES = 				\
	$(TARGETS)			\
	$(TARGETS:%=%.mdb)		\
	$(WRAPPERS)			\
	$(CRAWL_WRAPPER)

