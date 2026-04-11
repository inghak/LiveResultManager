#!/usr/bin/perl -w

# To log errors via CGI::Carp to browser/logfile

BEGIN {
#    use CGI::Carp qw(carpout fatalsToBrowser);
#     open(LOG, ">> /home/O/obergenn/www/idrett/2008/terminliste-error.log") or
#	 croak("Unable to open mycgi-log: $!\n");
#    carpout(LOG);
}

use CGI::Carp qw(fatalsToBrowser);
use CGI qw/:standard/;
use POSIX;
use HTTP::Date;
use POSIX;
#use Cwd;
use strict;
my ($query, $value);

$query = new CGI;

if ($value = $query->param('csv')) {
    &printcsv;
    
}

    
setlocale(LC_ALL, "no_NO.ISO8859-1");
#setlocale(LC_ALL, "UTF-8");

my $file_rg = "rg.txt";
my $rg_url;
my ($year,$file);
my @funnet_aar = &finncsv;
my $self = self_url();
my $heading = "Terminliste";
my $tmpheading = "Terminliste";
my $found = defined;

if ($year = $query->param('aar')) {
} else {
    $year =  strftime("%Y", localtime(time));
#    $year =  strftime("%Y", localtime(time + (60*60*24*61))); # Skifte år i novevmber
}

while (!$found) {
    if (grep /^$year$/, @funnet_aar) {
	$file = "terminliste-$year.csv";
	$found++;
    }
    else {
	$year--;
#    croak "Couldn't find terminliste for $year";
    }    
}

if ($value = $query->param('excel')) {
    print header(-type=>'application/vnd.ms-excel',
		 -expires=>'now',
		 -encoding=>'utf-8',
		 );
} else {


print header(-charset=>'UTF-8')
}

print start_html(-title => "$heading $year",
	       -author => "haugsvar\@gmail.com",
	       -bgcolor => "white",
	       -encoding => "UTF-8",
	       );

$self = url() . "?";

my ($allaar,$inline);

foreach (sort @funnet_aar) {
    my $string;
    $string = $self . "&aar=$_";
#    $string .= "&inline=yes" if $query->param('inline');
    $allaar .= "[<a href=\"$string\">$_</a>] \n";
}
#print "Hello fra Terminliste!";
#exit 1;


if ($inline = $query->param('inline')){
print qq |
<h1 align=left><font color="#f79646">$heading $year</font></h1>
<p>
    <font color="#f79646">$allaar
    </font>
    |;
$self .= "&inline=yes" unless $self =~ /inline=/;

} else {

print qq|
<table cellpadding=15 cellspacing=15>
<tr>
<td><img src="../images/hmbk.jpg"></td>
<td>
<h1><font align=center color="#f79646">Terminliste $year</font></h1>
<p>
    <font color="#f79646">$allaar
</font>
</td>
</table>

|;
}


print "<table border=1 cellpadding=3>";

my ($line,$field,$color,$time, $dirtime);
my (%txt_resultat);
	%txt_resultat = ("resultat/index.htm", "resultater",
			 "stafres/CLASS.HTM", "resultater",
			 "splhtm/index.htm", "strekktider",
			 "splchtm/COURCE.HTM", "strekktider pr løype",
			 );

open(FILE,"$file") || croak("Couldn't open file $file: $!");
while ($line = <FILE>) {
my    @data = split(/;/,$line);

    if ($. % 2 == 0) {
#	$color = "#99CCCC";
#	$color = "#ffffcc";
	$color = "#ffffff";
    }
    else {
#	$color = "#00CCFF";
#	$color = "#99CCFF";
#	$color = "#CCCCFF";
#	$color = "#ffffda";
	$color = "#ffffaa";
    }
    if ($. == 1) {
 	$color = "#f79646";
	push(@data,"Innbydelse","Resultater","Kart, GPS-spor");
    } elsif ($data[0] =~ /Sommerferie/) {
	$color = "#f79646";
	push(@data,"","","");
	$time = undef;
	$dirtime = undef;

    } else {

	if ($data[0] =~ /\w/) {
	    $data[0] =~ s/januar/jan/i;
	    $data[0] =~ s/februar/feb/ii;
	    $data[0] =~ s/mars/mar/i;
	    $data[0] =~ s/april/apr/i;
	    $data[0] =~ s/mai/may/i;
	    $data[0] =~ s/juni/jun/i;
	    $data[0] =~ s/juli/jul/i;
	    $data[0] =~ s/august/aug/i;
	    $data[0] =~ s/september/sep/i;
	    $data[0] =~ s/oktober/oct/i;
	    $data[0] =~ s/november/nov/i;
	    $data[0] =~ s/desember/doc/i;
	    $data[0] =~ s/\.//i;
	    $data[0] .= " " . $year;
	    
#	    die $data[0];

	}
#die HTTP::Date::parse_date($data[0]),$data[0];
	$time = str2time($data[0]);
	$dirtime = strftime("%Y-%m-%d",localtime($time));
	$data[0] = strftime("%d. %B",localtime($time));
	
	push(@data," "," "," ");
}
    print qq|
<tr bgcolor="$color">
|;


    my ($teller,$innbydelse,$kart);
    foreach $field (@data) {
	$teller++; 
##### Innbydelser
####################
    	if ($teller == 7 && $. > 1 &&  !$query->param('excel')) {
#	    croak getcwd;
	    if (! -d "../$year/$dirtime") {
		mkdir "../$year/$dirtime",0755 || croak ("Couldn't mkdir ../$year/$dirtime: $!");
	    }
	    foreach $innbydelse ("innbydelse.doc","innbydelse.pdf", "innbydelse.htm", "innbydelse.html") {
		if (-f "../$year/$dirtime/$innbydelse") {
		    $field = qq|<a target="_blank" href="../$year/$dirtime/$innbydelse">Innbydelse</a>|;
		}
	    }
	}

$field .= &ifresultat($teller);

if ($teller == 9 && $. > 1 &&  !$query->param('excel')) {
	    if (! -d "../$year/$dirtime") {
		mkdir "../$year/$dirtime",0755 || croak ("Couldn't mkdir ../$year/$dirtime: $!");
	    }

	    ## Link til 3DRerun
	    if ( str2time($dirtime) <= time && $data[0] != /Sommerferie/ ) {
		$field .= qq|<a target="_blank" href="http://3drerun.worldofo.com/index.php?date=$dirtime&lat=60.3724&lng=5.3414&tl=1&type=showoverview&dist=1000">3D Rerun</a> |;
	    }

	    foreach $kart ("LoypeA.pdf","LoypeB.pdf","LoypeC.pdf","LoypeN.pdf") {
		if (-f "../$year/$dirtime/$kart") {
		    $field .= qq|<a target="_blank" href="../$year/$dirtime/$kart">$kart</a> |;
		}
	    }

	    foreach $kart ("LoypeA.JPG","LoypeB.JPG","LoypeC.JPG","LoypeN.JPG") {
		if (-f "../$year/$dirtime/$kart") {
		    $field .= qq|<a target="_blank" href="../$year/$dirtime/$kart">$kart</a> |;
		}
	    }
	}

	print qq|
<td>$field</td>
|;
	
}
    print qq|
</tr>
|;
    

}
print "</table>
";


