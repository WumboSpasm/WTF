using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

public class WTF
{
    private static List<Entry> metadata = Database.Get();
    private static List<Entry> filteredMetadata = new();
    private static List<Genre> genres = new();

    private static bool requestsNSFW;
    private static int entryNum;
    private static Random rng = new();

    public static void Main()
    {
        Console.SetWindowSize(64, 16);
        Console.SetBufferSize(64, 16);
        Console.CursorVisible = false;
        
        StartScreen();
    }

    private static void StartScreen()
    {
        string[] options = { "SFW entry", "NSFW entry" };
        int cursor = 0;

        while (true)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Wumbo's Tagger for Flashpoint" + Environment.NewLine);
            Console.WriteLine("What type of entry would you like to tag?" + Environment.NewLine);

            for (int i = 0; i < options.Length; i++)
            {
                if (i == cursor)
                    ConsoleExt.Write(options[i] + Environment.NewLine, Console.BackgroundColor, Console.ForegroundColor);
                else
                    Console.WriteLine(options[i]);
            }

            ConsoleKey pressedKey = Console.ReadKey(true).Key;

            if (pressedKey == ConsoleKey.DownArrow)
                cursor = (cursor + 1) % options.Length;
            if (pressedKey == ConsoleKey.UpArrow)
                cursor = (cursor - 1 + options.Length) % options.Length;

            if (pressedKey == ConsoleKey.Enter)
                break;
        }

        Console.WriteLine(Environment.NewLine + "Hold on...");

        if (cursor == 0)
        {
            requestsNSFW = false;

            foreach (Entry entry in metadata)
                if (!entry.Tags.Contains(';'))
                    filteredMetadata.Add(entry);
        }
        else
        {
            requestsNSFW = true;

            foreach (Entry entry in metadata)
                if (entry.Tags.Contains("LEGACY-Extreme") && entry.Tags.Count(chr => chr == ';') == 1)
                    filteredMetadata.Add(entry);
        }

        if (!File.Exists("tags.json"))
        {
            Console.WriteLine(@"ERROR: tags.json not found");
            Environment.Exit(1);
        }

        using (StreamReader jsonStream = new("tags.json"))
        {
            string tagsJSON = jsonStream.ReadToEnd();

            dynamic? filterArray = JsonConvert.DeserializeObject(tagsJSON);

            foreach (var item in filterArray)
                genres.Add(new Genre
                {
                    Title = item.title,
                    GenreIsTag = item.genreIsTag,
                    Sexual = item.sexual,
                    Tags = item.tags.ToObject<string[]>()
                });
        }

