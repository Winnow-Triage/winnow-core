namespace Winnow.API.Services.Ai;

public interface IDuplicateChecker
{
    /// <summary>
    /// Determines if two reports are semantically identical (duplicates) based on their titles and descriptions.
    /// </summary>
    /// <returns>True if they are duplicates, False otherwise.</returns>
    Task<bool> AreDuplicatesAsync(string titleA, string descA, string titleB, string descB, CancellationToken ct);
}
