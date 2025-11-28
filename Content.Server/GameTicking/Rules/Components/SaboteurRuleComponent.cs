namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="SaboteurRuleSystem"/>.
/// The Provocateur/Saboteur is an agent focused on social manipulation and creating distrust.
/// Unlike the ninja (tech-focused sabotage), the Provocateur uses deception to turn crew against each other.
/// </summary>
[RegisterComponent, Access(typeof(SaboteurRuleSystem))]
public sealed partial class SaboteurRuleComponent : Component;