        PlayScreen();
    }

    public static void PlayScreen()
    {
        string[] options = { "Play this entry and get tagging", "Show me a different entry" };
        int cursor = 0;

        Console.Clear();

        entryNum = rng.Next(filteredMetadata.Count);

        while (true)
        {
            string tag = "";

            // Separate tag from LEGACY-Extreme if needed
            if (filteredMetadata[entryNum].Tags.Contains(';'))
            {
                string[] tags = filteredMetadata[entryNum].Tags.Split("; ");
                tag = tags[(Array.IndexOf(tags, "LEGACY-Extreme") + 1) % 2];
            }
            else
                tag = filteredMetadata[entryNum].Tags;

            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Wumbo's Tagger for Flashpoint" + Environment.NewLine);

            Console.WriteLine("The program has selected an entry that needs tagging: " + Environment.NewLine);
            Console.WriteLine(filteredMetadata[entryNum].Title + Environment.NewLine);

            Console.WriteLine($"Its only tag is {tag}." + Environment.NewLine);

            for (int i = 0; i < options.Length; i++)
            {
                if (i == cursor)
                    ConsoleExt.Write(options[i] + Environment.NewLine, Console.BackgroundColor, Console.ForegroundColor);
                else
                    Console.WriteLine(options[i]);
            }

            ConsoleKey pressedKey = Console.ReadKey(true).Key;

            if (pressedKey == ConsoleKey.DownArrow)
                cursor = (cursor + 1) % options.Length;
            if (pressedKey == ConsoleKey.UpArrow)
                cursor = (cursor - 1 + options.Length) % options.Length;

            if (pressedKey == ConsoleKey.Enter)
            {
                if (cursor == 0)
                    break;
                if (cursor == 1)
                {
                    entryNum = rng.Next(filteredMetadata.Count);
                    Console.Clear();
                }
            }
        }

        Database.StartEntry(filteredMetadata[entryNum].ID);
        GenreScreen();
    }

    public static void GenreScreen()
    {
        int cursorSel = 0;
        int scrollPos = 0;

        List<string> genreOptions = new(); 
        List<string> selectedGenres = new();

        foreach (Genre genre in genres)
        {
            if (genre.Sexual && !requestsNSFW)
                continue;

            genreOptions.Add(genre.Title);
        }

        while (true)
        {
            Console.Clear();
            
            Console.WriteLine("Wumbo's Tagger for Flashpoint" + Environment.NewLine);

            Console.WriteLine("Select the genres that apply to the entry:");

            int visibleOptions = Console.BufferHeight - 4;

            for (int i = scrollPos; i < Math.Min(genreOptions.Count, scrollPos + visibleOptions); i++)
            {
                Console.WriteLine();

                if (selectedGenres.Contains(genreOptions[i]))
                    Console.Write("X ");
                else
                    Console.Write("  ");

                if (i == cursorSel)
                    ConsoleExt.Write(genreOptions[i], Console.BackgroundColor, Console.ForegroundColor);
                else
                    Console.Write(genreOptions[i]);
            }

            ConsoleKey pressedKey = Console.ReadKey(true).Key;

            if (pressedKey == ConsoleKey.DownArrow)
            {
                cursorSel = (cursorSel + 1) % genreOptions.Count;

                if (cursorSel >= scrollPos + visibleOptions)
                    scrollPos += 1;

                if (cursorSel == 0)
                    scrollPos = 0;
            }
            if (pressedKey == ConsoleKey.UpArrow)
            {
                cursorSel = (cursorSel - 1 + genreOptions.Count) % genreOptions.Count;

                if (cursorSel < scrollPos)
                    scrollPos -= 1;

                if (cursorSel == genreOptions.Count - 1)
                    scrollPos = Math.Max(genreOptions.Count - visibleOptions, 0);
            }
            if (pressedKey == ConsoleKey.Spacebar)
            {
                if (!selectedGenres.Contains(genreOptions[cursorSel]))
                    selectedGenres.Add(genreOptions[cursorSel]);
                else
                    selectedGenres.Remove(genreOptions[cursorSel]);
            }

            if (pressedKey == ConsoleKey.Enter && selectedGenres.Count > 0)
                break;
        }

        TagScreen(selectedGenres);
    }

    public static void TagScreen(List<string> selectedGenres)
    {
        selectedGenres.Sort();
        List<string> selectedTags = new();

        foreach (string genre in selectedGenres)
        {
            int cursorSel = 0;
            int scrollPos = 0;

            int genreIndex = genres.FindIndex(g => g.Title.Equals(genre));
            List<string> tagOptions = genres[genreIndex].Tags.ToList();

            if (genres[genreIndex].GenreIsTag)
                tagOptions.Add(genres[genreIndex].Title);

            tagOptions.Sort();

            while (true)
            {
                Console.Clear();

                Console.WriteLine("Wumbo's Tagger for Flashpoint" + Environment.NewLine);

                string tagText = "Selected tags: " + String.Join(", ", selectedTags);
                Console.WriteLine(tagText);

                Console.WriteLine(Environment.NewLine + $"Select the {genre} tags that apply to the entry:");

                int headerHeight = 6 + (tagText.Length / Console.BufferWidth);
                int visibleOptions = Console.BufferHeight - headerHeight;

                for (int i = scrollPos; i < Math.Min(tagOptions.Count, scrollPos + visibleOptions); i++)
                {
                    Console.WriteLine();

                    if (selectedTags.Contains(tagOptions[i]))
                        Console.Write("X ");
                    else
                        Console.Write("  ");

                    if (i == cursorSel)
                        ConsoleExt.Write(tagOptions[i], Console.BackgroundColor, Console.ForegroundColor);
                    else
                        Console.Write(tagOptions[i]);
                }

                ConsoleKey pressedKey = Console.ReadKey(true).Key;

                if (pressedKey == ConsoleKey.DownArrow)
                {
                    cursorSel = (cursorSel + 1) % tagOptions.Count;

                    if (cursorSel >= scrollPos + visibleOptions)
                        scrollPos += 1;

                    if (cursorSel == 0)
                        scrollPos = 0;
                }
                if (pressedKey == ConsoleKey.UpArrow)
                {
                    cursorSel = (cursorSel - 1 + tagOptions.Count) % tagOptions.Count;

                    if (cursorSel < scrollPos)
                        scrollPos -= 1;

                    if (cursorSel == tagOptions.Count - 1)
                        scrollPos = Math.Max(tagOptions.Count - visibleOptions, 0);
                }
                if (pressedKey == ConsoleKey.Spacebar)
                {
                    if (!selectedTags.Contains(tagOptions[cursorSel]))
                        selectedTags.Add(tagOptions[cursorSel]);
                    else
                        selectedTags.Remove(tagOptions[cursorSel]);
                }

                if (pressedKey == ConsoleKey.Enter)
                    break;
            }
        }

        TagConfirmation(selectedTags);
    }

    public static void TagConfirmation(List<string> selectedTags)
    {
        Console.Clear();

        string[] options = { "Yes, export please", "No, I want to start over" };
        int cursor = 0;

        while (true)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Wumbo's Tagger for Flashpoint" + Environment.NewLine);
            Console.WriteLine($"A metadata edit for {filteredMetadata[entryNum].Title} will be created with the following tags:");
            Console.WriteLine(Environment.NewLine + String.Join(", ", selectedTags) + Environment.NewLine);
            Console.WriteLine("Are these tags correct?" + Environment.NewLine);

            for (int i = 0; i < options.Length; i++)
            {
                if (i == cursor)
                    ConsoleExt.Write(options[i] + Environment.NewLine, Console.BackgroundColor, Console.ForegroundColor);
                else
                    Console.WriteLine(options[i]);
            }

            ConsoleKey pressedKey = Console.ReadKey(true).Key;

            if (pressedKey == ConsoleKey.DownArrow)
                cursor = (cursor + 1) % options.Length;
            if (pressedKey == ConsoleKey.UpArrow)
                cursor = (cursor - 1 + options.Length) % options.Length;

            if (pressedKey == ConsoleKey.Enter)
                break;
        }

        if (cursor == 0)
        {
            Directory.CreateDirectory("WTFExport");

            File.WriteAllText(
                $"WTFExport\\{filteredMetadata[entryNum].ID}.json",
                "{\"metas\":[{\"id\":\"" + filteredMetadata[entryNum].ID +
                "\",\"tags\":[\"" + String.Join("\",\"", selectedTags) + "\"]}]}"
            );

            Console.WriteLine(Environment.NewLine + "Done! Press any key to restart the app...");
            Console.ReadKey(true);

            Console.Clear();
            Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }
        else
            GenreScreen();
    }
}

