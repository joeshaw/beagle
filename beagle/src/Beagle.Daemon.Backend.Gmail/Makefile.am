ASSEMBLY = Beagle.Daemon.Backend.Gmail
ASSEMBLY_BUILD_FLAGS = -define:MONO
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:System.Web -r:Mono.Security -pkg:gmime-sharp

SOURCES = \
	Beagle.Daemon.Backend.Gmail/GMailSearchDriver.cs	\
	xemail-net/src/Attachment.cs				\
	xemail-net/src/AttachmentCollections.cs 		\
	xemail-net/src/Flag.cs					\
	xemail-net/src/FlagCollection.cs			\
	xemail-net/src/HMACMD5.cs				\
	xemail-net/src/ImapClient.cs				\
	xemail-net/src/Mail.cs					\
	xemail-net/src/MailCollection.cs			\
	xemail-net/src/Mailbox.cs				\
	xemail-net/src/MailboxCollection.cs			\
	xemail-net/src/MessageSet.cs				\
	xemail-net/src/Namespace.cs				\
	xemail-net/src/Quota.cs

include $(top_srcdir)/build/build.mk
