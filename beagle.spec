Summary:     The Beagle Search Infrastructure
Name:        beagle
Version:     0.0.2
Release:     1
License:     LGPL
Group:       Applications/Development
Source:      beagle-%{version}.tar.gz
BuildRoot:   /var/tmp/%{name}-root
BuildPrereq: evolution-sharp, mono
Requires:    evolution-sharp >= 0.4
Prefix:	     /opt/gnome

%description
A general infrastructure for making your data easy to find. 

%prep
%setup -q

%build
./configure --prefix=%{_prefix} \
	--localstatedir=/var/lib \
	--datadir=%{_prefix}/share
make

%install
rm -rf $RPM_BUILD_ROOT
MAKE=${MAKE:-make}
DESTDIR=${DESTDIR:-"$RPM_BUILD_ROOT"}
case "${RPM_COMMAND:-all}" in
install|all)
        make install DESTDIR=${DESTDIR}
        ;;
esac

%clean
rm -rf $RPM_BUILD_ROOT

%post

%files
%defattr(-,root,root)
%doc COPYING README
%{_prefix}/lib/beagle/*
%{_prefix}/bin/beagle*
%{_prefix}/bin/best
%{_prefix}/bin/searchomatic
%{_prefix}/lib/epiphany/extensions/*beagle*
%{_prefix}/lib/*beagle*
%{_prefix}/lib/pkgconfig/beagle*

%changelog
* Thu Aug 27 2004 Nat Friedman <nat@novell.com>
- initial packaging of 0.0.3
