namespace AEHotel.API.Identity;

public static class MediaFileSeeder
{
    public static void Seed(IWebHostEnvironment environment)
    {
        var uploads = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(uploads);

        var frontendRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "FinalProject", "client", "hotelreservation", "src", "assets"));
        CopyFolder(Path.Combine(frontendRoot, "images"), uploads);
        CopyFolder(Path.Combine(frontendRoot, "videos"), uploads);
    }

    private static void CopyFolder(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(source))
        {
            var name = Path.GetFileName(file);
            var target = Path.Combine(destination, name);
            if (!File.Exists(target))
            {
                File.Copy(file, target, overwrite: false);
            }
        }
    }
}
