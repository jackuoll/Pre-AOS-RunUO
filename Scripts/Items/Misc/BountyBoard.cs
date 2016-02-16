using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Items
{
    public class BountyBoard : BaseBulletinBoard
    {
        private static BulletinBoard m_MasterBoard;
        public static BulletinBoard MasterBoard
        {
            get
            {
                if (m_MasterBoard == null)
                {
                    m_MasterBoard = new BulletinBoard();
                    m_MasterBoard.Name = "Master Bulletin Board";
                }

                return m_MasterBoard;
            }
        }

        [Constructable]
        public BountyBoard() : base(0x1E5E)
        {
            BoardName = "bounty board";
        }

        public BountyBoard(Serial serial) : base(serial)
        {
        }

        public override void Cleanup()
        {
            var validBountyPlayers = BountyInformation.GetValidBounties().Select(x=>x.BountyPlayer);
            MasterBoard.Items.Where(x => x is BountyMessage && !validBountyPlayers.Contains(((BountyMessage)x).BountyPlayer))
                .ToList()
                .ForEach( message => message.Delete() );
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
    }

    public class BountyMessage : BulletinMessage
    {
        private Mobile _bountyPlayer;

        public Mobile BountyPlayer { get { return _bountyPlayer; } }

        //public BulletinMessage(Mobile poster, BulletinMessage thread, string subject, string[] lines) : base( 0xEB0 )

        public BountyMessage(Mobile m) : base(m, null, BountyInformation.GetBounty(m).ToString() + " gold pieces", CreateDescription(m))
        {
            _bountyPlayer = m;
            //BountyBoard.MasterBoard.AddItem(this);
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

            string text = String.Format("The foul scum known as {0} {1} For {2} is responsible for {3} murders. {4} of {5} gold pieces for {6} head!", bountyPlayer.RawName, subtext1, (bountyPlayer.Body.IsFemale ? "she" : "he"), bountyPlayer.Kills, subtext2, BountyInformation.GetBounty(bountyPlayer).ToString(), (bountyPlayer.Body.IsFemale ? "her" : "his"));

            // TODO: wtf is the below?

            int current = 0;
            int lineCount = 25;
            List<String> linesList = new List<string>();

            string[] lines = new string[lineCount];
            char space = ' ';

            // break up the text into single line length pieces
            while (text != null && current < text.Length)
            {
                // make each line 25 chars long
                int length = text.Length - current;

                if (length > 25)
                {
                    length = 25;

                    while (text[current + length] != space)
                        length--;

                    length++;
                    linesList.Add(text.Substring(current, length));
                }
                else
                {
                    linesList.Add(String.Format("{0} ", text.Substring(current, length)));
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
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            _bountyPlayer = reader.ReadMobile();
            if (_bountyPlayer == null)
                Delete();
        }

        public static void UpdateBounty(Mobile m)
        {
            // Unfortunately BulletinMessage does not allow updating of the subject. So we have to delete and remake the bounty message.
            DeleteBounty(m);
            new BountyMessage(m);
        }

        public static void DeleteBounty(Mobile m)
        {
            var curMsg = BountyBoard.MasterBoard.Items.Where(x => x is BountyMessage && ((BountyMessage)x).BountyPlayer == m).FirstOrDefault();
            if(curMsg!=null)
                curMsg.Delete();
        }
    }
}
