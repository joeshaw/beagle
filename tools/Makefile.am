
CSC = mcs -g

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
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"


QUERY_TARGET  = Query.exe
QUERY_WRAPPER = beagle-query
QUERY_CSFILES = $(srcdir)/Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(CSFLAGS) $(QUERY_CSFILES) $(ASSEMBLIES)

$(QUERY_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(QUERY_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(QUERY_WRAPPER)

EXERCISE_FILE_SYSTEM_TARGET = ExerciseFileSystem.exe
EXERCISE_FILE_SYSTEM_WRAPPER = beagle-exercise-file-system
EXERCISE_FILE_SYSTEM_CSFILES = $(srcdir)/ExerciseFileSystem.cs

$(EXERCISE_FILE_SYSTEM_TARGET): $(EXERCISE_FILE_SYSTEM_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(CSFLAGS) $(EXERCISE_FILE_SYSTEM_CSFILES) $(ASSEMBLIES)

$(EXERCISE_FILE_SYSTEM_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(EXERCISE_FILE_SYSTEM_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(EXERCISE_FILE_SYSTEM_WRAPPER)

TARGETS = 				\
	$(QUERY_TARGET)                 \
	$(EXERCISE_FILE_SYSTEM_TARGET)

WRAPPERS = 				\
	$(QUERY_WRAPPER)		\
	$(EXERCISE_FILE_SYSTEM_WRAPPER)

bin_SCRIPTS = 			\
	$(WRAPPERS) 		\
	beagle-index-info	\
	beagle-ping             \
	beagle-shutdown		\
	beagle-status

man_MANS = 			\
	beagle-query.1		\
	beagle-shutdown.1	\
	beagle-status.1

all: $(TARGETS) $(WRAPPERS)

install-data-local: $(TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGETS)

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(QUERY_CSFILES)		\
	$(EXERCISE_FILE_SYSTEM_CSFILES)	\
	$(man_MANS)			\
	beagle-index-info		\
	beagle-ping			\
	beagle-shutdown			\
	beagle-status			\
	boot.inotify.init

CLEANFILES = 				\
	$(TARGETS)			\
	$(TARGETS:%=%.mdb)		\
	$(WRAPPERS)
