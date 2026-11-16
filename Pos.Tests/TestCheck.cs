namespace Pos.Tests;

public static class TestCheck
{
    public static void FileError(Action action)
    {
        try { action(); }
        catch (IOException) { return; }
        catch (UnauthorizedAccessException) { return; }
        throw new Exception("Ожидалась ошибка доступа к файлу.");
    }
}
