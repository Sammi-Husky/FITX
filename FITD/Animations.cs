using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FITD
{
    static class Animations
    {

        public static Dictionary<uint, string> ParseAnimations(string motionFolder)
        {
            var dict = new Dictionary<uint, string>();
            var files = Directory.EnumerateFiles(motionFolder, "*.*", SearchOption.AllDirectories).
                Where(x => x.EndsWith(".pac", StringComparison.InvariantCultureIgnoreCase) ||
                x.EndsWith(".bch", StringComparison.InvariantCultureIgnoreCase)).Select(x => x);
            foreach (var path in files)
                ParseAnim(path, ref dict);
            return dict;
        }

        public static void ParseAnim(string path, ref Dictionary<uint, string> dict)
        {
            if (path.EndsWith(".pac"))
            {
                byte[] filebytes = File.ReadAllBytes(path);
                int count = (int)Util.GetWord(filebytes, 8, Endianness.Big);

                for (int i = 0; i < count; i++)
                {
                    uint off = (uint)Util.GetWord(filebytes, 0x10 + (i * 4), Endianness.Big);
                    string FileName = Util.GetString(filebytes, off, Endianness.Big);
                    string AnimName = Regex.Match(FileName, @"(.*)([A-Z])([0-9][0-9])(.*)\.omo").Groups[4].ToString();
                    if (string.IsNullOrEmpty(AnimName))
                        continue;

                    AddAnimHash(AnimName, ref dict);
                    AddAnimHash(AnimName + "_C2", ref dict);
                    AddAnimHash(AnimName + "_C3", ref dict);
                    AddAnimHash(AnimName + "L", ref dict);
                    AddAnimHash(AnimName + "R", ref dict);
                    foreach (var anim in Animations.builtin_names)
                        AddAnimHash(anim, ref dict);


                    if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                       AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                        AddAnimHash(AnimName.Substring(0, AnimName.Length - 1), ref dict);
                }
            }
            else if (path.EndsWith(".bch"))
            {
                using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(0xC, SeekOrigin.Begin);
                    int off = reader.ReadInt32();
                    stream.Seek(off, SeekOrigin.Begin);

                    while (reader.PeekChar() != '\0')
                    {
                        var tmp = reader.ReadStringNT();
                        string AnimName = Regex.Match(tmp, @"(.*)([A-Z])([0-9][0-9])(.*)").Groups[4].ToString();
                        if (string.IsNullOrEmpty(AnimName))
                        {
                            continue;
                        }

                        AddAnimHash(AnimName, ref dict);
                        AddAnimHash(AnimName + "_C2", ref dict);
                        AddAnimHash(AnimName + "_C3", ref dict);
                        AddAnimHash(AnimName + "L", ref dict);
                        AddAnimHash(AnimName + "R", ref dict);
                        foreach (var anim in Animations.builtin_names)
                            AddAnimHash(anim, ref dict);


                        if (AnimName.EndsWith("s4s", StringComparison.InvariantCultureIgnoreCase) ||
                           AnimName.EndsWith("s3s", StringComparison.InvariantCultureIgnoreCase))
                            AddAnimHash(AnimName.Substring(0, AnimName.Length - 1), ref dict);
                    }
                }
            }
        }
        public static void AddAnimHash(string name, ref Dictionary<uint, string> dict)
        {
            uint crc = Crc32.Compute(name.ToLower());
            if (dict.ContainsValue(name) || dict.ContainsKey(crc))
                return;

            dict.Add(crc, name);
        }

        public static List<string> builtin_names = new List<string>()
        {
            "Attack11_set",
            "AttackS4_set",
            "AttackS4Hi_set",
            "AttackS4Lw_set",
            "AttackHi4_L_HAND_N_set",
            "AttackAirLw_set",
            "ThrowF_set",
            "ThrowB_set",
            "SpecialSAttack_set",
            "SpecialAirSAttack_set",
            "SpecialSWall_set",
            "SpecialSWallJump_set",
            "SpecialSWallAttackF_set",
            "SpecialSWallAttackB_set",
            "SpecialSWallEnd_set",
            "AttackHi4_R_HAND_N_set",
            "game_LuigiFinalShootIndirectNoReactionCommon",
            "game_LuigiFinalShootIndirectCommon",
            "game_EquipmentAbilityHighSpeedDash",
            "effect_EquipmentAbilityHighSpeedDash",
            "game_EquipmentAbilityJustGuardBomber",
            "effect_EquipmentAbilityJustGuardBomber",
            "effect_GuardOn",
            "effect_GuardLandingEffect",
            "effect_Catch",
            "effect_SwordSwing1",
            "effect_SwordSwing3",
            "effect_SwordSwing4",
            "effect_SwordSwingDash",
            "effect_BatSwing1",
            "effect_BatSwing3",
            "effect_BatSwing4",
            "effect_BatSwingDash",
            "effect_HarisenSwing1",
            "effect_HarisenSwing3",
            "effect_HarisenSwing4",
            "effect_HarisenSwingDash",
            "effect_StarRodSwing1",
            "effect_StarRodSwing3",
            "effect_StarRodSwing4",
            "effect_StarRodSwingDash",
            "effect_LipStickSwing1",
            "effect_LipStickSwing3",
            "effect_LipStickSwing4",
            "effect_LipStickSwingDash",
            "effect_ClubSwing1",
            "effect_ClubSwing3",
            "effect_ClubSwing4",
            "effect_ClubSwingDash",
            "effect_FirebarSwing1",
            "effect_FirebarSwing3",
            "effect_FirebarSwing4",
            "effect_FirebarSwingDash",
            "SpecialLwAttack_C%d",
            "KirbyLinkBowChargeMax",
            "MetaknightSpecialNSpinGroundEffect",
            "KirbyToonLinkBowChargeMax",
            "SpecialNSearch",
            "MurabitoSpecialNErase",
            "AttackAirLw2Bound",
            "AttackAirLw2Attack",
            "LinkBowChargeMax",
            "LinkBowChargeMax_C2",
            "LinkBowChargeMax_C3",
            "LinkBowChargeMax_C4",
            "SpecialSLanding",
            "SpecialLwChargeMax",
            "SpecialNSpinGroundEffect",
            "SpecialNSpinGroundEffect_C2",
            "SpecialNSpinGroundEffect_C3",
            "SpecialNSpinGroundEffect_C4",
            "FinalHitWait",
            "SpecialS2EndLanding",
            "SpecialHi1",
            "SpecialAirHi1",
            "SpecialHi2",
            "SpecialAirHi2",
            "FinalAttack",
            "FinalHit",
            "SpecialHiEnd",
            "SpecialHiEnd_C2",
            "SpecialHiEnd_C3",
            "SpecialHiEnd_C4",
            "SpecialNSuccess",
            "SpecialNSuccess_C2",
            "SpecialNSuccess_C3",
            "SpecialNSuccess_C4",
            "SpecialNFailure",
            "SpecialNFailure_C2",
            "SpecialNFailure_C3",
            "SpecialNFailure_C4",
            "BodyToChange",
            "BodyToSphere",
            "SpecialNSearch",
            "SpecialNSearch_C2",
            "SpecialNSearch_C3",
            "SpecialNSearch_C4",
            "SpecialSDashAttack",
            "SpecialSDashAttack_C2",
            "SpecialSDashAttack_C3",
            "SpecialSDashAttack_C4",
            "AttackAirLw2Bound",
            "AttackAirLw2Attack",
            "ToonlinkBowChargeMax",
            "ToonlinkBowChargeMax_C2",
            "ToonlinkBowChargeMax_C3",
            "ToonlinkBowChargeMax_C4",
        };
    }
}
