namespace Pos.Infrastructure;

internal static class AtomicFile
{
    public static void Write(string path, Action<Stream> write, bool overwrite)
    {
        var target = Path.GetFullPath(path);
        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                write(stream);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, target, overwrite);
        }
        catch
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            throw;
        }
    }
}
