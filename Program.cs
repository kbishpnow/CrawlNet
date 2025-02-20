using CrawlNet.Crawler;

// Example usage of the Crawl class
List<string> links =
[
    "https://example.com",
    "https://www.w3schools.com/",
    "https://en.wikipedia.org/wiki/Web_scraping"
];

// Iterate through each link and check robots.txt
foreach (string link in links)
{
    //send the homepage to get the robots.txt to see if the link can be crawled
    if (await Crawl.CheckRobotsTxt(link, link))
    {
        await FetchLinks(link);
    }
}

// Method to fetch and print links from a given URL
static async Task FetchLinks(string url)
{
    List<string> websiteLinks = await Crawl.GetWebsiteLinksAsync(url);

    // Print the links to the console
    Console.WriteLine($"Links from {url}");
    Console.WriteLine("---");

    foreach (string websiteLink in websiteLinks)
    {
        // Scrape and process the content of each link
        Console.WriteLine(websiteLink);
    }

    Console.WriteLine();
    Console.WriteLine("==========================================");
    Console.WriteLine();
}
