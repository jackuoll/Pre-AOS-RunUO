using Server.Misc;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Server.Network.Misc;

/*
    TODO: Head creation
    TODO: IdleGuard which collects heads.
    TODO: Serializing BountyInformation with XML
*/

namespace Server.Items
{
    public class BountyBoard : BaseBulletinBoard
    {
        private static List<BountyBoard> _allBoards = new List<BountyBoard>();

        [Constructable]
        public BountyBoard() : base(0x1E5E)
        {
            BoardName = "bounty board";
            _allBoards.Add(this);
            GetInitialBounties();
        }

        private void GetInitialBounties()
        {
            var bountyPlayers = BountyInformation.GetValidBounties().Select(x=>x.BountyPlayer);

            foreach (var bounty in bountyPlayers)
            {
                AddBountyToBoard(bounty);
            }
        }

        public BountyBoard(Serial serial) : base(serial)
        {
            _allBoards.Add(this);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CheckRange(from))
            {
                Cleanup();
                
                NetState state = from.NetState;

                state.Send(new BBDisplayBoard(this));
                if (state.ContainerGridLines)
                    state.Send(new BountyPackets.BBContent6017(from, this));
                else
                    state.Send(new BountyPackets.BBContent(from, this));
            }
            else
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
        }

        public override void Cleanup()
        {
            // Remove any outdated bounties, and remove any posts made by players. Cannot deny player postings without editing BaseBulletinBoard.
            var validBountyPlayers = BountyInformation.GetValidBounties().Select(x=>x.BountyPlayer);
            Items.Where(x => x is BountyMessage && !validBountyPlayers.Contains(((BountyMessage)x).BountyPlayer) || !(x is BountyMessage))
                .ToList()
                .ForEach( message => message.Delete() );
        }

        public override void Delete()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        internal static void GloballyCreateMessage(Mobile m)
        {
            foreach(var bb in _allBoards)
                bb.AddBountyToBoard(m);
        }

        internal static void GloballyDeleteMessage(Mobile m)
        {
            foreach (var bb in _allBoards)
            {
                var curMsg = bb.Items.FirstOrDefault(x => x is BountyMessage && ((BountyMessage)x).BountyPlayer == m);
                if (curMsg != null)
                    curMsg.Delete();
            }
        }

        private void AddBountyToBoard(Mobile m)
        {
            AddItem(new BountyMessage(m));
        }
    }

    public class BountyMessage : BulletinMessage
    {
        private Mobile _bountyPlayer;

        public Mobile BountyPlayer { get { return _bountyPlayer; } }
        public int BountyAmount;
        
        public BountyMessage(Mobile m) : this(m, BountyInformation.GetBounty(m))
        {
        }

        public BountyMessage(Mobile bountied, int bounty) : base(bountied, null, bounty + " gold pieces", CreateDescription(bountied))
        {
            _bountyPlayer = bountied;
            BountyAmount = bounty;
        }

        public static string[] CreateDescription(Mobile bountyPlayer)
        {
            string subtext1 = null;
            string subtext2 = null;

            switch (Utility.Random(18))
            {
                case 0: subtext1 = "hath murdered one too many!"; break;
                case 1: subtext1 = "shall not slay again!"; break;
                case 2: subtext1 = "hath slain too many!"; break;
                case 3: subtext1 = "cannot continue to kill!"; break;
                case 4: subtext1 = "must be stopped."; break;
                case 5: subtext1 = "is a bloodthirsty monster."; break;
                case 6: subtext1 = "is a killer of the worst sort."; break;
                case 7: subtext1 = "hath no conscience!"; break;
                case 8: subtext1 = "hath cowardly slain many."; break;
                case 9: subtext1 = "must die for all our sakes."; break;
                case 10: subtext1 = "sheds innocent blood!"; break;
                case 11: subtext1 = "must fall to preserve us."; break;
                case 12: subtext1 = "must be taken care of."; break;
                case 13: subtext1 = "is a thug and must die."; break;
                case 14: subtext1 = "cannot be redeemed."; break;
                case 15: subtext1 = "is a shameless butcher."; break;
                case 16: subtext1 = "is a callous monster."; break;
                case 17: subtext1 = "is a cruel, casual killer."; break;
            }

            switch (Utility.Random(7))
            {
                case 0: subtext2 = "A bounty is hereby offered"; break;
                case 1: subtext2 = "Lord British sets a price"; break;
                case 2: subtext2 = "Claim the reward! 'Tis"; break;
                case 3: subtext2 = "Lord Blackthorn set a price"; break;
                case 4: subtext2 = "The Paladins set a price"; break;
                case 5: subtext2 = "The Merchants set a price"; break;
                case 6: subtext2 = "Lord British's bounty"; break;
            }

            var text = String.Format("The foul scum known as {0} {1} For {2} is responsible for {3} murders. {4} of {5} gold pieces for {6} head!", bountyPlayer.RawName, subtext1, (bountyPlayer.Body.IsFemale ? "she" : "he"), bountyPlayer.Kills, subtext2, BountyInformation.GetBounty(bountyPlayer).ToString(), (bountyPlayer.Body.IsFemale ? "her" : "his"));

            var current = 0;
            var linesList = new List<string>();

            // break up the text into single line length pieces
            while (current < text.Length)
            {
                // make each line 25 chars long
                var length = text.Length - current;

                if (length > 25)
                {
                    length = 25;

                    while (text[current + length] != ' ')
                        length--;

                    length++;
                    linesList.Add(text.Substring(current, length));
                }
                else
                {
                    linesList.Add(string.Format("{0} ", text.Substring(current, length)));
                }

                current += length;
            }

            return linesList.ToArray();
        }

        public BountyMessage(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(_bountyPlayer);
            writer.Write(BountyAmount);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            _bountyPlayer = reader.ReadMobile();
            BountyAmount = reader.ReadInt();

            if (_bountyPlayer == null)
                Delete();
        }

        public static void UpdateBounty(Mobile m)
        {
            // Unfortunately BulletinMessage does not allow updating of the subject. So we have to delete and remake the bounty message.
            // Move these to BountyBoard.
            BountyBoard.GloballyDeleteMessage(m);
            BountyBoard.GloballyCreateMessage(m);
        }
    }
}
