using HtmlAgilityPack;
using Microsoft.Playwright;

namespace CrawlNet.Crawler
{
    internal class Crawl
    {
        // Method to get all website links on a given URL
        public static async Task<List<string>> GetWebsiteLinksAsync(string urlString)
        {
            List<string> links = [];

            // Load the HTML document from the given URL
            HtmlWeb web = new();
            HtmlDocument htmlDoc = web.Load(urlString);

            // Select all anchor tags
            HtmlNodeCollection anchorNodes = htmlDoc.DocumentNode.SelectNodes("//a");

            // If anchor tags exist, extract href attributes
            if (anchorNodes != null)
            {
                foreach (var anchorNode in anchorNodes)
                {
                    string hrefValue = anchorNode.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(hrefValue) && (hrefValue.StartsWith("http://") || hrefValue.StartsWith("https://")))
                    {
                        links.Add(hrefValue);
                    }
                }
            }

            return await Task.FromResult(links);
        }

        // Method to get the content of a specific page element based on a given CSS selector
        public static async Task<HtmlNode> GetPageContentAsync(string urlString, string selector)
        {
            // Initialize HtmlWeb to load the HTML document from the given URL
            HtmlWeb web = new();
            HtmlDocument htmlDoc = web.Load(urlString);

            // Select a single node based on the given CSS selector
            HtmlNode htmlNode = htmlDoc.DocumentNode.SelectSingleNode(selector);

            // Return the selected node
            return await Task.FromResult(htmlNode);
        }

        // Method to download the robots.txt file from a given URL
        public static async Task<string> DownloadRobotsTxtAsync(string urlString)
        {
            try
            {
                // Append "robots.txt" to the URL
                urlString += "robots.txt";
                using var client = new HttpClient();
                var response = await client.GetAsync(urlString);
                if (response.IsSuccessStatusCode)
                {
                    // Return the content of the robots.txt file if the request is successful
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the download
                Console.WriteLine("Error downloading robots.txt: " + ex.Message);
            }
            // Return a failure message if the download fails
            return "failed to fetch";
        }

        // Separator for splitting lines in the robots.txt file
        private static readonly string[] separator = ["\r\n", "\r", "\n"];

        // Method to parse the robots.txt file and extract user-agent rules
        public static Dictionary<string, List<string>> ParseRobotsTxt(string robotsTxt)
        {
            var userAgentRules = new Dictionary<string, List<string>>();
            string[] lines = robotsTxt.Split(separator, StringSplitOptions.None);
            string? currentUserAgent = null;

            foreach (string line in lines)
            {
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(':');
                if (parts.Length < 2) continue;

                string directive = parts[0].Trim().ToLower();
                string value = parts[1].Trim();

                if (directive == "user-agent")
                {
                    // Set the current user-agent
                    currentUserAgent = value;
                    if (!userAgentRules.ContainsKey(currentUserAgent))
                    {
                        userAgentRules[currentUserAgent] = [];
                    }
                }
                else if ((directive == "disallow" || directive == "allow") && currentUserAgent != null)
                {
                    // Add the directive and value to the current user-agent's rules
                    userAgentRules[currentUserAgent].Add(directive + ":" + value);
                }
            }

            // Return the parsed user-agent rules
            return userAgentRules;
        }

        // Method to check if scraping is allowed based on user-agent rules and page URL
        public static bool IsAllowedToScrape(string userAgent, string pageUrl, Dictionary<string, List<string>> userAgentRules)
        {
            if (!userAgentRules.TryGetValue(userAgent, out List<string>? value))
            {
                // Allow scraping if no rules are found for the user-agent
                return true;
            }

            var rules = value;
            bool allowed = true;

            foreach (var rule in rules)
            {
                string[] ruleParts = rule.Split(':');
                string directive = ruleParts[0];
                string rulePath = ruleParts[1];

                if (pageUrl.StartsWith(rulePath))
                {
                    if (directive == "disallow")
                    {
                        // Disallow scraping if the directive is "disallow"
                        allowed = false;
                    }
                    else if (directive == "allow")
                    {
                        // Allow scraping if the directive is "allow"
                        allowed = true;
                    }
                }
            }

            // Return whether scraping is allowed
            return allowed;
        }

        // Method to check the robots.txt file and determine if scraping is allowed for a specific page
        public static async Task<bool> CheckRobotsTxt(string scrapeSiteUrl, string scrapePageUrl)
        {
            string userAgent = "ET";
            string robotsTxtUrl = scrapeSiteUrl + "robots.txt";
            string robotsTxt = await DownloadRobotsTxtAsync(robotsTxtUrl);
            var userAgentRules = ParseRobotsTxt(robotsTxt);
            bool canScrape = IsAllowedToScrape(userAgent, scrapePageUrl, userAgentRules) || IsAllowedToScrape("*", scrapePageUrl, userAgentRules);

            // Return whether scraping is allowed
            if (canScrape)
            {
                return true;
            }
            return false;
        }


        // Gets the rendered HTML of a webpage using Playwright
        public static async Task<HtmlDocument> GetRenderedHtmlAsync(string url)
        {
            // Initialize Playwright and launch the browser
            using var playwright = await Playwright.CreateAsync();

            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true // Don't show the browser
            });

            // Create a new page
            var page = await browser.NewPageAsync();

            // Navigate to the specified URL
            await page.GotoAsync(url);

            // Wait for the page to load completely
            await page.WaitForLoadStateAsync();

            // Get the page content
            var content = await page.ContentAsync();

            // Load the content into an HtmlDocument
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(content);

            // Return the rendered HTML document
            return htmlDoc;
        }
    }
}
