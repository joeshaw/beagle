
CSC = mcs -g

TARGET = Renderers.dll

CSFLAGS = -target:library

CSFILES = 					\
	$(srcdir)/Favicons.cs			\
	$(srcdir)/HitRenderer.cs		\
	$(srcdir)/HitRendererHtml.cs		\
	$(srcdir)/DefaultHitRenderer.cs		\
	$(srcdir)/ContactHitRenderer.cs		\
	$(srcdir)/FileHitRenderer.cs		\
	$(srcdir)/MailMessageHitRenderer.cs	\
	$(srcdir)/WebLinkHitRenderer.cs

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	-r:../Util/Util.dll		\
	-r:../Filters/Filters.dll	\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../indexer/Indexer.dll

imagedir = $(top_srcdir)/images

RESOURCES =                                                  			\
	-resource:$(imagedir)/bookmark.png,bookmark.png 			\
	-resource:$(imagedir)/document.png,document.png 			\
	-resource:$(imagedir)/default.png,default.png     			\
	-resource:$(imagedir)/person.png,person.png     			\
	-resource:$(imagedir)/bug.png,bug.png           			\
	-resource:$(imagedir)/white.png,white.png       			\
	-resource:$(imagedir)/foaf.png,foaf.png         			\
	-resource:$(imagedir)/book.png,book.png         			\
	-resource:$(imagedir)/stock.png,stock.png				\
	-resource:$(imagedir)/globe.png,globe.png				\
	-resource:$(imagedir)/default.png,default.png				\
	-resource:$(imagedir)/stock.png,stock.png				\
        -resource:$(imagedir)/evolution.png,evolution.png  			\
        -resource:$(imagedir)/music.png,music.png				\
	-resource:$(imagedir)/mail.png,mail.png         			\
        -resource:$(imagedir)/attachment.png,attachment.png			\
        -resource:$(imagedir)/flag-for-followup.png,flag-for-followup.png	\
        -resource:$(imagedir)/mail-new.png,mail-new.png				\
        -resource:$(imagedir)/mail-replied.png,mail-replied.png			\
        -resource:$(imagedir)/mail-read.png,mail-read.png			\
	-resource:$(imagedir)/rss.png,rss.png


$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $^ $(ASSEMBLIES) $(RESOURCES) 

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
