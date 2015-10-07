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
			if( Expansion > Expansion.UOR )
				throw new Exception( "This copy of RunUO will not be designed for anything higher than UOR." );
			
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
