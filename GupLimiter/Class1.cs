using BepInEx;
using BepInEx.Configuration;
using RoR2;
using MonoMod.Cil;
using System;
using System.Collections.ObjectModel;
using Mono.Cecil.Cil;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace GupLimiter
{
    [BepInPlugin("com.Moffein.GupLimiter", "GupLimiter", "1.0.1")]
    public class GupLimiter : BaseUnityPlugin
    {
        public static bool aggressive = false;
        public static bool ignorePlayerTeam = true;

        private static BodyIndex gupIndex;
        private static BodyIndex geepIndex;

        public void Awake()
        {
            aggressive = Config.Bind("General", "Aggressive Limiting", false, "Count Gups as 4 enemies instead of 2.").Value;
            ignorePlayerTeam = Config.Bind("General", "Ignore Player Team", true, "Don't apply changes to Gups/Geeps on the Player team.").Value;

            IL.RoR2.MasterSummon.Perform += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, x => x.MatchCall<TeamComponent>("GetTeamMembers"));
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);    //MasterSummon
                c.Emit(OpCodes.Ldloc_0);    //teamIndex
                c.EmitDelegate<Func<int, MasterSummon, TeamIndex, int>>((memberCount, self, teamIndex) =>
                {
                    if (!(ignorePlayerTeam && teamIndex == TeamIndex.Player))
                    {
                        var teamMembers = TeamComponent.GetTeamMembers(teamIndex);
                        foreach (TeamComponent tc in teamMembers)
                        {
                            if (tc.body)
                            {
                                if (tc.body.bodyIndex == geepIndex)
                                {
                                    memberCount++;
                                }
                                else if (tc.body.bodyIndex == gupIndex)
                                {
                                    memberCount += aggressive ? 3 : 1;
                                }
                            }
                        }
                    }
                    return memberCount;
                });
            };

            RoR2.RoR2Application.onLoad += GetBodyIndices;
        }

        private void GetBodyIndices()
        {
            gupIndex = BodyCatalog.FindBodyIndex("GupBody");
            geepIndex = BodyCatalog.FindBodyIndex("GeepBody");
        }
    }
}
