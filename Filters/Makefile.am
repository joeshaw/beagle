CSC=mcs -g
LIBFLAGS =				\
	-target:library			\
	-r:ICSharpCode.SharpZipLib.dll

# Found at http://sharptoolbox.madgeek.com/
HTML_CSFILES =					\
	HtmlAgilityPack/crc32.cs		\
	HtmlAgilityPack/Header.cs		\
	HtmlAgilityPack/HtmlAttribute.cs	\
	HtmlAgilityPack/HtmlDocument.cs		\
	HtmlAgilityPack/HtmlEntity.cs		\
	HtmlAgilityPack/HtmlNode.cs		\
	HtmlAgilityPack/HtmlNodeNavigator.cs	\
	HtmlAgilityPack/MixedCodeDocument.cs	\
	HtmlAgilityPack/tools.cs		\
	FilterHtml.cs

FILTERS_CSFILES = 		\
	Filter.cs		\
	FilterOpenOffice.cs	\
	FilterText.cs

Filters.dll: $(FILTERS_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Filters.dll
