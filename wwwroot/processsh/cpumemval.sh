ps -A -o %cpu,%mem | awk '{s+=$1; s2+=$2} END {print s","s2}'