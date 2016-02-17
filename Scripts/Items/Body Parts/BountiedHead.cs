using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.Misc;

namespace Server.Items
{
    class BountiedHead : Head
    {
        public static void Initialize()
        {
            EventSink.PlayerDeath += EventSink_PlayerDeath;
        }

        private static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            var killed = e.Mobile;
            var bounty = BountyInformation.GetBounty(killed);
            if (bounty <= 0)
                return;

                var killer =
                killed.DamageEntries.Where(x => x.Damager is PlayerMobile)
                    .OrderByDescending(x => x.DamageGiven)
                    .Select(x => x.Damager)
                    .FirstOrDefault();


            if (killer == null)
                return;

            Timer.DelayCall(TimeSpan.Zero, GiveHeadTo, new object[] {killer, killed, bounty});
        }

        // Possibly the best method name in the history of programming
        private static void GiveHeadTo(object[] me)
        {
            var killer = me[0] as PlayerMobile;
            var killed = me[1] as PlayerMobile;
            var bounty = (int)me[2];
            var corpse = killed.Corpse as Corpse;
            var pack = killer.Backpack;

            // if the corpse does not exist (events, sacrifice?), is carved, not human, or does not have the itemid of a human corpse, terminate
            if (pack == null || corpse == null || corpse.Carved || !((Body)corpse.Amount).IsHuman || corpse.ItemID != 0x2006)
                return;

            corpse.Carved = true;
            var loc = corpse.Location;
            var map = corpse.Map;
            new Blood(0x122D).MoveToWorld(loc, map);

            new Torso().MoveToWorld(loc, map);
            new LeftLeg().MoveToWorld(loc, map);
            new LeftArm().MoveToWorld(loc, map);
            new RightLeg().MoveToWorld(loc, map);
            new RightArm().MoveToWorld(loc, map);
            //new Head(killed.Name).MoveToWorld(loc, map);
            var head = new BountiedHead(killed.Name)
            {
                Player = killed,
                Bounty = bounty,
                Created = DateTime.UtcNow
            };

            corpse.ProcessDelta();
            corpse.SendRemovePacket();
            corpse.ItemID = Utility.Random(0xECA, 9); // bone graphic
            corpse.Hue = 0;
            corpse.ProcessDelta();

            killer.SendLocalizedMessage(500551); // As you kill them, you realize there is a bounty on their head! You take the head as evidence of your victory.
            pack.DropItem(head);
        }

        [CommandProperty(AccessLevel.Counselor)]
        public DateTime Created { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public int Bounty { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public PlayerMobile Player { get; set; }

        public BountiedHead(string killer) : base(killer)
        {
        }

        public BountiedHead(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(Created);
            writer.Write(Bounty);
            writer.Write(Player);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Created = reader.ReadDateTime();
            Bounty = reader.ReadInt();
            Player = reader.ReadMobile<PlayerMobile>();
        }
    }
}