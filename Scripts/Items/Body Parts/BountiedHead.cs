﻿using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Server.Items
{
    class BountiedHead : Head
    {
        [CommandProperty(AccessLevel.Counselor)]
        public DateTime Created { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public int Bounty { get; set; }

        [CommandProperty(AccessLevel.Counselor)]
        public PlayerMobile Player { get; set; }

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