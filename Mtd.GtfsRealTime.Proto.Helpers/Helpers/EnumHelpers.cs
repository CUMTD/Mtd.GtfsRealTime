using static TransitRealtime.Alert.Types;

namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

/// <summary>
/// Extension methods that map database string values to GTFS-RT
/// <see cref="Alert.Types.Cause"/> and <see cref="Alert.Types.Effect"/> enumerations.
/// </summary>
/// <remarks>
/// Unrecognized cause strings fall back to <see cref="Alert.Types.Cause.OtherCause"/>;
/// unrecognized effect strings fall back to <see cref="Alert.Types.Effect.UnknownEffect"/>.
/// </remarks>
public static class EnumHelpers
{
	/// <summary>
	/// Maps a human-readable cause string (as stored in the database) to its
	/// GTFS-RT <see cref="Alert.Types.Cause"/> equivalent.
	/// </summary>
	/// <param name="reason">The cause string from the database.</param>
	/// <returns>The matching <see cref="Alert.Types.Cause"/>, or <see cref="Alert.Types.Cause.OtherCause"/> if unrecognized.</returns>
	public static Cause ToCauseEnum(this string reason) => reason switch
	{
		"Unknown cause" => Cause.UnknownCause,
		"Technical problem" => Cause.TechnicalProblem,
		"Strike" => Cause.Strike,
		"Demonstration" => Cause.Demonstration,
		"Accident" => Cause.Accident,
		"Holiday" => Cause.Holiday,
		"Weather" => Cause.Weather,
		"Maintenance" => Cause.Maintenance,
		"Construction" => Cause.Construction,
		"Police activity" => Cause.PoliceActivity,
		"Medical emergency" => Cause.MedicalEmergency,
		_ => Cause.OtherCause
	};

	/// <summary>
	/// Maps a human-readable effect string (as stored in the database) to its
	/// GTFS-RT <see cref="Alert.Types.Effect"/> equivalent.
	/// </summary>
	/// <param name="effect">The effect string from the database.</param>
	/// <returns>The matching <see cref="Alert.Types.Effect"/>, or <see cref="Alert.Types.Effect.UnknownEffect"/> if unrecognized.</returns>
	public static Effect ToEffectEnum(this string effect) => effect switch
	{
		"Unknown effect" => Effect.UnknownEffect,
		"No service" => Effect.NoService,
		"Reduced service" => Effect.ReducedService,
		"Significant delays" => Effect.SignificantDelays,
		"Detour" => Effect.Detour,
		"Additional service" => Effect.AdditionalService,
		"Modified service" => Effect.ModifiedService,
		"Stop moved" => Effect.StopMoved,
		_ => Effect.UnknownEffect
	};

}
