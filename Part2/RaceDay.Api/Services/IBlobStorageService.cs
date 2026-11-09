namespace RaceDay.Api.Services;

/// <summary>All file handling goes through the API, so the MVC app never talks to Azure directly.</summary>
public interface IBlobStorageService
{
    /// <summary>Uploads a file and returns the generated blob name.</summary>
    Task<string> UploadAsync(Stream content, string contentType, string extension);

    /// <summary>Opens a stored file, or returns null if it does not exist.</summary>
    Task<(Stream Content, string ContentType)?> OpenReadAsync(string blobName);
}
