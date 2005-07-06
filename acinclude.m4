dnl Add compile warnings

AC_DEFUN([IDT_COMPILE_WARNINGS],[
   if test -f $srcdir/autogen.sh; then
	default_compile_warnings="error"
    else
	default_compile_warnings="no"
    fi

    AC_ARG_WITH(compile-warnings,
                AS_HELP_STRING([--with-compile-warnings=@<:@no/yes/error@:>@],
                               [Compiler warnings]),
                [enable_compile_warnings="$withval"],
                [enable_compile_warnings="$default_compile_warnings"])

    warnCFLAGS=
    if test "x$GCC" != xyes; then
	enable_compile_warnings=no
    fi

    warning_flags=
    realsave_CFLAGS="$CFLAGS"

    case "$enable_compile_warnings" in
    no)
	warning_flags=
	;;
    yes)
	warning_flags="-Wall -Wunused -Wmissing-prototypes -Wmissing-declarations"
	;;
    maximum|error)
	warning_flags="-Wall -Wunused -Wchar-subscripts -Wmissing-declarations -Wmissing-prototypes -Wnested-externs -Wpointer-arith"
	CFLAGS="$warning_flags $CFLAGS"
	for option in -Wno-sign-compare; do
		SAVE_CFLAGS="$CFLAGS"
		CFLAGS="$CFLAGS $option"
		AC_MSG_CHECKING([whether gcc understands $option])
		AC_TRY_COMPILE([], [],
			has_option=yes,
			has_option=no,)
		CFLAGS="$SAVE_CFLAGS"
		AC_MSG_RESULT($has_option)
		if test $has_option = yes; then
		  warning_flags="$warning_flags $option"
		fi
		unset has_option
		unset SAVE_CFLAGS
	done
	unset option
	if test "$enable_compile_warnings" = "error" ; then
	    warning_flags="$warning_flags -Werror"
	fi
	;;
    *)
	AC_MSG_ERROR(Unknown argument '$enable_compile_warnings' to --enable-compile-warnings)
	;;
    esac
    CFLAGS="$realsave_CFLAGS"
    AC_MSG_CHECKING(what warning flags to pass to the C compiler)
    AC_MSG_RESULT($warning_flags)

    WARN_CFLAGS="$warning_flags"
    AC_SUBST(WARN_CFLAGS)
])


dnl -*- mode: autoconf -*-

# serial 1

dnl Usage:
dnl   GTK_DOC_CHECK([minimum-gtk-doc-version])
AC_DEFUN([GTK_DOC_CHECK],
[
  AC_BEFORE([AC_PROG_LIBTOOL],[$0])dnl setup libtool first
  AC_BEFORE([AM_PROG_LIBTOOL],[$0])dnl setup libtool first
  dnl for overriding the documentation installation directory
  AC_ARG_WITH(html-dir,
    AC_HELP_STRING([--with-html-dir=PATH], [path to installed docs]),,
    [with_html_dir='${datadir}/gtk-doc/html'])
  HTML_DIR="$with_html_dir"
  AC_SUBST(HTML_DIR)

  dnl enable/disable documentation building
  AC_ARG_ENABLE(gtk-doc,
    AC_HELP_STRING([--enable-gtk-doc],
                   [use gtk-doc to build documentation [default=no]]),,
    enable_gtk_doc=no)

  have_gtk_doc=no
  if test x$enable_gtk_doc = xyes; then
    if test -z "$PKG_CONFIG"; then
      AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
    fi
    if test "$PKG_CONFIG" != "no" && $PKG_CONFIG --exists gtk-doc; then
      have_gtk_doc=yes
    fi

  dnl do we want to do a version check?
ifelse([$1],[],,
    [gtk_doc_min_version=$1
    if test "$have_gtk_doc" = yes; then
      AC_MSG_CHECKING([gtk-doc version >= $gtk_doc_min_version])
      if $PKG_CONFIG --atleast-version $gtk_doc_min_version gtk-doc; then
        AC_MSG_RESULT(yes)
      else
        AC_MSG_RESULT(no)
        have_gtk_doc=no
      fi
    fi
])
    if test "$have_gtk_doc" != yes; then
      enable_gtk_doc=no
    fi
  fi

  AM_CONDITIONAL(ENABLE_GTK_DOC, test x$enable_gtk_doc = xyes)
  AM_CONDITIONAL(GTK_DOC_USE_LIBTOOL, test -n "$LIBTOOL")
])