print qq|
<p>
I 2026 kreves det deltagelse på 12 løp for å få deltakerpremie, og rankingen har 12 tellende løp. Dersom vi ikke får arrangør til det siste løpet vil vi redusere til 11. NB: Det blir ikke Tangen glass i 2026, men premie blir det.
<p>
Spørsmål og melding om feil vedrørende ranking, statistikk, ubegrunnet diskvalifikasjoner, resultater eller strekktider meldes til <a href="mailto:resultatservice\@o-bergen.no">resultatservice\@o-bergen.no</a> senest to arbeidsdager etter at resultatene er publisert. Rettelser og korrigeringer som meldes senere enn dette blir dessverre ikke tatt hensyn til i ranking, statistikk o.l. 
    |;
print  end_html;

#close LOG;


sub printcsv {
    my $file = "terminliste.csv"; 
    print header(-type=>'application/octet-stream',
		 -expires=>'now',
		 -charset=>'utf-8',
		 -attachment=>"$file",
		 );
   my $size = -s "$file";
    open(FILE,"$file") || croak "Couldn't open file $file: $!";
    my @file = <FILE>;
    close FILE;
    print @file;
    return 1;
}


sub ifresultat {
    my $teller = shift;
    my $field;
    my ($resultat);
    my %bmtext = (
		  'bm-db.html' => 'Bergensmesterskap D16B- D70B',
		  'bm-hb.html' => 'Bergensmesterkap H60B og H70B',
		  'bm-ha.html' => 'Bergensmesterkap H16A - H50A',
		  );
    
    if ($teller == 8 && $. > 1 && ! $query->param('excel')) {
	foreach $resultat (sort {$txt_resultat{$a} cmp $txt_resultat{$b} } keys %txt_resultat) {
	    if (-f "../$year/$dirtime/data/htmlfiler/$resultat") {
#		print "<td>../$year/$dirtime/data/htmlfiler/$resultat";
		$field .= qq| <a target="_blank" href="../$year/$dirtime/data/htmlfiler/$resultat">$txt_resultat{$resultat}</a> |;
		
		
	    }
	}
	if (-f "../$year/$dirtime/data/htmlfiler/Sploype.csv") {
	    $field .=  qq|<a target="_blank" href="http://events.worldofo.com/woosplits/?obergen=1&id=$dirtime"><font color="red">Splitanalyser</font></a></font><br> |;
	}
	my $file_rg = "../$year/$dirtime/data/htmlfiler/$file_rg";
#	    croak (getcwd, "/$file_rg");
	    if (-f "$file_rg") {
#		croak $file_rg;
		open(RG,"$file_rg") || croak "Couldn't open file $file_rg";
#		$field .= $file_rg;
		$rg_url = <RG>;
		close RG;
		$field .= qq| <a target="_blank" href="$rg_url"><font color="red">Routegadget</font></a></font><br> |;
		    
		}

	my $bmdir = "../$year/$dirtime/data/htmlfiler/bm";
	if (-d $bmdir) {
	    
	    opendir(my $dh,$bmdir || croak "Couldn't open dir $bmdir");
	    my $fil;
	    while ($fil = readdir $dh) {
		next unless $fil =~ /bm.*html$/;
#		$field .= $bmdir ."<br>$fil\<n>";
#		    $field .= "a:$_";
#		    carp $_;
#		    carp($_) if /bm*.html/;
			       
		    $field .= qq| <a target="_blank" href="$bmdir/$fil"><font color="red">$bmtext{$fil}</font></a></font> <br>|;
		}
	closedir $dh;
	}	   
	      
    }
    return $field;
}


sub finncsv {
    my $dir = ".";
    my @aar;
    my $file;
    opendir(DIR,"$dir") || croak "Couldn't open $dir: $!";
    foreach $file (grep /^terminliste-\d{4}\.csv$/,readdir(DIR)) {
	$file =~ /terminliste-(\d{4}).csv/;
	push(@aar,$1);
    }
return @aar;

}


