CSC = mcs -g

XSP_TARGET = bgXsp.dll

XSP_CSFLAGS = -target:library 

XSP_ASSEMBLIES = 			\
	-r:System 			\
	-r:System.Web 			\
	-r:System.Runtime.Remoting	

XSP_CSFILES =  					\
	$(srcdir)/IApplicationHost.cs		\
	$(srcdir)/MonoWorkerRequest.cs		\
	$(srcdir)/Tracing.cs			\
	$(srcdir)/ApplicationServer.cs		\
	$(srcdir)/LingeringNetworkStream.cs	\
	$(srcdir)/BaseApplicationHost.cs	\
	$(srcdir)/BaseRequestBroker.cs		\
	$(srcdir)/IWebSource.cs			\
	$(srcdir)/server.cs			\
	$(srcdir)/InitialWorkerRequest.cs	\
	$(srcdir)/XSPApplicationHost.cs		\
	$(srcdir)/XSPWorkerRequest.cs		\
	$(srcdir)/AssemblyInfo.cs

$(XSP_TARGET): $(XSP_CSFILES)
	$(CSC) -o $@ $(XSP_CSFLAGS) $(XSP_CSFILES) $(XSP_ASSEMBLIES)

all: $(XSP_TARGET)

install-data-local: $(XSP_TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(XSP_TARGET) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(XSP_TARGET)


EXTRA_DIST = $(XSP_CSFILES)

CLEANFILES = $(XSP_TARGET) $(XSP_TARGET).mdb