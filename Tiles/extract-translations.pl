#!/usr/bin/perl -w

opendir(DIR, ".");
@files = grep(/\.html$/,readdir(DIR));
closedir(DIR);

open (OUTPUT, ">TranslationHack.cs");

print OUTPUT "using Mono.Unix;\n\npublic class TranslationHack {\n\tprivate void NotToBecalled () {\n";

foreach $file (@files) {
    open (TEMPLATE, $file);
    while ($data = <TEMPLATE>) {
	@matches = ($data =~ m/\@text\%([^@]*)\@/g);
	
	for ($i = 0; $i < scalar (@matches); $i++) {
	    print OUTPUT "\t\t/* For translators: From template Tiles/$file */\n";
	    print OUTPUT "\t\tCatalog.GetString (\"$matches[$i]\");\n";
	}
    }
}

print OUTPUT "\t}\n}\n";
close (OUTPUT)



