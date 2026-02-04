set encoding utf8

# Basic style
set style data histograms
set style histogram rowstacked
set style fill solid 1.0 border -1
set boxwidth 0.6

# Labels
set ylabel "Počet her" font ",12"

# Grid lines
set grid ytics lc rgb "#d3d3d3" lw 1
set grid noxtics

# Legend styling
set key outside right top vertical
set key title "Obtížnosti"  # bold title
set key box lw 1 lc rgb "#aaaaaa"           # light gray border box
set key spacing 1.5                         # more space between entries
set key samplen 2                           # shorter color sample lines
set key opaque                               # opaque background
set key invert                               # order from top to bottom (optional)

# X-axis handled automatically from first column
set xtics rotate by -45 font ",10"

# Plot the 4 segments
plot 'unityStatistikaBarCharts.dat' using 2:xtic(1) title "Velmi lehké" linecolor rgb "#30d048", \
     '' using 3 title "Lehké"        linecolor rgb "#abe125", \
     '' using 4 title "Těžké"        linecolor rgb "#f18200", \
     '' using 5 title "Velmi těžké"  linecolor rgb "#f10016"
