set encoding utf8

set xlabel "Pole hráče" font "CMU Serif,12"
set ylabel "Kolo"        font "CMU Serif,12"

unset key
set view map scale 1
set style data lines
set xtics border in scale 0,0 mirror norotate  autojustify
set ytics border in scale 0,0 mirror norotate  autojustify
set ztics border in scale 0,0 nomirror norotate  autojustify
unset cbtics
set rtics border in scale 0,0 nomirror norotate  autojustify
set title "Rozdělení dvou figurek dvou hráčů \npři jednoduché hře Človče nezlob se" font "CMU Serif,25"
set xrange [ -0.5 : 19.5 ] reverse
set yrange [ -0.5 : 79.5 ] reverse
set cblabel "Score" 
set cbrange [ 0.00000 : 1 ] noreverse
set palette rgbformulae 21, 22, 23
NO_ANIMATION = 1

set rgbmax 1.0
plot 'figurky.dat' using 2:1:3:4:5 with rgbimage