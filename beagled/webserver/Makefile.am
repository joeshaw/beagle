MCS = mcs -g

MCSFLAGS= -debug+ -debug:full -nologo -target:library 

XSP_TARGET = bgXsp.dll

CLEANFILES = $(XSP_TARGET)
EXTRA_DIST = $(xsp_only) 

ASSEMBLIES =	-r:System 		\
		-r:System.Web 		\
		-r:System.Runtime.Remoting	

xsp_only =  $(srcdir)/IApplicationHost.cs \
	  $(srcdir)/MonoWorkerRequest.cs \
	  $(srcdir)/Tracing.cs \
	  $(srcdir)/ApplicationServer.cs \
	  $(srcdir)/LingeringNetworkStream.cs \
	  $(srcdir)/BaseApplicationHost.cs \
	  $(srcdir)/BaseRequestBroker.cs \
	  $(srcdir)/IWebSource.cs \
	  $(srcdir)/server.cs \
	  $(srcdir)/InitialWorkerRequest.cs \
	  $(srcdir)/XSPApplicationHost.cs \
	  $(srcdir)/XSPWorkerRequest.cs \
		$(srcdir)/AssemblyInfo.cs


$(XSP_TARGET): $(xsp_only) 
	$(MCS) $(MCSFLAGS) /out:$(XSP_TARGET) $(ASSEMBLIES) $(xsp_only) 

all: $(XSP_TARGET)

install-data-local: $(XSP_TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(XSP_TARGET) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(XSP_TARGET)
