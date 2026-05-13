using Mtd.GtfsRealTime.Proto.Helpers.Helpers;
using Mtd.Stopwatch.Core.Entities.Schedule;

using TransitRealtime;

namespace Mtd.GtfsRealTime.Proto.Helpers;

public static class RerouteConverter
{
	private const string GtfsRealtimeVersion = "2.0";

	private static IEnumerable<EntitySelector> GetInformedEntities(Reroute reroute) => reroute
			.AffectedRoutes
			.SelectMany(ar => ar.Routes.Select(r => r.Id))
			.Distinct()
			.OrderBy(r => r, StringComparer.Ordinal)
			.Select(id => new EntitySelector
			{
				RouteId = id
			});

	private static TimeRange GetTimeRange(Reroute reroute)
	{
		var timeRange = new TimeRange
		{
			Start = reroute.StartDate.ToPosixTime()
		};
		if (reroute.EndDate.HasValue)
		{
			timeRange.End = reroute.EndDate.Value.ToPosixTime();
		}
		return timeRange;
	}

	private static FeedEntity ConvertRerouteToFeedEntity(Reroute reroute)
	{
		var alert = new Alert
		{
			Cause = reroute.Reason.ToCauseEnum(),
			Effect = reroute.Effect.ToEffectEnum(),
			HeaderText = reroute.Title.ToTranslatedString(),
			DescriptionText = reroute.BriefDescription?.ToTranslatedString(),
			Url = $"https://mtd.org/maps-and-schedules/reroutes/reroute/{reroute.Id}".ToTranslatedString(),
		};
		alert.InformedEntity.AddRange(GetInformedEntities(reroute));
		alert.ActivePeriod.Add(GetTimeRange(reroute));

		var entity = new FeedEntity
		{
			Id = reroute.Id.ToString(),
			Alert = alert
		};

		return entity;
	}

	public static FeedMessage ConvertToFeedMessage(this IEnumerable<Reroute> reroutes)
	{
		var feedMessage = new FeedMessage
		{
			Header = new FeedHeader
			{
				GtfsRealtimeVersion = GtfsRealtimeVersion,
				Timestamp = DateTime.UtcNow.ToPosixTime()
			}
		};

		feedMessage.Entity.AddRange(reroutes.Select(ConvertRerouteToFeedEntity));

		return feedMessage;
	}
}
