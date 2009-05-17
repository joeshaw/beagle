INSTALL_DIR = $(pkglibdir)

UNIQUE_FILTER_PIPE = tr [:space:] \\n | sort | uniq
BUILD_DATA_DIR = $(top_builddir)/bin/share/$(PACKAGE)

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(top_srcdir)/src/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

if ENABLE_TESTS
    LINK += " $(NUNIT_LIBS)"
    ENABLE_TESTS_FLAG = "-define:ENABLE_TESTS"
endif

FILTERED_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE)

run: 
	@pushd $(top_builddir); \
	make run \
	popd;

build-debug:
	@echo $(DEP_LINK)

$(ASSEMBLY_FILE).mdb: $(ASSEMBLY_FILE)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	@colors=no; \
	case $$TERM in \
		"xterm" | "rxvt" | "rxvt-unicode") \
			test "x$$COLORTERM" != "x" && colors=yes ;; \
		"xterm-color") colors=yes ;; \
	esac; \
	if [ "x$$colors" = "xyes" ]; then \
		tty -s && true || { colors=no; true; } \
	fi; \
	test "x$$colors" = "xyes" && \
		echo -e "\033[1mCompiling $(notdir $@)...\033[0m" || \
		echo "Compiling $(notdir $@)...";
#	@if [ ! "x$(ENABLE_RELEASE)" = "xyes" ]; then \#
#		$(top_srcdir)/build/dll-map-makefile-verifier $(srcdir)/Makefile.am $(srcdir)/$(notdir $@.config) && \
#		$(MONO) $(top_builddir)/build/dll-map-verifier.exe $(srcdir)/$(notdir $@.config) -iwinmm -ilibbanshee -ilibbnpx11 -ilibc -ilibc.so.6 -iintl -ilibmtp.dll $(SOURCES_BUILD); \
#	fi;
	@$(BUILD) $(GMCS_FLAGS) -nowarn:0078 -target:$(TARGET) -out:$@ $$warn -define:HAVE_GTK_2_10 -define:NET_2_0 $(BUILD_DEFINES) $(ENABLE_TESTS_FLAG) $(FILTERED_LINK) $(RESOURCES_BUILD) $(SOURCES_BUILD) 
	@if [ -e $(srcdir)/$(notdir $@.config) ]; then \
		cp $(srcdir)/$(notdir $@.config) $(top_builddir)/bin; \
	fi;
	@if [ ! -z "$(EXTRA_BUNDLE)" ]; then \
		cp $(EXTRA_BUNDLE) $(top_builddir)/bin; \
	fi;

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED)

CLEANFILES = $(OUTPUT_FILES)
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

