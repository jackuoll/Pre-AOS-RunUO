using System;
using System.Collections.Generic;
using Server.Misc;
using Server.Network;
using Server.Mobiles;
using System.Linq;
using Server.Items;

namespace Server.Gumps
{
    public class ReportMurdererBountyGump : Gump
    {
        private int m_Idx;
        private List<Mobile> m_Killers;
        private Mobile m_Victum;

        [CallPriority(1)]
        public static void Initialize()
        {
            EventSink.PlayerDeath -= ReportMurdererGump.EventSink_PlayerDeath;
            EventSink.PlayerDeath += EventSink_PlayerDeath;
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            Mobile m = e.Mobile;

            List<Mobile> killers = new List<Mobile>();
            List<Mobile> toGive = new List<Mobile>();

            foreach (AggressorInfo ai in m.Aggressors)
            {
                if (ai.Attacker.Player && ai.CanReportMurder && !ai.Reported)
                {
                    // Note: The default reportmurderer script only allows reporting the same player once every 10 minutes in the Core.SE expansion.
                    // This was also the case in the T2A era, so I'm not sure why it was set to a minimum expansion of Core.SE
                    if (!((PlayerMobile) m).RecentlyReported.Contains(ai.Attacker))
                    {
                        killers.Add(ai.Attacker);
                        ai.Reported = true;
                        ai.CanReportMurder = false;
                    }
                }

                if (ai.Attacker.Player && (DateTime.UtcNow - ai.LastCombatTime) < TimeSpan.FromSeconds(30.0) &&
                    !toGive.Contains(ai.Attacker))
                    toGive.Add(ai.Attacker);
            }

            foreach (AggressorInfo ai in m.Aggressed)
            {
                if (ai.Defender.Player && (DateTime.UtcNow - ai.LastCombatTime) < TimeSpan.FromSeconds(30.0) &&
                    !toGive.Contains(ai.Defender))
                    toGive.Add(ai.Defender);
            }

            foreach (Mobile g in toGive)
            {
                int n = Notoriety.Compute(g, m);

                int theirKarma = m.Karma, ourKarma = g.Karma;
                bool innocent = (n == Notoriety.Innocent);
                bool criminal = (n == Notoriety.Criminal || n == Notoriety.Murderer);

                int fameAward = m.Fame/200;
                int karmaAward = 0;

                if (innocent)
                    karmaAward = (ourKarma > -2500 ? -850 : -110 - (m.Karma/100));
                else if (criminal)
                    karmaAward = 50;

                Titles.AwardFame(g, fameAward, false);
                Titles.AwardKarma(g, karmaAward, true);
            }

            if (m is PlayerMobile && ((PlayerMobile) m).NpcGuild == NpcGuild.ThievesGuild)
                return;

            if (killers.Count > 0)
            {
                var g = m.FindGump(typeof (ReportMurdererBountyGump)) as ReportMurdererBountyGump;
                if (g != null)
                    g.TryAddKillers(killers);
                else
                    new GumpTimer(m, killers).Start();
            }
        }

        private class GumpTimer : Timer
        {
            private Mobile m_Victim;
            private List<Mobile> m_Killers;

            public GumpTimer(Mobile victim, List<Mobile> killers) : base(TimeSpan.FromSeconds(4.0))
            {
                m_Victim = victim;
                m_Killers = killers;
            }

            protected override void OnTick()
            {
                m_Victim.SendGump(new ReportMurdererBountyGump(m_Victim, m_Killers));
            }
        }

        public ReportMurdererBountyGump(Mobile victum, List<Mobile> killers) : this(victum, killers, 0)
        {
        }

        private ReportMurdererBountyGump(Mobile victum, List<Mobile> killers, int idx) : base(0, 0)
        {
            m_Killers = killers;
            m_Victum = victum;
            m_Idx = idx;
            BuildGump();
        }

