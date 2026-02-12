gamma = 0.5
rgb_max = 1.0

set encoding utf8

set xlabel "Pole hráče" font "CMU Serif,12"
set ylabel "Kolo"      font "CMU Serif,12"

unset key
set view map scale 1
set style data lines

set xtics border in scale 0,0 mirror norotate autojustify
set ytics border in scale 0,0 mirror norotate autojustify
set ztics border in scale 0,0 nomirror norotate autojustify
set rtics border in scale 0,0 nomirror norotate autojustify
unset cbtics

set xrange [ -0.5 : 19.5 ]
set yrange [ 79.5 : -0.5 ]

set cblabel "Score"
set cbrange [0.0 : 1.0] noreverse

set palette rgbformulae 21,22,23
set rgbmax rgb_max

NO_ANIMATION = 1

plot 'figurky.dat' using \
     2:1: \
     (($3/rgb_max)**gamma): \
     (($4/rgb_max)**gamma): \
     (($5/rgb_max)**gamma) \
     with rgbimage
