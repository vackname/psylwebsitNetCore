ps aux | grep productCar | awk '{ cpup += $3; memp += $4; memval += $6; pid = pid $2 ","; masterck = masterck $12 ",";   } END { print cpup"_"memp"_"memval"_"pid"_"masterck }'