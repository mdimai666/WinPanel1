namespace WinPanel1;

public class SaveFile
{
    public List<DockApp> Apps { get; set; }

    string GetJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }

    static string GetSaveDockFilePath()
    {
        var p = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        var path = Path.Combine(p, "WinPanel1", "save.json");
        return path;
    }

    void CheckSaveFolder()
    {
        string dir = Path.GetDirectoryName(GetSaveDockFilePath())!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    public static SaveFile LoadUserData()
    {
        string path = GetSaveDockFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveFile? save = System.Text.Json.JsonSerializer.Deserialize<SaveFile>(json);
            return save ?? new SaveFile();
        }
        return new SaveFile();
    }

    public void Save()
    {
        CheckSaveFolder();
        string path = GetSaveDockFilePath();
        string json = GetJson();
        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}
