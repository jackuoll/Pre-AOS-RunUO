using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Misc
{
    class BountyInformation
    {
        public static List<BountyInformation> AllInfo = new List<BountyInformation>();

        public PlayerMobile BountyPlayer;
        public int Bounty;
        public DateTime LastBounty;

        public bool Expired
        {
            get { return BountyPlayer.Deleted || BountyPlayer.Kills < 5 || LastBounty + TimeSpan.FromDays(14.0) < DateTime.UtcNow; }
        }

        public static BountyInformation AddBounty(PlayerMobile pm, int bounty)
        {
            var bi = AllInfo.Where(info => info.BountyPlayer == pm).FirstOrDefault();

            if (bi == null)
            {
                bi = new BountyInformation{ BountyPlayer = pm };
                AllInfo.Add(bi);
            }

            bi.AddBounty(bounty);
            return bi;
        }

        internal static List<BountyInformation> GetValidBounties()
        {
            AllInfo = AllInfo.Where(x => !x.Expired).ToList();
            return AllInfo;
        }

        private void AddBounty(int bounty)
        {
            Bounty += bounty;
            LastBounty = DateTime.UtcNow;
        }

        internal static object GetBounty(Mobile bountyPlayer)
        {
            return AllInfo.Where(info => info.BountyPlayer == bountyPlayer).Select(info => info.Bounty).First();
        }
    }
}