        private void BuildGump()
        {
            AddBackground(265, 205, 393, 270, 70000);
            AddImage(265, 205, 1140);

            Closable = false;
            Resizable = false;

            AddPage(0);

            AddHtml(325, 255, 300, 60,
                "<BIG>Would you like to report " + m_Killers[m_Idx].Name + " as a murderer?</BIG>", false, false);

            int bountymax = GetBountyMax(m_Victum);

            if (m_Killers[m_Idx].Kills >= 4 && bountymax > 0)
            {
                AddHtml(325, 325, 300, 60, "<BIG>Optional Bounty: [" + bountymax + " max] </BIG>", false, false);
                AddImage(323, 343, 0x475);
                AddTextEntry(329, 346, 311, 16, 0, 1, "");
            }

            AddButton(385, 395, 0x47B, 0x47D, 1, GumpButtonType.Reply, 0);
            AddButton(465, 395, 0x478, 0x47A, 2, GumpButtonType.Reply, 0);
        }

        private int GetBountyMax(Mobile from)
        {
            Item[] gold = from.BankBox.FindItemsByType(typeof (Gold), true);
            int total = 0;
            for (int i = 0; i < gold.Length; i++)
                total += gold[i].Amount;

            return total;
        }

        private int RemoveGoldFromBank(Mobile from, int total)
        {
            Item[] gold, checks;
            int balance = Banker.GetBalance(from, out gold, out checks);

            if (total > balance)
                total = balance;

            int totalremoved = 0;

            for (int i = 0; total > 0 && i < gold.Length; ++i)
            {
                if (gold[i].Amount <= total)
                {
                    total -= gold[i].Amount;
                    totalremoved += gold[i].Amount;
                    gold[i].Delete();
                }
                else
                {
                    gold[i].Amount -= total;
                    totalremoved += total;
                    total = 0;
                }
            }
            return totalremoved;
        }

        private void TryAddKillers(List<Mobile> killers)
        {
            m_Killers.AddRange(
                killers.Where(
                    killer =>
                        !m_Killers.Contains(killer) && !((PlayerMobile) m_Victum).RecentlyReported.Contains(killer)));
        }

        public static void ReportedListExpiry_Callback(object state)
        {
            object[] states = (object[]) state;

            PlayerMobile from = (PlayerMobile) states[0];
            Mobile killer = (Mobile) states[1];

            if (from.RecentlyReported.Contains(killer))
            {
                from.RecentlyReported.Remove(killer);
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            Mobile from = state.Mobile;

            switch (info.ButtonID)
            {
                case 1:
                {
                    Mobile killer = m_Killers[m_Idx];
                    if (killer != null && !killer.Deleted)
                    {
                        killer.Kills++;
                        killer.ShortTermMurders++;


                        ((PlayerMobile) from).RecentlyReported.Add(killer);
                        Timer.DelayCall(TimeSpan.FromMinutes(10), new TimerStateCallback(ReportedListExpiry_Callback),
                            new object[] {from, killer});

                        PlayerMobile pk = (PlayerMobile) killer;

                        int bounty = 0;
                        if (info.GetTextEntry(1) != null)
                        {
                            TextRelay c = info.GetTextEntry(1);
                            if (c != null)
                            {
                                bounty = Utility.ToInt32(c.Text);
                                if (bounty > 0)
                                {
                                    bounty = RemoveGoldFromBank(from, bounty);
                                    BountyInformation bi = BountyInformation.AddBounty(pk, bounty);
                                    //BountyMessage.UpdateBounty(bi);

                                    pk.SendAsciiMessage("{0} has placed a bounty of {1} {2} on your head!", from.Name,
                                        bounty, (bounty == 1) ? "gold piece" : "gold pieces");

                                    from.SendAsciiMessage("You place a bounty of {0}gp on {1}'s head.", bounty, pk.Name);
                                }
                            }
                        }

                        pk.ResetKillTime();
                        pk.SendLocalizedMessage(1049067); //You have been reported for murder!

                        if (pk.Kills == 5)
                        {
                            pk.SendLocalizedMessage(502134); //You are now known as a murderer!
                        }
                        else if (SkillHandlers.Stealing.SuspendOnMurder && pk.Kills == 1 &&
                                 pk.NpcGuild == NpcGuild.ThievesGuild)
                        {
                            pk.SendLocalizedMessage(501562); // You have been suspended by the Thieves Guild.
                        }
                    }
                    break;
                }
                case 2:
                {
                    break;
                }
            }

            m_Idx++;
            if (m_Idx < m_Killers.Count)
                from.SendGump(new ReportMurdererBountyGump(from, m_Killers, m_Idx));
        }
    }
}