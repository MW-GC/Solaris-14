using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Shared.Humanoid;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// The Provocateur is a pacifist agent focused on social manipulation and creating distrust among the crew.
/// Unlike the ninja who uses advanced tech for sabotage, the Provocateur uses deception and framing
/// to turn the crew against each other. They have kill objectives but cannot kill directly.
/// </summary>
public sealed class ProvocateurRuleSystem : GameRuleSystem<ProvocateurRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProvocateurRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);

        SubscribeLocalEvent<ProvocateurRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    // Greeting upon provocateur activation
    private void AfterAntagSelected(Entity<ProvocateurRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
    {
        var ent = args.EntityUid;
        _antag.SendBriefing(ent, MakeBriefing(ent), null, null);
    }

    // Character screen briefing
    private void OnGetBriefing(Entity<ProvocateurRoleComponent> role, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if (ent is null)
            return;
        args.Append(MakeBriefing(ent.Value));
    }

    private string MakeBriefing(EntityUid ent)
    {
        var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
        var briefing = isHuman
            ? Loc.GetString("provocateur-role-greeting-human")
            : Loc.GetString("provocateur-role-greeting-animal");

        if (isHuman)
            briefing += "\n \n" + Loc.GetString("provocateur-role-greeting-equipment") + "\n";

        return briefing;
    }
}
