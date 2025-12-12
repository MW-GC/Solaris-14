using Content.Server.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Camera;

public sealed class HandheldCameraSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _useDelays = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldCameraComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, HandheldCameraComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        // Check use delay
        if (_useDelays.TryGetValue(uid, out var lastUse))
        {
            var timeSinceUse = _timing.CurTime - lastUse;
            if (timeSinceUse.TotalSeconds < component.UseDelay)
            {
                var remaining = component.UseDelay - timeSinceUse.TotalSeconds;
                _popup.PopupEntity($"Camera is recharging! ({remaining:F1}s)", uid, args.User);
                return;
            }
        }

        // Take the photo!
        _useDelays[uid] = _timing.CurTime;

        // Play the camera shutter sound
        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);

        // Spawn the photo item
        var coords = Transform(args.User).Coordinates;
        var photo = Spawn(component.PhotoPrototype, coords);

        // Try to put it in the user's hands
        _hands.PickupOrDrop(args.User, photo);

        _popup.PopupEntity("You take a photo!", uid, args.User);

        args.Handled = true;
    }
}
