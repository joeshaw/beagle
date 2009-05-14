xemaildir=$(pkglibdir)
xemail_SCRIPTS = xemail-net.dll
EXTRA_DIST = $(xemail_sources) $(xemail_sources_in)
CLEANFILES = xemail-net.dll
DISTCLEANFILES = AssemblyInfo.cs Makefile.in
DEPENDENCIES_LIBS = -r:System.Web -r:Mono.Security

xemail_sources_in = AssemblyInfo.cs.in
xemail_generated_sources = $(xemail_sources_in:.in=)
xemail_sources = *.cs

xemail_build_sources = $(addprefix $(srcdir)/, $(xemail_sources))
#xemail_build_sources += $(xemail_generated_sources)

xemail-net.dll: $(xemail_build_sources)
	$(MCS) -target:library -out:$@ -d:MONO $(xemail_build_sources) $(DEPENDENCIES_LIBS)

-include $(top_srcdir)/git.mk
