ASSEMBLY = Beagle
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -pkg:glib-sharp-2.0 -r:Mono.Posix

SOURCES = \
	Beagle/AssemblyInfo.cs			\
	Beagle/Hit.cs				\
	Beagle/IIndexableGenerator.cs		\
	Beagle/Indexable.cs			\
	Beagle/IndexingService.cs		\
	Beagle/IndexingStatusResponse.cs	\
	Beagle/InformationalMessagesRequest.cs	\
	Beagle/Message.cs			\
	Beagle/Property.cs			\
	Beagle/Query.cs				\
	Beagle/QueryPart.cs			\
	Beagle/QueryResponses.cs		\
	Beagle/QueryableStatus.cs		\
	Beagle/RemoteControl.cs			\
	Beagle/RemovableIndexControl.cs		\
	Beagle/Snippet.cs			\
	Beagle/Transport.cs			\
	Beagle/UnixTransport.cs			\
	Beagle/Versioned.cs

include $(top_srcdir)/build/build.mk
