#!/usr/bin/env perl -an

if ($. == 1) {
  # new file: open a new output file
  open(OUTPUT,"| tee $ARGV.new");
  # first line is special (don't have to be adjusted)
  print OUTPUT $_;
  $offset = 0;
} else {
  # compensate for mysterios extra column
  print OUTPUT "$F[0]\t$offset\t$F[-1]\n";
  $offset += $F[-1];
}

# restart line counter
if (eof) {
  close ARGV;
  close OUTPUT;
  rename "$ARGV", "$ARGV.bak";
  rename "$ARGV.new", "$ARGV";
}
