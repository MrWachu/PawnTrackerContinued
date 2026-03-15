using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld.QuestGen;
using System.Reflection;
using PawnTrackerMain;
using static PawnTrackerMain.PawnDeathDetails;
using static PawnTrackerMain.DocumentedEventDefOf;
using static PawnTrackerMain.PawnTrackerUtility;
using Verse.AI.Group;

namespace PawnTrackerMain.HospitalityPatches
{   
    public static class Patches
    {
        private static MethodInfo recruitMethod;
        private static MethodInfo createLordForPawnMethod;
        private static MethodInfo isGuestMethod;
        private static MethodInfo joinLordMethod;
        private static Type guestUtilityType;
        static Patches()
        {
            if (!ModsConfig.IsActive("Orion.Hospitality"))
            {
                return;
            }

            var hospitalityAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(asm => asm.GetName().Name == "Hospitality");
            guestUtilityType = hospitalityAssembly?.GetType("Hospitality.Utilities.GuestUtility");
            if (guestUtilityType == null)
            {
                Log.Warning("Hospitality is active but Hospitality.Utilities.GuestUtility was not found. Hospitality patches will be skipped.");
                return;
            }

            recruitMethod = guestUtilityType.GetMethod("Recruit");
            createLordForPawnMethod = guestUtilityType.GetMethod("CreateLordForPawn");
            joinLordMethod = guestUtilityType.GetMethod(name: "JoinLord");
            isGuestMethod = guestUtilityType.GetMethod("IsGuest");
        }

        public static void ApplyPatches(Harmony harmony)
        {
            if (recruitMethod != null)
            {
                harmony.Patch(recruitMethod,
                    prefix: new HarmonyMethod(typeof(Patches), nameof(HospitalityGuestUtility_Recruit_Patch_Prefix)));
            }
            else
            {
                Log.Warning("Hospitality patch skipped: Recruit method not found.");
            }

            if (createLordForPawnMethod != null)
            {
                harmony.Patch(createLordForPawnMethod,
                    prefix: new HarmonyMethod(typeof(Patches), nameof(HospitalityGuestUtility_CreateLordForPawn_Patch_Postfix)));
            }
            else
            {
                Log.Warning("Hospitality patch skipped: CreateLordForPawn method not found.");
            }

            if (joinLordMethod != null)
            {
                harmony.Patch(joinLordMethod,
                    postfix: new HarmonyMethod(typeof(Patches), nameof(HospitalityGuestUtility_JoinLord_Patch_Postfix)));
            }
            else
            {
                Log.Warning("Hospitality patch skipped: JoinLord method not found.");
            }
        }

        public static void HospitalityGuestUtility_Recruit_Patch_Prefix(Pawn guest, int recruitPenalty, bool forced)
        {
            if (guest == null)
                return;

            if (isGuestMethod != null && (bool)isGuestMethod.Invoke(null, new object[] { guest }))
            {
                guest.GetComp<PawnTrackerMain.PawnTrackerComp>().AddEvent(new JoinEvent(guest, forced ? GuestRecruitedForced : GuestRecruited, priorFaction: guest.Faction));
            }
        }
        
        public static void HospitalityGuestUtility_CreateLordForPawn_Patch_Postfix(Pawn pawn)
        {
            if (pawn == null)
                return;
            if (isGuestMethod != null && (bool)isGuestMethod.Invoke(null, new object[] { pawn }))
            {
                pawn.GetComp<PawnTrackerMain.PawnTrackerComp>().AddEvent(new LifeEvent(pawn, BecameGuest));
            }
        } 

        public static void HospitalityGuestUtility_JoinLord_Patch_Postfix(Lord lord, Pawn pawn)
        {
            if (pawn == null)
                return;
            if (isGuestMethod != null && (bool)isGuestMethod.Invoke(null, new object[] { pawn }))
            {
                pawn.GetComp<PawnTrackerMain.PawnTrackerComp>().AddEvent(new LifeEvent(pawn, BecameGuest));
            }
        }
    }
}