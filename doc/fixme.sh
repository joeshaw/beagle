#!/bin/sh
#
# Autogenerate http://nat.org/beagle-fixmes.php3
#

BEAGLE_DIR=/home/nat/cvs/beagle
URL_PREFIX="http://cvs.gnome.org/viewcvs/beagle/"
EXCLUDE_LIST="fixme.sh\|ltmain.sh"

cd $BEAGLE_DIR
timestamp=`date`

echo "<?"
echo "   \$title = \"beagle FIXMEs\";"
echo "   include (\"path.php3\");"
echo "   nat_inc (\"nat-header.php3\");"
echo "?>"
echo 
echo "<table border=0 align=left width=100%><tr><td>"
echo "<h3>Automatically generated at $timestamp.</h3>"
echo "<br><tt>"

# Get list of FIXMEs
find . |grep "\(\.\(c\|h\|cs\|txt\|sh\)\|Makefile.am\)$" |xargs grep -n FIXME |grep -v $EXCLUDE_LIST |sed "s/\.\///g" | (
while true
do
  read fixme

  if [ "x$fixme" = "x" ]
  then
      exit
  fi

  path=`echo "$fixme" |cut -d: -f 1`
  line=`echo "$fixme" |cut -d: -f 2`
  rest=`echo "$fixme" |cut -d: -f 3-`

  url="$URL_PREFIX/$path?view=markup#$line"

  echo "<a href=\"$url\">$path:$line</a>&nbsp;&nbsp;$rest<br>"

done
)

echo "</tt>"
echo "</td></tr></table>"
echo "<? "
echo "   nat_inc (\"nat-plain-footer.php3\");"
echo "?>"



