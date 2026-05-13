using static TransitRealtime.Alert.Types;

namespace Mtd.GtfsRealTime.Proto.Helpers.Helpers;

public static class EnumHelpers
{

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
