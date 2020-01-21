using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;


namespace WebCrawler
{
    class Program
    {
        static HashSet<String> visitedUris = new HashSet<String>();
        static int numHopsRemaining = 5;
        static string baseUri = "http://www.amazon";
        static string endingHtmlResponse = null;
        static readonly string regexPattern = @"<(?<Tag_Name>(a))\b[^>]*?\b(?<URL_Type>(?(1)href))\s*=\s*(?:""(?<URL>(?:\\""|[^""])*)""|'(?<URL>(?:\\'|[^'])*)')";

        static void Main(string[] args)
        {
            switch (args.Length)
            {
                case 0:
                case 1:
                    Console.WriteLine("Requires 2 arguments, try again");
                    return;
                case 2:
                    try
                    {
                        baseUri = args[0].ToString().TrimEnd();
                        numHopsRemaining = Int32.Parse(args[1]);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Argument excpetion: " + e);
                    }
                    break;
                default:
                    Console.WriteLine("Unknown URL or Hops.");
                    return;
            }
            if (numHopsRemaining > 0)
            {
                HttpGetRequest(baseUri);
            }
            else
            {
                Console.WriteLine("You requested 0 hops: " + baseUri);
            }
        }

        private static void HttpGetRequest(string currentUri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            HttpClientHandler clientHandler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            using (var client = new HttpClient(clientHandler))

            {
                if (currentUri != null && Uri.IsWellFormedUriString(currentUri, UriKind.Absolute) && numHopsRemaining >= 0)
                {
                    if (CheckUrlExceptions(client, currentUri))
                    {
                        visitedUris.Add(currentUri);
                        if (currentUri.Substring(currentUri.Length - 1).Equals('/'))
                        {
                            visitedUris.Add(currentUri.Remove(currentUri.Length - 1));
                        }
                        else
                        {
                            visitedUris.Add(currentUri + '/');
                        }
                        numHopsRemaining--;
                        try
                        {
                            HttpResponseMessage response = client.GetAsync(currentUri).Result;
                            HttpGetRequest(SearchCurrentHtml(response));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Failed to reach: " + currentUri + "This was the Exception:" + e);
                            Environment.Exit(0);
                        }

                    }
                    else
                    {
                        Console.WriteLine("Problem with URL: " + currentUri);
                    }

                }
                else
                {
                    Console.WriteLine("Last hop URL: " + visitedUris.ElementAtOrDefault(visitedUris.Count - 1));
                    if (endingHtmlResponse != null)
                    {
                        Console.WriteLine("Ending HTML code: \n" + endingHtmlResponse);
                        return;
                    }
                }

            }
            return;
        }

        static string SearchCurrentHtml(HttpResponseMessage currentHtmlResponse)
        {
            CheckResponseStatus((int)currentHtmlResponse.StatusCode);
            string htmlCode = currentHtmlResponse.Content.ReadAsStringAsync().Result;
            if (numHopsRemaining == 0)
            {
                endingHtmlResponse = htmlCode;
            }
            MatchCollection listOfRegexMatches = Regex.Matches(htmlCode,
                regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            foreach (Match match in listOfRegexMatches)
            {
                string urlValue = match.Groups["URL"].Value.ToString();
                if (urlValue.Length >= 4 && urlValue.Substring(0, 4).Equals("http") && !visitedUris.Contains(urlValue))
                {
                    Console.WriteLine("Printing next URL: " + urlValue);
                    return urlValue;
                }
            }
            return null;
        }

        static void CheckResponseStatus(int responseCode)
        {
            switch (responseCode)
            {
                case 300:
                    Console.WriteLine("Reponse code 300: More than one result from URI.");
                    return;
                case 301:
                    Console.WriteLine("Reponse code 301: Destination relocated.");
                    return;
                case 400:
                    Console.WriteLine("Reponse code 400: Bad URL request.");
                    return;
                case 401:
                    Console.WriteLine("Reponse code 401: Unauthorized request.");
                    return;
                case 402:
                    Console.WriteLine("Reponse code 402: Payment required for request.");
                    return;
                case 403:
                    Console.WriteLine("Reponse code 403: Forbidden request.");
                    return;
                case 404:
                    Console.WriteLine("Reponse code 404: Request not found.");
                    return;
                default:
                    if (responseCode > 301 && responseCode < 400)
                        Console.WriteLine("Redirect request for reponse code: " + responseCode);
                    else if (responseCode > 404 && responseCode < 500)
                        Console.WriteLine("Client error for response code: " + responseCode);
                    else if (responseCode > 504 && responseCode < 600)
                        Console.WriteLine("Internal server error  for response code: " + responseCode);
                    else if (!(responseCode >= 200 && responseCode < 300))
                        Console.WriteLine("Unknown response code found: " + responseCode);
                    return;
            }
        }
        static bool CheckUrlExceptions(HttpClient client, string currentUri)
        {
            if (currentUri != null && Uri.IsWellFormedUriString(currentUri, UriKind.Absolute) && numHopsRemaining >= 0)
            {
                try
                {
                    client.BaseAddress = new Uri(currentUri, UriKind.Absolute);
                    return true;
                }
                catch (AggregateException ae)
                {
                    Console.WriteLine("Cannot resolve the remote name for this uri\nuri: " + currentUri + "\nException:" + ae);
                    return false;
                }
                catch (InvalidOperationException ioe)
                {
                    Console.WriteLine("Cannot resolve operation for this uri\n" + currentUri + "\nException:" + ioe);
                    return false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to create URI\nuri: " + currentUri + "\nException:" + e);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Url cannot be requested: " + currentUri);
                return false;
            }
        }
    }
}
