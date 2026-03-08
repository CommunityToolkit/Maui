namespace CommunityToolkit.Maui;

/// <summary>Used by CommunityToolkit.Maui source generators to define access modifiers for generated code</summary>
public enum AccessModifier
{
	/// <summary>Generates <see langword="public"/> access modifier</summary>
	Public = 0,
	/// <summary>Generates <see langword="internal"/> access modifier</summary>
	Internal = 1,
	/// <summary>Generates <see langword="protected"/> <see langword="internal"/> access modifier</summary>
	ProtectedInternal = 2,
	/// <summary>Generates <see langword="protected"/> access modifier</summary>
	Protected = 3,
	/// <summary>Generates <see langword="private"/> <see langword="protected"/> access modifier</summary>
	PrivateProtected = 4,
	/// <summary>Generates <see langword="private"/> access modifier</summary>
	Private = 5,
	/// <summary>Generates no code</summary>
	None = 6
}