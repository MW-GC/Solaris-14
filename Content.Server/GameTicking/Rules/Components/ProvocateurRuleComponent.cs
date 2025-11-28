namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="ProvocateurRuleSystem"/>.
/// The Provocateur is a pacifist agent focused on social manipulation and creating distrust.
/// Unlike the ninja (tech-focused sabotage), the Provocateur uses deception to turn crew against each other.
/// They have kill objectives but cannot kill directly - they must manipulate the crew into doing it.
/// </summary>
[RegisterComponent, Access(typeof(ProvocateurRuleSystem))]
public sealed partial class ProvocateurRuleComponent : Component;
