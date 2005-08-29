#!/bin/sh

# this script is for development use only... it is used to fetch updated
# sources from an svn checkout of entagged-sharp to include in the local
# copy of the sonance development tree - do not run if you don't know 
# what you're doing.

# The TREE_SOURCE variable should point to the TOP of an entagged-sharp 
# svn checkout. After setting, sync sources by executing this script fom the
# sonance/entagged directory

MONO_BRANCH="entagged-sharp"
svn co svn+ssh://$(whoami)@mono-cvs.ximian.com/source/trunk/$MONO_BRANCH

TREE_SOURCE="./$MONO_BRANCH"

#@DO NOT EDIT@#

TREE_BRANCHES="Ape Ape/Util Mpc Mpc/Util M4a M4a/Util Mp3 Mp3/Util Mp3/Util/Id3frames Flac Flac/Util Ogg Ogg/Util Tracker Tracker/Util Exceptions Util"

rm -f entagged.sources
# create local branches and update source
for branch in $TREE_BRANCHES; do
	mkdir -p $branch;
	cp $TREE_SOURCE/src/$branch/*.cs ./$branch/
done;
cp $TREE_SOURCE/src/*.cs .

rm -rf $MONO_BRANCH

FILES="ENTAGGED_CSFILES = "
for file in `find ./ | grep -e '.cs$'`; do
	TRFILE=`echo "$file" | sed 's/^.\///'`
	FILES="$FILES \$(srcdir)/entagged-sharp/$TRFILE"
done;
echo $FILES > entagged-sharp.sources

