# mulping
Simple console application that enables pinging IP addresses range.

To start pinging it is necessary to input first and last IP address and timeout value in milliseconds.  
It can be done either during program execution or by passing parameters in the console.  
Following parameters can be passed:  
-f - first IP address  
-l - last IP address  
-t - timeout value in milliseconds  

Example command to run application:  
mulping.exe -f192.168.0.1 -l192.168.1.255 -t100  

IP addresses range can contain many subnets.  
After pinging is done, a table with IP addresses, replies, times and names is shown.  
