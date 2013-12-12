This proxy project was written in C# using Visual Studio 2012. In bin/Release there is an executable that can be run without building. 
Otherwise, Visual Studio 2012 will need to be opened and load in the ProxyServer.sln solution file and rebuild the executable.
The proxy server runs on port 8888 and sends all traffic out to the server on port 80.
The program will print all Headers and POST requests to standard out. Response data was omitted as it can get to be too much
very quickly and cause problems (printing 10MB in text to the terminal in under a second causes problems). There are three
results files in the root directory, "cnn headers only.txt", "cs455.txt", and "EECS orgsync browsing.txt". These files are example
output of the program running on websites. The cs455.txt file has the response data included for completeness as it was just HTML.