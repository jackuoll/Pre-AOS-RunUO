using Server.Items;
using Server.Misc;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class BountyGuard : WarriorGuard
    {
        [Constructable]
        public BountyGuard()
        {
        }

        private static readonly List<int> RejectedHeadSayings = new List<int>()
        {
            500661, // I shall place this on my mantle!
            500662, // This tasteth like chicken.
            500663, // This tasteth just like the juicy peach I just ate.
            500664, // Ahh!  That was the one piece I was missing!
            500665, // Somehow, it reminds me of mother.
            500666, // It's a sign!  I can see Elvis in this!
            500667, // Thanks, I was missing mine.
            500668, // I'll put this in the lost-and-found box.
            500669 // My family will eat well tonight!
        };

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            var bh = dropped as BountiedHead;
            if (bh != null && bh.Player != null && !bh.Player.Deleted &&
                bh.Created + TimeSpan.FromDays(1.0) > DateTime.UtcNow)
            {
                SayTo(from, 500670); // Ah, a head!  Let me check to see if there is a bounty on this.
                Timer.DelayCall(TimeSpan.FromSeconds(5.0), CheckBountyOnHead, bh);
                return true;
            }
            else if (dropped is Head)
            {
                Say(RejectedHeadSayings[Utility.Random(RejectedHeadSayings.Count)]);
                return true;
            }

            SayTo(from, 500660); // If this were the head of a murderer, I would check for a bounty.
            return false;
        }

        private void CheckBountyOnHead(BountiedHead head)
        {
            var bi = BountyInformation.GetBountyInformation(head.Player);

            if (bi == null)
            {
                Say("The reward on this scoundrel's head has already been claimed!");
                return;
            }

            var totalBounty = bi.Bounty;
            var headBounty = head.Bounty;
            var difference = totalBounty - headBounty;

            if (difference < 0)
            {
                Say("The reward on this scoundrel's head has already been claimed!");
                return;
            }

            bi.SubtractBounty(headBounty);

            if (headBounty >= 15000)
                Say(string.Format(
                        "Thou hast brought the infamous {0} to justice!  Thy reward of {1}gp hath been deposited in thy bank account.",
                        head.Player.Name, headBounty));
            else if (headBounty > 100)
                Say(string.Format(
                        "Tis a minor criminal, thank thee. Thy reward of {0}gp hath been deposited in thy bank account.",
                        headBounty));
            else
                Say(string.Format("Thou hast wasted thy time for {0}gp.", headBounty));
        }

        public BountyGuard(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}