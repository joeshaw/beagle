CSC=mcs -g

LIBFLAGS =			\
	-target:exe		\
	-r:gtk-sharp		\
	-r:gnome-sharp		\
	-L ../Lucene.Net	\
	-r:Lucene.Net		\
	-L ../Filters		\
	-r:Filters		\
	-L ../indexer		\
	-r:Indexer

BEST_CSFILES =		\
	gnome.cs	\
	Best.cs		\
	BestWindow.cs	\
	HitContainer.cs	\
	HitView.cs

BEST_TARGET = Best.exe

$(BEST_TARGET) : $(BEST_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: $(BEST_TARGET)