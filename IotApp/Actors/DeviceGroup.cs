using System;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using IotApp.Protocols;

namespace IotApp.Actors
{
    public class DeviceGroup : UntypedActor
    {
        private IImmutableDictionary<string, IActorRef> deviceIdToActor = ImmutableDictionary<string, IActorRef>.Empty;
        private IImmutableDictionary<IActorRef, string> actorToDeviceId = ImmutableDictionary<IActorRef, string>.Empty;

        public DeviceGroup(string groupId)
        {
            GroupId = groupId;
        }

        protected override void PreStart() => Log.Info($"Device group {GroupId} started");
        protected override void PostStop() => Log.Info($"Device group {GroupId} stopped");

        protected ILoggingAdapter Log { get; } = Context.GetLogger();
        protected string GroupId { get; }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestAllTemperatures r:
                    Context.ActorOf(DeviceGroupQuery.Props(actorToDeviceId, r.RequestId, Sender, TimeSpan.FromSeconds(3)));
                    break;
                case RequestTrackDevice trackMsg when trackMsg.GroupId.Equals(GroupId):
                    if (deviceIdToActor.TryGetValue(trackMsg.DeviceId, out var actorRef))
                    {
                        actorRef.Forward(trackMsg);
                    }
                    else
                    {
                        Log.Info($"Creating device actor for {trackMsg.DeviceId}");
                        var deviceActor = Context.ActorOf(Device.Props(trackMsg.GroupId, trackMsg.DeviceId), $"device-{trackMsg.DeviceId}");
                        Context.Watch(deviceActor);
                        actorToDeviceId = actorToDeviceId.Add(deviceActor, trackMsg.DeviceId);
                        deviceIdToActor = deviceIdToActor.Add(trackMsg.DeviceId, deviceActor);
                        deviceActor.Forward(trackMsg);
                    }
                    break;
                case RequestTrackDevice trackMsg:
                    Log.Warning($"Ignoring TrackDevice request for {trackMsg.GroupId}. This actor is responsible for {GroupId}.");
                    break;
                case RequestDeviceList deviceList:
                    Sender.Tell(new ReplyDeviceList(deviceList.RequestId, deviceIdToActor.Keys.ToImmutableHashSet()));
                    break;
                case Terminated t:
                    var deviceId = actorToDeviceId[t.ActorRef];
                    Log.Info($"Device actor for {deviceId} has been terminated");
                    actorToDeviceId = actorToDeviceId.Remove(t.ActorRef);
                    deviceIdToActor = deviceIdToActor.Remove(deviceId);
                    break;
            }
        }

        public static Props Props(string groupId) => Akka.Actor.Props.Create(() => new DeviceGroup(groupId));
    }
}