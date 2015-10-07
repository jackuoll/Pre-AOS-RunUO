using System;
using Server.Network;

namespace Server
{
	public class CurrentExpansion
	{
		private static readonly Expansion Expansion = Expansion.T2A;
		public static readonly bool IncludeTrammel = false;

		public static void Configure()
		{
			Core.Expansion = Expansion;

			Mobile.InsuranceEnabled = false;
			ObjectPropertyList.Enabled = true;
			Mobile.VisibleDamageType = Expansion == Expansion.UOR ? VisibleDamageType.Related : VisibleDamageType.None;
			Mobile.GuildClickMessage = true;
			Mobile.AsciiClickMessage = true;
			Mobile.ActionDelay = 500;
		}
	}
}
