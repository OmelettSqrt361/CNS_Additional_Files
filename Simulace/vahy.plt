# -----------------------------------------
# Gnuplot: Vážené poziční strategie (0-1 axes, legend on the side)
# -----------------------------------------

set encoding utf8

# Plot style
set samples 200
set xrange [0:1]
set yrange [0:1]
set xlabel "x" font ",12"
set ylabel "v(x)" font ",12"
set title "Typy vážených strategií a jejich váhové funkce" font ",14"

# Grid
set grid ytics lc rgb "#d3d3d3" lw 1
set grid xtics lc rgb "#d3d3d3" lw 1

# Legend on the right
set key outside right top vertical
set key title "Strategie" font ",12,bold"
set key box lw 1 lc rgb "#aaaaaa"
set key spacing 1.5
set key samplen 3
set key opaque fc rgb "#f0f0f0"

# Colors for curves
Line1 = "#1f77b4"
Line2 = "#ff7f0e"
Line3 = "#2ca02c"
Line4 = "#d62728"
Line5 = "#9467bd"
Line6 = "#8c564b"

# Functions
f1(x) = x
f2(x) = x**2
f3(x) = sqrt(x)
f4(x) = log(x+1)/log(2)
f5(x) = exp(log(2)*x)-1
f6(x) = (1-cos(pi*x))/2

# Plot curves with thicker lines and legend
plot \
    f1(x) title "Lineární (Nevážená)" linecolor rgb Line1 lw 3, \
    f2(x) title "Kvadratická"           linecolor rgb Line2 lw 3, \
    f3(x) title "Odmocninná"           linecolor rgb Line3 lw 3, \
    f4(x) title "Logaritmická"         linecolor rgb Line4 lw 3, \
    f5(x) title "Exponenciální"        linecolor rgb Line5 lw 3, \
    f6(x) title "Sinusoidová"          linecolor rgb Line6 lw 3
