set encoding utf8
set terminal qt   # or: wxt
unset output

set logscale x
set xrange [5:*]

set xlabel "Počet partií"
set ylabel "Odchylka od očekávané hodnoty"

set grid
set key top right

set style fill transparent solid 0.25
set border linewidth 1.5

plot \
    "deviance.dat" using 1:2:4 with filledcurves lc rgb "#4C72B0" title "Hranice", \
    "deviance.dat" using 1:3 with lines lw 2 lc rgb "#1f77b4" title "Průměr"
