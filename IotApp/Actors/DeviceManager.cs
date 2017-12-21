using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using IotApp.Protocols;

namespace IotApp.Actors
{
    public class DeviceManager : UntypedActor
    {
        private IImmutableDictionary<string, IActorRef> groupIdToActor = ImmutableDictionary<string, IActorRef>.Empty;
        private IImmutableDictionary<IActorRef, string> actorToGroupId = ImmutableDictionary<IActorRef, string>.Empty;

        protected override void PreStart() => Log.Info("DeviceManager started");
        protected override void PostStop() => Log.Info("DeviceManager stopped");

        protected ILoggingAdapter Log { get; } = Context.GetLogger();

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestTrackDevice trackMsg:
                    if (groupIdToActor.TryGetValue(trackMsg.GroupId, out var actorRef))
                    {
                        actorRef.Forward(trackMsg);
                    }
                    else
                    {
                        Log.Info($"Creating device group actor for {trackMsg.GroupId}");
                        var groupActor = Context.ActorOf(DeviceGroup.Props(trackMsg.GroupId), $"group-{trackMsg.GroupId}");
                        Context.Watch(groupActor);
                        groupActor.Forward(trackMsg);
                        groupIdToActor = groupIdToActor.Add(trackMsg.GroupId, groupActor);
                        actorToGroupId = actorToGroupId.Add(groupActor, trackMsg.GroupId);
                    }
                    break;
                case Terminated t:
                    var groupId = actorToGroupId[t.ActorRef];
                    Log.Info($"Device group actor for {groupId} has been terminated");
                    actorToGroupId = actorToGroupId.Remove(t.ActorRef);
                    groupIdToActor = groupIdToActor.Remove(groupId);
                    break;
            }
        }

        public static Props Props() => Akka.Actor.Props.Create<DeviceManager>();
    }
}