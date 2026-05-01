namespace DevHabit.Api.Settings;

public sealed class EncryptionOptions
{
    // A good practice is to implement a Key rotation.
    public required string Key { get; init; }
}
