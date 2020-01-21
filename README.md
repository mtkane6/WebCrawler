# WebCrawler
Web crawler takes starting URL and number of hops.  Searches for first valid html link, and hops to that.  Repeats "hop" number of times.

I built this web scraper in C#, my first ever experience with the language.  I have a Mac, so I used Visual Studio for Mac in the .net environment. To build and run, place all contents of the zipped file except this word document into one local file on your machine.  Using your terminal or powershell application, navigate to that file.  Once there, simply input: dotnet[space]run[space] <url>[space]<number of hops>

For example:

mitchell@Mitchells-MBP WebCrawler % dotnet run http://www.washington.edu 5’

The output on the screen will either be a handled exception thrown by the program, or it will print the html link it is hopping to next.  It will continue to do to until either the number of hops has been reached, or there are no more valid html links on the current web page.
