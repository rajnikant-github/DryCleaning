namespace DryCleaning.Services.Mock;

/// <summary>Demo mode: the photo stays on the phone, in the demo user record.</summary>
public sealed class MockUserPhotoService(MockDataStore store, ISessionService session) : IUserPhotoService
{
    public Task<string?> UploadAsync(string localPath)
    {
        Set(localPath);
        return Task.FromResult<string?>(localPath);
    }

    public Task RemoveAsync()
    {
        Set(null);
        return Task.CompletedTask;
    }

    private void Set(string? path)
    {
        if (session.CurrentUser is { } user)
            lock (store.Gate)
                store.UserById(user.Id).PhotoUrl = path;
    }
}
