using Robust.Shared.Audio;

namespace Content.Server.Camera;

/// <summary>
/// A handheld camera that can take photos and spawn photo items.
/// </summary>
[RegisterComponent]
public sealed partial class HandheldCameraComponent : Component
{
    /// <summary>
    /// The entity prototype to spawn when a photo is taken.
    /// </summary>
    [DataField("photoPrototype")]
    public string PhotoPrototype = "Photo";

    /// <summary>
    /// Sound to play when taking a photo.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Machines/shutter.ogg");

    /// <summary>
    /// Use delay in seconds to prevent spam.
    /// </summary>
    [DataField("useDelay")]
    public float UseDelay = 2f;
}
