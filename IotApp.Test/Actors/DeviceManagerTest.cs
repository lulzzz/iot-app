using Akka.Actor;
using Akka.TestKit.Xunit2;
using IotApp.Actors;
using IotApp.Protocols;
using FluentAssertions;
using Xunit;
using System;

namespace IotApp.Tests.Actors
{
    public class DeviceManagerTest : TestKit
    {
        [Fact]
        public void DeviceManager_actor_must_be_able_to_register_a_device_group_actor()
        {
            var probe = CreateTestProbe();
            var managerActor = Sys.ActorOf(DeviceManager.Props());

            managerActor.Tell(new RequestTrackDevice("group1", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor1 = probe.LastSender;

            managerActor.Tell(new RequestTrackDevice("group2", "device2"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor2 = probe.LastSender;
            deviceActor1.Should().NotBe(deviceActor2);

            // Check that the device actors are working
            deviceActor1.Tell(new RecordTemperature(requestId: 0, value: 1.0), probe.Ref);
            probe.ExpectMsg<TemperatureRecorded>(s => s.RequestId == 0);
            deviceActor2.Tell(new RecordTemperature(requestId: 1, value: 2.0), probe.Ref);
            probe.ExpectMsg<TemperatureRecorded>(s => s.RequestId == 1);
        }

        [Fact]
        public void DeviceManager_actor_must_return_same_actor_for_same__groupId_and_deviceId()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceManager.Props());

            groupActor.Tell(new RequestTrackDevice("group1", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor1 = probe.LastSender;

            groupActor.Tell(new RequestTrackDevice("group1", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor2 = probe.LastSender;

            deviceActor1.Should().Be(deviceActor2);
        }

        [Fact]
        public void DeviceManager_actor_must_be_able_to_manage_one_actor_shuts_down()
        {
            var probe = CreateTestProbe();
            var groupActor = Sys.ActorOf(DeviceManager.Props());

            groupActor.Tell(new RequestTrackDevice("group1", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor1 = probe.LastSender;

            probe.Watch(deviceActor1);
            deviceActor1.Tell(PoisonPill.Instance);
            probe.ExpectTerminated(deviceActor1);

            groupActor.Tell(new RequestTrackDevice("group1", "device1"), probe.Ref);
            probe.ExpectMsg<DeviceRegistered>();
            var deviceActor2 = probe.LastSender;

            deviceActor1.Should().NotBe(deviceActor2);
        }

    }
}
