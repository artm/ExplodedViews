#!/usr/bin/env perl -w
use strict;
use warnings;

# use dependencies from support directory next to this script
use FindBin;
use lib "$FindBin::Bin/support/lib/perl5";

use File::Find;
use File::Path qw(make_path);
use File::Basename;
use FileHandle;
use ProgressBar::Stack;
use ProgressBar::Stack::Renderers qw/betterThenDefault/;

use Pod::Usage;
pod2usage() if $#ARGV != 1;

# get root directory names from command line...
my ($ROOT_OF_PLYS, $ROOT_OF_BINS) = @ARGV;

# sentinel...
pod2usage("\nERROR: ROOT_OF_PLYS ($ROOT_OF_PLYS) doesn't exist\n") unless -d $ROOT_OF_PLYS;
make_path($ROOT_OF_BINS) unless -d $ROOT_OF_BINS;

# collect a hash of ply lists 
my %LOC2LST = ();
find({ 
    # preprocess does the actual job, wanted is a dummy (necessary for find)
    wanted => sub { },
    preprocess => sub {
      my @plys = grep(/\.ply$/, @_);
      if (@plys) {
        $LOC2LST{ $File::Find::dir } = \@plys;
        # don't descend no more
        return ();
      } else {
        return grep { -d $_ } @_;
      }
    }
  }, 
$ROOT_OF_PLYS);

# maintain current output paths so signal handler can remove them
my ($bin_name, $map_name) = ("","");

my ($renderer, $clearProgressBar) = betterThenDefault;
my $prog = new ProgressBar::Stack( renderer => $renderer, minupdatevalue => 0 );
$prog->for( sub {
    my $ply_path = $_;
    my $LOC=basename($_);
    $bin_name = "${ROOT_OF_BINS}/${LOC}.bin";
    $map_name = "${ROOT_OF_BINS}/${LOC}.cloud";
    # conversion progress (for progress bar and ETA)
    my $tt = 0;

    &$clearProgressBar;

    if ( -f $bin_name ) {
      print "\n **** $bin_name already exists, skipping $LOC\n";
      print "      To reconvert you'd have to delete $bin_name\n\n";
      return;
    }

    print "Generating " . basename($bin_name) . "\n";

    my $bin = FileHandle->new(">$bin_name");
    $bin->binmode;

    my $cloudmap = FileHandle->new(">$map_name");
    $cloudmap->autoflush;
    $cloudmap->print( "$bin_name\n" );

    $prog->for( sub {
        my $ply_name = "$ply_path/$_";
        &$clearProgressBar;
        print " Storing " . basename($ply_name) . " to " . basename($bin_name) . "\n";

        my $ply = FileHandle->new($ply_name);

        my $vcount = 0;
        while(<$ply>) {
          /element vertex (\d+)/ and $vcount = int $1;
          /end_header/ and last;
        }
        return unless $vcount;

        $prog->for( sub {
            my @point = (split /\s+/,$ply->getline)[0..5];
            # CONVERT Y from German convention (Y down) to Unity's (Y up)
            $point[1] = -$point[1];
            $bin->print( pack("f[3]C[4]", @point, 0) ); # the last 0 is padding
          }, (1..$vcount));

        $ply->close;
        $cloudmap->print( "$ply_name\t$tt\t$vcount\n" );
        $tt += $vcount;
      }, 
      (@{$LOC2LST{$_}}));
  },
  (keys(%LOC2LST))
);

# remove current output files if aborted
use sigtrap qw(handler abort_handler normal-signals);
sub abort_handler {
  # extra newline so the progress bar remains on screen
  print STDERR "\nAborting";
  if (unlink $bin_name, $map_name) {
    die ", output files $bin_name and $map_name deleted\n";
  } else {
    die "...\n";
  }
}

exit 0;

__END__

=head1 NAME

ply2bin - convert multiple directories of ply point clouds to Exploded Views bin format

=head1 SYNOPSIS

PATHTO/ply2bin.pl ROOT_OF_PLYS ROOT_OF_BINS

where:

  - PATHTO is path to the script `ply2bin.pl' (use . (dot) for current directory)
  - ROOT_OF_PLYS is a directory containing location directories which in turn contain
    ply files. This directory must exist.
  - ROOT_OF_BINS is a directory to store convertion results (.bin and .cloud files). 
    This directory will be created if it does not exist.

If .bin file for particular location already exists it will not be regenerated. 

The script can be aborted at any time with Ctrl+C, the unfinished LOCATION.bin and
LOCATION.cloud files will be deleted automatically.

=head1 DESCRIPTION

This program converts multiple directories of ply point clouds to Exploded Views bin format.

=cut