public class ConsoleExt
{
    public static void Write(string text, ConsoleColor foreground, ConsoleColor background)
    {
        Console.ForegroundColor = foreground;
        Console.BackgroundColor = background;
        Console.Write(text);
        Console.ResetColor();
    }
}

public class Database
{
    public static List<Entry> Get()
    {
        if (!File.Exists(@"..\Data\flashpoint.sqlite"))
        {
            Console.WriteLine(@"ERROR: ..\Data\flashpoint.sqlite not found");
            Environment.Exit(1);
        }

        SqliteConnection connection = new(@"Data Source=..\Data\flashpoint.sqlite");
        connection.Open();

        SqliteCommand command = new("SELECT title, tagsStr, id FROM game", connection);

        List<Entry> data = new();

        using (SqliteDataReader dataReader = command.ExecuteReader())
            while (dataReader.Read())
                data.Add(new Entry
                {
                    Title = dataReader.IsDBNull(0) ? "" : dataReader.GetString(0), // Title
                    Tags  = dataReader.IsDBNull(1) ? "" : dataReader.GetString(1), // Tags
                    ID    = dataReader.IsDBNull(2) ? "" : dataReader.GetString(2)  // ID
                });

        connection.Close();

        return data;
    }

    public static void StartEntry(string id)
    {
        if (!File.Exists(@"..\CLIFp\CLIFp.exe"))
        {
            Console.WriteLine(@"ERROR: ..\CLIFp\CLIFp.exe not found");
            Environment.Exit(1);
        }

        Process CLIFp = new();

        CLIFp.StartInfo.FileName = @"..\CLIFp\CLIFp.exe";
        CLIFp.StartInfo.Arguments = $"play -i {id}";
        CLIFp.Start();
    }
}

// Templates
public class Entry
{
    public string Title { get; set; } = "";
    public string Tags { get; set; } = "";
    public string ID { get; set; } = "";
}
public class Genre
{
    public string Title { get; set; } = "";
    public bool GenreIsTag { get; set; } = false;
    public bool Sexual { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
}