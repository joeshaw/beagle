CSC=mcs -g
LIBFLAGS =				\
	-target:library			\
	-r:gtk-sharp.dll		\
	-r:ICSharpCode.SharpZipLib.dll

# Found at http://sharptoolbox.madgeek.com/
HAP_CSFILES =					\
	HtmlAgilityPack/crc32.cs		\
	HtmlAgilityPack/Header.cs		\
	HtmlAgilityPack/HtmlAttribute.cs	\
	HtmlAgilityPack/HtmlDocument.cs		\
	HtmlAgilityPack/HtmlEntity.cs		\
	HtmlAgilityPack/HtmlNode.cs		\
	HtmlAgilityPack/HtmlNodeNavigator.cs	\
	HtmlAgilityPack/MixedCodeDocument.cs	\
	HtmlAgilityPack/tools.cs


FILTERS_CSFILES = 		\
	$(HAP_CSFILES)		\
	gnome.cs		\
	Filter.cs		\
	FilterHtml.cs		\
	FilterOpenOffice.cs	\
	FilterText.cs

Filters.dll: $(FILTERS_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Filters.dll
