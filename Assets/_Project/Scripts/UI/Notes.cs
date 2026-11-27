using System.Collections.Generic;
using UnityEngine;

// every tip the player has found kept between runs so the menus can list them again
public static class Notes
{
    private const string SaveKey = "notes"; // playerprefs key
    private const char Separator = '\u001f'; // between notes in the saved text and never typed in a tip

    private static List<string> found; // in the order they were found

    // every note in the order it was found
    public static IReadOnlyList<string> All
    {
        get
        {
            Load();
            return found;
        }
    }

    // adds a note the first time it is found and saves the list
    public static void Add(string text)
    {
        Load();
        if (found.Contains(text)) return;
        found.Add(text);
        PlayerPrefs.SetString(SaveKey, string.Join(Separator.ToString(), found));
        PlayerPrefs.Save();
    }

    // reads the saved notes the first time they are needed
    private static void Load()
    {
        if (found != null) return;
        string saved = PlayerPrefs.GetString(SaveKey, "");
        found = saved == "" ? new List<string>() : new List<string>(saved.Split(Separator));
    }
}