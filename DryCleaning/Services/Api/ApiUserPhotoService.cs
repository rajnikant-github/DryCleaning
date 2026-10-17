using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

public sealed class ApiUserPhotoService(ApiClient api) : IUserPhotoService
{
    public async Task<string?> UploadAsync(string localPath)
    {
        var user = await api.UploadFileAsync("api/me/photo", localPath, ApiJsonContext.Default.UserDto);
        return ApiMapping.Absolute(user.PhotoUrl);
    }

    public async Task RemoveAsync() =>
        await api.DeleteAsync("api/me/photo");
}
