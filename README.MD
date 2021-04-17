% WS(1) ws 0.0.1
% Marijn Stevens
% April 2021

# WS - WebServer

ws - host a webserver of an directory.

## SYNOPSIS
**ws** [*OPTIONS*]... [*DIRECTORY*]... 

## DESCRIPTION
**ws** can be used to host a static website, or it can just host the directory 
so you can access it from a website.
As default, the port will be a random value between 8000 and 65535 will 
start the default browser with that directory as http://localhost. 

## OPTIONS
**-h**, **--help**
: Display commana-line options. 

**-v**, **--verbose**
: Show more debug information while hosting the server.

**-p**, **--port**
: Set the port number to be used. Ignores the WS_PORT. Must be an 
valid number of the value is ignored.


## EXAMPLES
**ws**
: Host the current working directory or if set WS_DIRECTORY.

**ws --verbose ~/website1/**
: Host the directory in the home directory named website1 and display 
verbose messages.

**ws --port 5000**
: Host the directory on port 5000.

# EXIT VALUES 
**0**
: Success.

**1**
: An error occurred.

## BUGS
Unless you set a specific port with the **-p, --port** options, a 
random port is picked. It doesn't check if the port is in use.
You can set the WS_PORT enviroment variable for a default port to be used.

## COPYRIGHT
Copyright © 2021 Marijn Stevens. License GPLv2. GNU GPL version 2 only

<https://www.gnu.org/licenses/old-licenses/gpl-2.0.html>.
This is free software: you are free to change and redistribute it. 
There is NO WARRANTY, to the extent permitted by law.