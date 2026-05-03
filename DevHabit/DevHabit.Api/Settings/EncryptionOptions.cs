namespace DevHabit.Api.Settings;

public sealed class EncryptionOptions
{
	public const string SectionName = "Encryption";
	
    // A good practice is to implement a Key rotation.
    public required string Key { get; init; }
}
