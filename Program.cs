class Program
{
    static void Main(string[] args)
    {
        string inputDir = @"C:\Users\admin\Desktop\input";
        string blocklistPath = @"C:\Users\admin\compare\blocklist\blocklist.txt";
        string outputCsvPath = @"C:\Users\admin\compare\matches.csv";

        // Read all domains into a HashSet for faster lookup
        var blocklist = new HashSet<string>(File.ReadAllLines(blocklistPath));

        // Get all CSV files
        var csvFiles = Directory.GetFiles(inputDir, "*.csv");

        // Prepare the CSV output with headers
        File.WriteAllText(outputCsvPath, "FileName,URL,BlocklistedDomain\n");

        // Use PLINQ to process files in parallel
        csvFiles.AsParallel().ForAll(file =>
        {
            var data = File.ReadLines(file).Skip(1); // Assuming the first line is headers

            foreach (var line in data)
            {
                var columns = line.Split(',');
                var url = columns[0]; // Assuming URLs are in the first column
                if (string.IsNullOrWhiteSpace(url)) continue;

                string domain = ExtractDomain(url);
                if (domain != null && blocklist.Contains(domain))
                {
                    string csvLine = $"{Path.GetFileName(file)},\"{url}\",\"{domain}\"\n";
                    // Thread-safe append to CSV file
                    lock (outputCsvPath)
                    {
                        File.AppendAllText(outputCsvPath, csvLine);
                    }
                }
            }
        });
    }

    // Extracts the domain from a URL
    static string ExtractDomain(string url)
    {
        // Exclude local file URLs and blob URLs
        if (url.StartsWith("file:///") || url.StartsWith("blob:"))
        {
            return null; // Return null to skip processing
        }

        try
        {
            var uri = new Uri(url);
            string domain = uri.Host;
            return domain.StartsWith("www.") ? domain.Substring(4) : domain;
        }
        catch (UriFormatException)
        {
            return null; // or handle errors as needed
        }
    }
}